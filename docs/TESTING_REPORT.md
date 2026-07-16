# TIME LOOP — Testing Report

## What this report is (and isn't)

This delivery was authored in a sandbox with **no Unity Editor and no dotnet CLI installed** — the
Bash environment used to build it cannot compile C#, run the Unity Test Runner, or execute a single
line of the code in this repository. Every automated test in `Assets/Tests/` is written, correct to
the best of careful manual and cross-agent review, and **has never actually been run**. This report
documents what verification *was* possible without a compiler (careful hand-tracing, independent
reimplementations of the simulation in Python, targeted code-review passes) and what remains for a
human opening the project in real Unity for the first time — which should be treated as the actual
first verification step, not an afterthought. See `docs/BUILD_INSTRUCTIONS.md` for exactly how to
run the test suite.

## What was actually caught and fixed without a compiler

Not being able to compile does not mean nothing was verified — three genuine, confirmed defects
were found and fixed during this build, each through a different verification technique:

### 1. Box-pushing was silently disabled (found via test-writing cross-check)

`GridWorld.IsSolidEntityAt` (`Assets/Scripts/Grid/GridWorld.cs`) checked `e.IsSolid` without
excluding `MovableBox`, even though its own doc comment said it should. Since `MovableBox.IsSolid`
is `true`, `IsBlockedIgnoringBoxes` treated every box tile as a wall, so `MoveResolver`'s
wall/door check short-circuited before the box-push feasibility branch was ever reached — every box
in the game would have acted exactly like an immovable wall. This was caught by an agent writing
`MoveResolverTests.cs`, who traced the actual call path by hand while writing the box-push
assertions and noticed the contradiction between the doc comment and the implementation. **Fixed**
by excluding `MovableBox` explicitly in `IsSolidEntityAt`.

### 2. Multi-actor spawn deadlock (found via independent Python reimplementation)

Every ghost and the live player reset to the *identical* spawn tile at the start of every loop.
The original `MoveResolver` rule for two-or-more actors intending the same destination tile was
"everybody bounces back" — symmetric, order-independent. But a ghost always reissues the exact same
recorded command at the exact same tick index regardless of outcome, so whenever two or more
co-located actors took the same first step out of spawn (the overwhelmingly common case — most
solutions start by walking the same corridor), they would all cancel back to spawn, tick after
tick, forever: a genuine permanent deadlock, not a one-off bump. This was caught by a level-design
agent that, rather than trust its own hand-traced tick-by-tick reasoning, wrote a from-scratch
Python reimplementation of `MoveResolver.Resolve`/`GridWorld.Step` and replayed its claimed
solutions against it — the simulation simply never terminated for any level requiring 2+
simultaneous ghosts. **Fixed** by making contested-tile resolution asymmetric: the earliest actor
in the fixed processing order (oldest ghost first, live player last) claims the tile, the rest stay
put — which funnels a bunched-up group into a self-clearing single-file queue within one tick per
actor, rather than a standoff. This is a strict improvement with no regression risk: content that
had already worked around the old symmetric rule (several campaign levels explicitly use an
idle-padding "convoy" pattern in their header trace comments) is unaffected, since correctly-padded
actors never trigger the contested-tile branch in the first place.

### 3. Missing `UnityEngine.UI` assembly reference (found via cross-file review pass)

Neither `TimeLoop.Runtime.asmdef` nor `TimeLoop.Editor.asmdef` listed `UnityEngine.UI` in their
`references` array, even though every UI controller script and both editor scene builders use
`UnityEngine.UI`/`UnityEngine.EventSystems` types (`Button`, `Image`, `Slider`, `Toggle`,
`IPointerDownHandler`, etc.). Unity does not auto-reference `com.unity.ugui`'s assembly the way it
does native engine modules, and assembly definition references are not transitive — this omission
would have failed the *entire project's* compilation on first open. **Fixed** by adding
`"UnityEngine.UI"` to both asmdefs' `references` arrays.

A dedicated cross-file consistency review (checking every UI/Editor/VFX/Audio/Monetization/
Achievements/Save file's calls against the actual signatures of the classes they reference) found
no other defects of this kind — no other missing `using` directives, namespace mismatches, or
API-signature mismatches across the 83 runtime scripts.

## Automated test suite (written, unexecuted)

### EditMode (`Assets/Tests/EditMode/`) — pure logic, no scene required

| File | Covers |
|---|---|
| `GridWorldTests.cs` | `IsWallTile` bounds/wall/empty logic, entity register/get/unregister round-trips, a real `Switch` + `Door` toggling together |
| `MoveResolverTests.cs` | Contested-tile resolution, wall blocking, box push success/rejection (including against the fix in defect #1 above), stationary-actor blocking, vacate-exception |
| `LevelParserTests.cs` | Header parsing, spawn coordinate math, door-gating via `BuildWorld()`, goal-id collection, malformed/unknown LEGEND entries not throwing |
| `PuzzleSolverTests.cs` | BFS finds the exact minimal path on an open corridor; correctly reports failure on an unreachable goal within a bounded tick budget |
| `SaveSystemTests.cs` | Save/load round-trip, missing-file fallback, star-rating never regressing on a worse replay |
| `DailySeedGeneratorTests.cs` | Same date → same seed, adjacent dates → seeds exactly 1 apart, puzzle number = seed + 1 |

### PlayMode (`Assets/Tests/PlayMode/`) — scene-driven integration

| File | Covers |
|---|---|
| `TimelineRecorderPlayModeTests.cs` | A scripted input sequence is recorded frame-accurately, and `RecordedTimeline` holds `Idle` forever past its recorded length |
| `GhostReplayAccuracyTests.cs` | **The single most important test in the codebase.** Two independently hand-chosen input sequences are each recorded, then replayed through a *fresh* `GridWorld` built from the same source, and the ghost's final position/facing/switch-state are asserted to exactly match the original live run. This is the test that proves the core promise of the game — perfect ghost fidelity — actually holds. |
| `LevelCompletionPlayModeTests.cs` | Drives an actual `TimeLoopManager`/`PuzzleManager` through `1-02.level`'s documented canonical solution and asserts `OnLevelResult` fires with the expected loop count |

## Manual verification performed on campaign content

All 23 hand-authored `.level` files (`docs/ROADMAP.md` has the full per-world breakdown) carry a
`#`-prefixed header comment tracing their intended solution tick-by-tick. Several authoring passes
went further and wrote disposable Python scripts (not checked in) that reimplement `LevelParser`,
`MoveResolver`, and `GridWorld.Step` faithfully enough to mechanically replay every level's claimed
solution and confirm: every plate/switch/box/hazard/goal/platform/teleport tile is reachable
(flood-fill from spawn), the floor-tile graph is cycle-free (a pure spine-and-branches tree, ruling
out the kind of accidentally-enclosed pocket a maze layout could hide — an actual mistake caught and
discarded during this project's own design phase), and the simulated win tick matches what the
header comment claims. This is real, if informal, verification — but it is not a substitute for
running the actual `PuzzleSolver`/Test Runner inside Unity, which should be treated as confirmatory,
not optional.

## Known caveat: PAR_TICKS precision after the deadlock fix

World 4's levels were authored with `PAR_TICKS` set within a couple of ticks of a hand-traced
"true minimum," which is appropriate for expert-tier precision content. The multi-actor deadlock
fix (defect #2) was applied *before* those levels were authored, so their traces already account
for it (each was independently verified with the Python reimplementation described above,
including its convoy-timing side effects). Earlier worlds' `PAR_TICKS` values were deliberately set
with generous slack and were not re-derived to the exact tick after the fix, so 3-star thresholds
there may be very slightly looser or tighter than a frame-perfect minimum — inconsequential for
solvability, and exactly the kind of number a real playtesting pass (`docs/ROADMAP.md`'s v1.1) is
expected to tune.

## What a human should do first in real Unity

1. Open the project (`docs/BUILD_INSTRUCTIONS.md`) and let it compile. Expect to fix minor,
   mechanical issues rather than structural ones — three real defects were already caught and
   fixed without a compiler in the loop (above), but "zero remaining issues" cannot be claimed
   for code that has never actually been built.
2. Run the full Test Runner (EditMode then PlayMode) — this is the actual first execution of every
   test in this report, and `GhostReplayAccuracyTests` in particular is the test to trust most.
3. Play through World 1 by hand. If ghost replay ever looks even slightly off, that is the single
   highest-priority bug class in this codebase — everything else is built on top of it being exact.
