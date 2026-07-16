# TIME LOOP — Puzzle Generation

## Why a constrained template family, not fully general generation

The design brief's 5-step recipe (create a target solution → build the environment backward from
it → add required interactions → simulate possible solutions → validate difficulty) is exactly
what this pipeline implements. The open question for any procedural puzzle generator is *how
general* step 1 (the target solution) is allowed to be. Fully general multi-loop puzzle generation
— "invent an arbitrary combination of switches, boxes, hazards, and timing windows across an
arbitrary number of loops, guaranteed solvable" — is a much harder search/constraint problem than
it looks, because the thing being searched over (what should every earlier loop's recording
contain) explodes combinatorially with loop count.

Time Loop v1 ships a **single, well-defined template family** — "N plates, each held by its own
ghost, gating one door the final unencumbered live player walks through" — generated at arbitrary
difficulty, and makes it genuinely solid: connected by construction, independently re-verified by
real search, difficulty-rated transparently, and infinitely repeatable with no two seeds producing
the same room. This is deliberately the same design choice real Sokoban-style generators make:
constrain the family, randomize freely *within* it, and validate hard. See `docs/ROADMAP.md`'s
post-launch plan for how additional families (relay-carrying, timed windows, hazard choreography)
are expected to be added to the same pipeline without touching its architecture.

## The pipeline, step by step

1. **`SolutionSynthesizer`** (`Assets/Scripts/Generation/SolutionSynthesizer.cs`) computes the
   exact N+1-loop plan *analytically* from the layout's geometry — no search needed at this step,
   because a spine-and-branch layout's shortest path is just "walk up the spine, then along the
   branch," directly computable from coordinates.
2. **`EnvironmentBuilder`** (`EnvironmentBuilder.cs`) builds that geometry: one vertical one-tile
   corridor ("the spine") from spawn to the goal, with N one-tile side branches connecting directly
   to the spine at alternating rows, each branch a dead end holding one `PressurePlate`. A single
   AND-gated `Door` sits on the spine just below the goal. This shape is connected by construction
   — there is no wall configuration the algorithm can produce that disconnects a branch, because
   branches are carved as an unconditional straight line from a spine tile that was already marked
   floor.
3. **Emission as ordinary `.level` text.** `ProceduralGenerator` assembles a header (`NAME`,
   `WORLD: 5`, `LOOP_SECONDS`, `MAX_TIMELINES`, `PAR_LOOPS`, `PAR_TICKS`) plus the `GRID:`/
   `LEGEND:` text `EnvironmentBuilder` produced, and hands it to **the exact same
   `LevelParser.Parse`** hand-authored campaign levels use. This is the single most important
   integrity property of the whole system: there is no separate "generated level" code path in the
   game to ever fall out of sync with hand-authored content. If `LevelParser`/`GridWorld`/
   `MoveResolver` behave correctly for campaign levels, they behave identically for generated ones.
4. **Independent verification via real search.** `PuzzleSolver.FindPathToGoal` is a genuine
   breadth-first search over `(tick, position)` states that drives the *actual*
   `GridWorld.Step`/`MoveResolver` at every node (via `GridWorldState`/actor-position snapshot and
   restore for backtracking) — never an approximate model of the rules. `ProceduralGenerator.Verify`
   uses it to confirm, independently of the synthesizer's own math, that every ghost loop can
   really reach its plate and the final loop can really reach the goal once every plate is held. If
   verification fails (which would only happen from an actual bug — the layout is connected by
   construction), `GeneratedLevelCache` retries with a small number of deterministic seed
   variants before giving up, so a single bad seed can never reach a player.
5. **`DifficultyRater`** turns `(plateCount, layout size, final-loop tick count)` into one
   transparent weighted integer — deliberately not a black box, so both the game and a designer
   reading this doc can explain *why* a given puzzle is rated the way it is:
   `plateCount * 25 + (width * height) / 4 + finalLoopTicks`.

## Determinism: why not `System.Random` or `UnityEngine.Random`

Every player must get the *same* Daily Challenge on the same UTC date. `System.Random`'s sequence
is not contractually guaranteed identical across .NET runtime versions, and Unity's own RNG isn't
designed for this kind of cross-platform seed-reproducibility guarantee either. `DeterministicRandom`
(`Assets/Scripts/Generation/DeterministicRandom.cs`) is a tiny, dependency-free xorshift32 generator
whose entire algorithm is ~10 lines of pure integer arithmetic — the same on iOS, Android, and the
Editor, forever, by construction. `DailySeedGenerator` (`Assets/Scripts/Daily/`) turns the UTC
calendar date into an integer seed fed straight into this pipeline.

## Infinite Mode vs. Daily Challenge

Both features call the identical `ProceduralGenerator.Generate(seed, difficultyTier)` through
`GeneratedLevelCache`. Infinite Mode lets the player pick a difficulty tier (0–2, mapped to
"Easy"/"Medium"/"Hard" in the UI) and generates a fresh seed each time; Daily Challenge always uses
`DailySeedGenerator.SeedForToday()` at a fixed tier (1, "Medium") so the day's puzzle is identical
worldwide and moderately challenging without requiring Infinite Mode's difficulty controls.

## Known limitations of the v1 generation family (see docs/ROADMAP.md for the plan to broaden this)

- Only the plates-behind-a-gated-door archetype is generated; boxes, moving platforms, hazards,
  and teleport pads appear only in hand-authored campaign content in this delivery.
- Difficulty scaling is currently plate count + room size + final approach length — it does not
  yet vary branch shape, add decoy branches, or otherwise vary the *texture* of the puzzle, only
  its scale. Hand-authored levels currently carry all of the "creative" puzzle-vocabulary variety
  (relay-carrying, timed windows, divergent loop lengths); broadening the generator to synthesize
  those same patterns is the natural v1.1+ extension, and the pipeline (synthesize → build →
  parse → verify → rate) does not need to change shape to support it, only `EnvironmentBuilder`
  and `SolutionSynthesizer` need new template variants.
- The solver verifies and times a *given* construction; it does not, by itself, discover creative
  alternative multi-loop strategies a player might find. That is intentional — see
  `Assets/Scripts/Generation/PuzzleSolver.cs`'s doc comment for the exact scope this was built to.
