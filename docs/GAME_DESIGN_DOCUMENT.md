# TIME LOOP — Game Design Document

## 1. Vision

*"Wait... I can use myself?"*

Time Loop is a premium mobile puzzle game about cooperating with your own past. Every level is a room. You have a short window to act in it. When the window closes, everything you just did becomes a silent, translucent echo that repeats forever — and you step back to the start, now sharing the room with yourself.

The one-sentence pitch: **solve puzzles designed for two (or more) people, using only yourself.**

Comparable games and what we take from each:
- **Baba Is You** — a small rule set with combinatorial depth; the player discovers the "trick" of the game once and re-applies it in escalating novel ways.
- **The Room** — tactile, premium, atmospheric object-focused puzzle-box feel.
- **Braid** — time manipulation as the *verb* of the game, not a gimmick bolted onto platforming.
- **Monument Valley** — minimalist, calm, gorgeous; a 15-minute campaign people replay for the feeling, not the challenge.
- **Wordle / Sudoku** — one puzzle a day, shareable result, zero-friction daily ritual.

Time Loop's differentiator: none of those games make *you* into a puzzle piece. The ghost is not an obstacle or a recording to watch — it's a cooperative partner built entirely out of your own choices.

## 2. Core Loop

1. Level opens: a player character, a room, a countdown ("20 seconds remaining" / or a move budget in later worlds).
2. Player acts: move, push, flip switches, open doors, carry objects.
3. Timer expires (or player manually ends the loop early once comfortable — see §4.2).
4. A translucent ghost now performs *exactly* what the player just did, forever, on a loop.
5. Player resets to the start tile and must reach the goal *using the ghost as a tool* — standing where the ghost can't, pressing what it doesn't, timing an action around its fixed rhythm.
6. Repeat until the goal is reached. Levels can require 1 ghost (World 1) up to 3+ ghosts (World 3-4).

The emotional arc every level is built to produce: **confusion → the "wait" moment → cleverness → satisfaction.** The 60-second rule: a first-time player must understand "my past self repeats what I did" from Level 1 alone, without a tutorial popup wall of text — taught entirely through a single guided first level (see §5, World 1).

## 3. Controls (Mobile-First)

- **Move:** swipe in a direction, or tap a tile in one of the 4 cardinal directions from the player (both inputs map to the same discrete `InputCommand` — see Technical Architecture §4.2). An on-screen D-pad is available as an accessibility/precision option in Settings.
- **Interact:** tap the actor's own tile (or a dedicated Interact button) to press/use whatever is underfoot.
- **Reset:** one visible button, always available — instantly restarts the current loop (does not erase already-recorded ghost timelines, only the in-progress one).
- **Pause:** top-corner icon — settings, level restart-from-scratch, quit to menu.

Movement is grid-locked and tweened (150–250ms per tile depending on `GameSettings`) so it reads as fluid, not robotic, while staying perfectly deterministic underneath (Technical Architecture §2).

## 4. Mechanics Depth

### 4.1 Teaching the "Wait" Moment (Level 1 walkthrough)

Level 1-01: a single room, one switch, one door, the goal just past the door. The player can reach the switch or the door, never both — the switch is on the near side, the door + goal on the far side, separated so a single character physically cannot hold the switch and walk through. First loop: walk to the switch, press it (door opens visibly, then swings shut the instant you're not on the switch anymore — this "shows the rules" without text). Timer runs out with the player standing on the switch. Ghost appears, replaying "walk to switch, hold it." Player resets, walks straight past the now-permanently-held-open door to the goal. No dialogue box says "use your ghost" — the level *is* the tutorial.

### 4.2 Loop Timing Control

Early worlds use a **fixed countdown** so players can't overthink — it forces action and teaches the rhythm. From World 2 onward, levels increasingly use a **manual loop-end button** ("Split Timeline") instead of/alongside a countdown, so advanced players can act efficiently rather than padding out a timer, which matters once 3-star scoring rewards fewest loops *and* fewest total ticks (§7).

### 4.3 Cooperation Patterns (the actual puzzle vocabulary)

These are the "verbs" levels are built from, escalating World 1 → 4:

- **Hold-and-pass** (World 1): ghost holds a switch/plate so present-self can walk through what it opens.
- **Two-switch doors** (World 1): a door needs two plates pressed at once — impossible alone, trivial with one ghost.
- **Relay carrying** (World 2): ghost carries a box partway; present-self picks up where it left off.
- **Timed windows** (World 2): a ghost-triggered platform or door is only open during a specific tick range — the player must arrive inside that window, teaching tick-precise planning.
- **Stacked timelines** (World 3): 3+ ghosts each hold one of three simultaneous plate requirements.
- **Divergent loop lengths** (World 3): ghosts of different recorded lengths finish and "idle" at different times — a puzzle can be built so a short-loop ghost frees a path only after a long-loop ghost has already passed through it.
- **Hazard choreography** (World 3): a moving hazard's fixed patrol must be dodged by weaving between where the live player is and where a ghost's earlier pass drew it away (via a decoy plate).
- **Minimal-input mastery** (World 4): levels solvable only within a very tight tick/move budget — no room for a wasted step, rewarding players who plan the *entire* multi-timeline solution before touching the screen.

### 4.4 Advanced Time Variations (post-launch modifiers, see Roadmap)

Slow Time (all ticks take longer, more reaction room), Fast Forward (ghosts tick faster than the live player — forces the player to "catch up" to a rendezvous), Broken Timeline (a ghost's recorded input stream has a small, level-defined perturbation applied — e.g. one extra idle tick — turning "predict the exact repeat" into "predict the approximate repeat," a deliberate mastery-tier twist), Parallel Timeline (two ghosts run *the same recorded input stream simultaneously offset in time* rather than sequentially recorded), Reverse Timeline (a ghost plays its recorded command list back-to-front). These are implemented as a `LoopRuleModifier` hook on `TimeLoopManager` (see Technical Architecture) so World 5+ content can mix and match without new engine code.

## 5. World-by-World Progression

| World | Levels | Theme | New concepts | Loop count |
|---|---|---|---|---|
| **1 — Understanding Time** | 1–20 | Clean, bright, minimal training-room aesthetic | Recording, ghost replay, single switch/door, hold-and-pass | 1 ghost |
| **2 — Multiple Timelines** | 21–50 | Cooler light, particle drift increases | Two ghosts, moving platforms, timed windows, relay carrying | 1–2 ghosts |
| **3 — Advanced Time Manipulation** | 51–100 | Deeper contrast, visible "time distortion" haze | 3+ ghosts, divergent loop lengths, moving hazards, chained objects | 2–3 ghosts |
| **4 — Expert Challenges** | 100+ | Near-monochrome, high contrast, minimal HUD | Tiny tick windows, minimal-move constraints, community/daily-caliber puzzles | 3+ ghosts, tight budgets |

This delivery implements the full engine + a representative, hand-verified slice of each world (exact counts in `docs/ROADMAP.md`) built on a level format and editor tool designed to scale to the full 150+ without further engineering work.

## 6. Game Modes

- **Campaign** — the worlds above, star-rated, gated by cumulative stars (soft-gating: a world requires N stars from the previous world, not 100% completion, so a player stuck on one hard level isn't blocked from everything after it).
- **Daily Time Loop Challenge** — one procedurally generated, seed-shared puzzle per calendar day, Wordle-style shareable result text, no ads, always free.
- **Infinite Mode** — endless procedurally generated puzzles with a numeric difficulty rating, generated and solver-verified on-device (Technical Architecture §4.6, full algorithm in `docs/PUZZLE_GENERATION.md`).

## 7. Scoring: Stars

Per level, `PuzzleManager` compares the player's result against author-set (or solver-computed, for generated levels) thresholds:

- **★★★** — solved at or under `PAR_LOOPS` *and* `PAR_TICKS` (fewest loops, fastest completion).
- **★★☆** — solved within a looser standard-completion band.
- **★☆☆** — solved, but only after using a hint (see §8).

## 8. Hints

Levels beyond World 1 include an optional, opt-in hint: after a configurable idle/fail threshold, the game offers to visually flash the *next single tile* the intended solution moves through (not the full solution) — enough to unstick a player without solving the puzzle for them. Using a hint caps the level at 1 star, preserving the integrity of the star rating as a genuine mastery signal.

## 9. Achievements (starter set — full list in `AchievementDatabase.cs`)

- **First Timeline** — complete your first loop.
- **Future Self** — cooperate with a ghost 100 times.
- **Perfect Prediction** — complete a level with zero wasted moves (matches the solver's minimal solution exactly).
- **Time Master** — complete every Expert (World 4) level.
- **Split Second** — solve a level with under 1 second of the countdown remaining.
- **Streak** — complete 7 consecutive Daily Challenges.

## 10. Visual & Audio Direction

**Visual:** minimal geometric environments, soft directional lighting, a single accent color per world that shifts subtly as difficulty escalates (World 1 warm white → World 4 near-monochrome with a cold accent). Ghosts render as translucent (~35% alpha), unlit, with a fading particle trail (`GhostTrailEffect`) along their path. Loop transitions play a `TimeRewindEffect` (a soft radial shutter + chromatic aberration pulse) and `TimelineSplitEffect` (the screen visibly "forks" for a beat as the ghost peels away from the player). Level completion plays a `LevelCompleteEffect` (light bloom + gentle time-freeze on the solved tile).

**Audio:** ambient, sparse, futuristic pad-based background music per world (`MusicTrackId`), rising in tempo/density with world difficulty. SFX: footstep tick, switch clunk, door slide, loop-transition "rewind whoosh," goal chime. Music and SFX have independent mute toggles in Settings, persisted via `SaveSystem`.

## 11. Monetization (see also `docs/MARKETING_STRATEGY.md`)

**Ethical, one-time-purchase-first model**, not F2P gacha/energy-timer:

- **Free:** Worlds 1 (20 levels) + first 30 levels of World 2, Daily Challenge (always, forever, no ads), light interstitial ads only *between* campaign level completions (never mid-puzzle, never in Daily Challenge or Infinite Mode).
- **Premium ($4.99–$7.99 one-time):** full campaign (all 150+ levels), Infinite Mode, Advanced Time Variations, ad removal.
- **Cosmetic-only IAP (optional, does not affect difficulty):** Timeline visual themes — Cyberpunk, Ancient Clockwork, Space-Time, Dream World, Minimal Black & White — reskins of the ghost trail, environment palette, and UI accent only.

No pay-to-win, no energy systems, no forced ads mid-puzzle — monetization never interrupts the thinking moment, which is the product's entire appeal.

## 12. Accessibility

Colorblind-safe interactable palette (shape-coded, not color-only: switches/doors/hazards are distinguished by icon silhouette in addition to color), adjustable tween speed (for motion sensitivity), on-screen D-pad alternative to swipe, and a "no countdown pressure" toggle in Settings that converts fixed-timer levels to manual-split levels for players who find real-time pressure stressful rather than fun (does not affect star eligibility beyond disabling the fastest-time component of ★★★, loop-count still applies).
