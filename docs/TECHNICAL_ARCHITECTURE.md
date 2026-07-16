# TIME LOOP — Technical Architecture

## 1. Engine & Target

- **Engine:** Unity 2022.3 LTS (URP, Universal 2D/2.5D rendering)
- **Language:** C# (.NET Standard 2.1)
- **Platforms:** iOS 13+, Android 8+ (API 26+)
- **Input:** Unity Input System package (touch: tap/swipe, on-screen D-pad; editor: WASD/arrows)
- **Target performance:** 60fps on iPhone 11 / mid-tier Android (Snapdragon 700-series), with a 30fps floor guarantee on low-end devices via URP quality tiers.
- **Offline-first:** all campaign content, save data, and progression work with zero network connectivity. Daily Challenge and leaderboards are the only features that benefit from (but do not require) connectivity — the daily puzzle seed is derived from the device date and cached locally.

## 2. The Central Design Decision: Discrete Deterministic Simulation

Time Loop's entire value proposition is trust: **the ghost must reproduce your past actions with perfect, uncanny fidelity, every single time.** If a ghost ever drifts — clips a wall, mistimes a switch by a frame, lands on the wrong tile because of float error — the core fantasy breaks and the player stops trusting the mechanic.

Two implementations were considered:

| Approach | Records | Replay method | Risk |
|---|---|---|---|
| Continuous transform recording | Position/rotation samples every frame | Interpolate sampled transforms | Physics/float drift, frame-rate dependent capture, desync between recorded contact events and interpolated position |
| **Discrete input recording (chosen)** | The player's **input command** on every fixed simulation tick | Feed the identical recorded input stream through the identical deterministic simulation function | None — same inputs + same deterministic function + same starting world state = bit-identical outcome, always |

**Decision: Time Loop is a grid-based, tick-driven puzzle game**, in the tradition of *Baba Is You* and *Sokoban*, not a physics platformer. Movement is discrete (one tile per step), but rendered with smooth tweened interpolation between tiles so it *feels* continuous and premium (Monument Valley-style polish on top of Sokoban-style logic).

This decision is what makes every other systemic requirement in the spec tractable:

- **Perfect ghost replay** — a ghost is not a "recording being played back," it is *a second instance of the exact same actor simulation, fed recorded inputs instead of live ones.* There is no separate replay code path to desync from the original.
- **Procedural generation with guaranteed solvability** — a deterministic simulation can be run forward from a candidate solution (construct-by-trace) and re-verified backward by exhaustive search (BFS/IDDFS solver), because there is no hidden physics state to make the search space unbounded.
- **Daily Challenge notation** — a solution is literally a sequence of discrete moves per timeline, which maps 1:1 to the spec's `⬆️⬆️➡️` arrow notation.
- **Deterministic simultaneous multi-actor resolution** — with N timelines (player + ghosts) all ticking together, conflicts (two actors entering the same tile, a chain push) must resolve identically every loop. A discrete simulation makes this a solvable, fully-specified rule set (§5) instead of an emergent physics problem.

The tick rate is `GameSettings.TicksPerSecond` (default 12 Hz — fast enough to feel responsive on touch input, slow enough that queued swipe input never feels laggy). One **tick** = one `InputCommand` per active actor = one atomic simulation step.

## 3. Solution & File Structure

```
Time-Loop/
├── docs/                                   Design + business deliverables (this folder)
└── TimeLoop/                                Unity project root
    ├── Packages/manifest.json               Package dependencies (Input System, TextMeshPro, Test Framework, Addressables)
    ├── ProjectSettings/ProjectVersion.txt    Pinned editor version
    ├── Assets/
    │   ├── Scripts/
    │   │   ├── Core/                         GameManager, GameSettings (ScriptableObject tuning), SceneRouter
    │   │   ├── Grid/                          GridCoord, Direction, InputCommand, IGridEntity, GridWorld, GridWorldState, MoveResolver
    │   │   ├── Actors/                        GridActor (pure sim), IInputProvider, Live/RecordedInputProvider, PlayerController, GhostAgent, ActorVisual
    │   │   ├── Timeline/                      InputFrame, RecordedTimeline, TimelineRecorder, TimeLoopManager, TimelineEvents
    │   │   ├── Interactables/                 IInteractable, Switch, PressurePlate, Door, MovableBox, Hazard, Goal, MovingPlatform, TeleportPad
    │   │   ├── Puzzle/                        PuzzleManager, WinCondition, LevelResult
    │   │   ├── Levels/                        LevelDefinition, LevelParser, LevelLoader, LevelDatabase
    │   │   ├── Generation/                    ProceduralGenerator, SolutionSynthesizer, EnvironmentBuilder, PuzzleSolver, DifficultyRater, GeneratedLevelCache
    │   │   ├── Daily/                         DailyChallengeManager, DailySeedGenerator, MoveNotation, ShareCardGenerator
    │   │   ├── Save/                          SaveSystem, SaveData, PlayerProfile, LevelProgressRecord
    │   │   ├── Achievements/                  AchievementDefinition, AchievementSystem, AchievementDatabase
    │   │   ├── Audio/                         AudioManager, SFXId, MusicTrackId
    │   │   ├── Monetization/                  IAPManager, AdManager, PremiumUnlock, ThemeUnlockSystem
    │   │   ├── VFX/                           GhostTrailEffect, TimeRewindEffect, ClockDistortionEffect, TimelineSplitEffect, LevelCompleteEffect
    │   │   └── UI/                            HUD/, Menus/, Common/
    │   ├── Editor/                            SceneBuilders (programmatic scene construction), LevelEditorWindow
    │   └── Resources/Levels/World{1..4}/      Hand-authored .level text assets, loaded via Resources.LoadAll at runtime
    └── Tests/
        ├── EditMode/                          Pure-logic unit tests (no Unity runtime needed)
        └── PlayMode/                          Timeline/ghost fidelity integration tests
```

Four assembly definitions keep compile times low and enforce boundaries: `TimeLoop.Runtime`, `TimeLoop.Editor` (editor-only, references Runtime), `TimeLoop.Tests.EditMode`, `TimeLoop.Tests.PlayMode`.

## 4. Core Systems Reference

### 4.1 Grid layer (`Grid/`) — pure C#, zero MonoBehaviour dependency, fully unit-testable

- `GridCoord` — integer `(x, y)` struct with arithmetic + `Direction` offset helper.
- `Direction` — `None, Up, Down, Left, Right`.
- `InputCommand` — `{ Direction Move; bool Interact; }`, the atomic unit that gets recorded.
- `IGridEntity` — implemented by every interactable/actor placed on the grid (`GridCoord Position`, `bool IsSolid`, `bool IsPushable`).
- `GridWorld` — the mutable simulation state for one level attempt: tile map, dictionary of live entities by coordinate, and the single authoritative `Step(IReadOnlyList<ActorTickInput> inputs)` method that advances everything by exactly one tick.
- `GridWorldState` — an immutable snapshot (struct of arrays) used to reset the world to its initial configuration at the start of every loop, and to save/restore state for the procedural solver's search.
- `MoveResolver` — implements the simultaneous multi-actor conflict rules (§5) as a pure function `Resolve(GridWorld, inputs) -> ResolvedMoves`.

### 4.2 Actors (`Actors/`)

- `GridActor` — the deterministic per-actor simulation core: given a `GridWorld` and one `InputCommand`, computes intended movement/interaction. Identical code path for the live player and every ghost.
- `IInputProvider` — `InputCommand GetCommand(int tick)`.
  - `LiveInputProvider` — reads the Input System this tick, queues the next command.
  - `RecordedInputProvider` — returns `RecordedTimeline[tick]`, clamined at the timeline's recorded length (idle thereafter).
- `PlayerController` (MonoBehaviour) — owns a `GridActor` + `LiveInputProvider`, forwards ticks from `TimeLoopManager`, drives `ActorVisual` tweening.
- `GhostAgent` (MonoBehaviour) — owns a `GridActor` + `RecordedInputProvider` bound to one `RecordedTimeline`, renders translucent with `GhostTrailEffect`.
- `ActorVisual` — shared tile-to-tile tween/animation (shared by player and ghosts so they move identically on screen).

### 4.3 Timeline (`Timeline/`) — the loop mechanic

- `InputFrame` — `{ int Tick; InputCommand Command; }`.
- `RecordedTimeline` — `List<InputFrame>` + metadata (`LoopIndex`, `TotalTicks`, `MoveCount`).
- `TimelineRecorder` — attached alongside the live `PlayerController`; appends an `InputFrame` every tick.
- `TimeLoopManager` — the orchestrator and the single most important class in the codebase:
  1. Drives the countdown (`GameSettings.TicksPerSecond * level.LoopDurationSeconds` ticks, or a move-count budget for later worlds).
  2. Every tick: gathers `(player command, every active ghost's recorded command for this tick)`, calls `GridWorld.Step(...)`.
  3. On expiry: freezes the `RecordedTimeline`, spawns a new `GhostAgent` from it, resets `GridWorld` to its initial `GridWorldState`, resets the player to the spawn tile, and starts a fresh recording — up to `level.MaxTimelines` concurrent ghosts.
  4. Fires `TimelineEvents` (`OnLoopStart`, `OnLoopEnd`, `OnGhostSpawned`, `OnTimeCritical`) that UI/Audio/VFX subscribe to — no other system reaches into `TimeLoopManager`'s internals.

### 4.4 Interactables (`Interactables/`)

Each implements `IInteractable.OnEnter/OnExit/OnInteract(GridActor)`: `Switch` (toggled by standing + pressing Interact), `PressurePlate` (auto-toggles from occupancy, releases on exit), `Door` (subscribes to one or more Switches/Plates, opens when its condition — AND/OR of source states — is satisfied), `MovableBox` (pushable crate, chains pushes), `Hazard` (resets the current loop's player, does not affect ghosts — a ghost has already "survived" by definition), `Goal` (marks level completion tile(s)), `MovingPlatform` (patrols a fixed deterministic path keyed to tick count, not real time), `TeleportPad` (paired instant relocation, World 3+).

### 4.5 Puzzle & Levels

- `PuzzleManager` — checks the level's `WinCondition` every tick (all required `Goal`s occupied by the live player, optionally requiring specific ghosts also present); on success computes `LevelResult` (loops used, ticks elapsed, hints used) and awards 1–3 stars per the level's thresholds.
- `LevelDefinition` / `LevelParser` / `LevelLoader` — see §6 for the text format. `LevelDatabase` indexes all campaign levels by world/number at boot.

### 4.6 Generation (`Generation/`) — Infinite Mode, see `docs/PUZZLE_GENERATION.md` for the full algorithm.

### 4.7 Daily Challenge (`Daily/`)

`DailySeedGenerator` derives a deterministic seed from UTC calendar date (`yyyyMMdd` → hash), so every player worldwide gets the same puzzle. `DailyChallengeManager` feeds that seed to `ProceduralGenerator` to build the day's puzzle, tracks completion locally, and gates re-attempts (one official result per day, unlimited practice replays). `MoveNotation` encodes/decodes a `RecordedTimeline` list to the `⬆️⬆️➡️` arrow-emoji share string. `ShareCardGenerator` composes the shareable result text (`TIME LOOP #365 · Solved in 3 timelines · ⭐⭐⭐⭐⭐`).

### 4.8 Save / Achievements / Audio / Monetization / VFX / UI

See inline XML doc comments in each class. `SaveSystem` persists a single JSON `SaveData` blob to `Application.persistentDataPath` (offline-first, no cloud dependency in v1; a cloud-sync hook point is documented in `BUILD_INSTRUCTIONS.md` for a future release). All manager singletons follow the same lightweight pattern: a single `MonoBehaviour` created once by `GameManager` in the boot scene, exposed via `GameManager.Instance.X`, communicating outward only via C# events — no other system calls into concrete class internals across a system boundary, which is what keeps the parallelizeable subsystems (UI, Audio, Monetization) decoupled from the core mechanic.

## 5. Simultaneous Multi-Actor Resolution Rules

Every tick, the live player and every active ghost (up to `MaxTimelines`) move at once. Resolution order is fixed and documented so it is 100% reproducible:

1. **Order:** ghosts resolve in the order their timelines were recorded (oldest loop first), then the live player resolves last. This order is arbitrary but *fixed* — determinism requires *a* consistent rule, not a specific one.
2. **Intent phase:** every actor computes its intended destination tile from its `InputCommand` without mutating the world.
3. **Static collision:** an intended destination that is a wall or solid entity cancels that actor's move (it stays put) but its `Interact` flag still resolves against its *current* tile.
4. **Actor-vs-actor collision:** if two or more actors intend the same destination tile, the one earliest in the fixed order (rule 1) claims it; the rest cancel and stay put. This is deliberately *not* symmetric — every actor starts every loop from the exact same spawn tile, so "several actors take the same first step out of spawn" is the common case, not an edge case. A ghost always reissues the same recorded command at the same tick regardless of outcome, so a symmetric "everybody bounces" rule would make co-located actors retry, and fail, an identical move forever. First-in-order-wins instead funnels them into a single-file queue that clears itself within one tick per actor — the same intuitive reading a player would give "I bumped into my own ghost."
5. **Push chains:** a `MovableBox` in an actor's intended destination is only enterable if the box's next tile in the same direction is free; the box moves first, then the actor. A box cannot be pushed into another actor's resolved destination (falls back to rule 4).
6. **Interactions apply after movement resolves**, in the same fixed order (ghosts oldest→newest, then player), so a switch a ghost presses this tick is visible to a door check the player triggers later the same tick.
7. **Hazards** apply only to the live player — a ghost is an immutable record of a *successful* past run, so if the original run touched a hazard it wouldn't have been recorded as a viable timeline in the first place (see `TimeLoopManager` — a hazard hit resets the current loop immediately, discarding that attempt's recording).

This rule set is the actual "physics" of Time Loop and is exercised directly by `Tests/EditMode/MoveResolverTests.cs`.

## 6. Level Data Format

Hand-authored and procedurally-generated levels share one plain-text format (`.level`), chosen so both human designers and `ProceduralGenerator` can produce/consume it, and so it is trivially diffable in git:

```
# Comments start with #
NAME: 1-04 Two Switches
WORLD: 1
LOOP_SECONDS: 15
MAX_TIMELINES: 1
PAR_LOOPS: 2
PAR_TICKS: 40
GRID:
##########
#S.......#
#.####.#.#
#.#..#.#.#
#.#A.#.B.#
#.#..#.#.#
#.####.#.#
#........#
####DD####
#........#
#....G...#
##########
LEGEND:
S=spawn A=switch:a B=switch:b D=door:a+b G=goal
```

`LevelParser` reads the header key/values plus the ASCII grid (each character maps to a tile type or entity spawn via `LEGEND`), producing a `LevelDefinition` and initial `GridWorldState`. `PAR_LOOPS`/`PAR_TICKS` drive the 3-star/2-star thresholds in `PuzzleManager`.

## 7. Testing Strategy

See `docs/TESTING_REPORT.md`. Summary: pure-logic systems (`Grid`, `MoveResolver`, `PuzzleSolver`, `LevelParser`, `SaveSystem`, `DailySeedGenerator`) are covered by `EditMode` tests requiring no scene; `Timeline`/`GhostAgent` fidelity (the single highest-risk correctness property in the game — "does the ghost reproduce the exact recorded run") is covered by `PlayMode` tests that record a scripted input sequence, force a loop, and assert the ghost's resulting position/interactions exactly match the original run tick-for-tick.

## 8. What This Delivery Includes vs. Scaffolds for Later

This is a full, playable, architecturally complete implementation of every system in the design brief. Two categories of pragmatic scope decisions, documented here rather than hidden:

- **Campaign content:** a representative, fully-solvable set of levels spanning all four worlds (see `docs/ROADMAP.md` for exact counts) rather than all 150+, because handcrafting 150 individually-tuned puzzles is a content-authoring effort measured in weeks, not an architecture question — the `LevelDatabase`, `.level` format, and `LevelEditorWindow` tool are built to scale to the full count without any code changes.
- **Store-integration stubs:** `IAPManager` and `AdManager` implement the full app-facing API (`PurchasePremium()`, `RestorePurchases()`, `ShowInterstitial()`, events) against an in-memory mock backend, with clearly marked integration points for Unity IAP and a mediation SDK (e.g. LevelPlay/AdMob) — actual store account setup, product IDs, and SDK credentials are business/account tasks outside what a codebase can contain.

No gameplay system is stubbed: recording, ghost replay, interactables, puzzle logic, procedural generation + solver, daily challenge, save, and achievements are fully implemented and playable end-to-end.
