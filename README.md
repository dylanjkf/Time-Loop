# TIME LOOP

*Solve puzzles by cooperating with your previous actions.*

A premium mobile puzzle game: a countdown ends your turn, your exact recorded moves become a
translucent ghost that repeats forever, and you must use that ghost — holding a switch, carrying a
box partway — to solve what one person alone cannot.

## Repository layout

```
docs/           Design, architecture, and business deliverables (read GAME_DESIGN_DOCUMENT.md and
                TECHNICAL_ARCHITECTURE.md first)
TimeLoop/       The Unity 2022.3 LTS project itself
```

## Start here

- **Playing/building it:** `docs/BUILD_INSTRUCTIONS.md`
- **What the game is:** `docs/GAME_DESIGN_DOCUMENT.md`
- **How it's built:** `docs/TECHNICAL_ARCHITECTURE.md`
- **How Infinite Mode generates puzzles:** `docs/PUZZLE_GENERATION.md`
- **What's been verified and how:** `docs/TESTING_REPORT.md`
- **Content scope and what's next:** `docs/ROADMAP.md`
- **Store listing, ASO, and go-to-market:** `docs/APP_STORE_LISTING.md`, `docs/ASO_KEYWORDS.md`, `docs/MARKETING_STRATEGY.md`

## The core idea in one paragraph

Movement is grid-based and tick-deterministic rather than continuous — a deliberate choice (see
`docs/TECHNICAL_ARCHITECTURE.md` §2) that makes ghost replay perfectly reproducible, makes
procedural puzzle generation solver-verifiable, and maps directly onto the Daily Challenge's
arrow-emoji share notation. A ghost is not a recorded video clip; it is a second instance of the
exact same deterministic actor simulation, fed the player's recorded inputs instead of live ones —
so it never desyncs from what actually happened.
