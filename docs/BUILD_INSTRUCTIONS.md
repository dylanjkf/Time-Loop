# TIME LOOP — Build Instructions

This is the practical, first-time-opening guide for a developer picking up this project in Unity. It assumes zero prior context beyond "I have Unity and I need to get this running."

## Before you start: what to expect

This entire codebase — every script under `Assets/Scripts`, the editor tooling under `Assets/Editor`, and the design docs in `docs/` — was authored by an AI coding agent working in a sandbox that had **no Unity Editor and no `dotnet`/MSBuild CLI installed**. That means nothing in this repository has ever actually been compiled by anything. There are no `.meta` files checked in (Unity generates one per asset on import, and this repo has none yet), no `ProjectSettings.asset` beyond the bare `ProjectVersion.txt`, and no `.unity` scene files — those get created by the steps below. In practice this means: the first time you open the project, expect Unity to spend several minutes re-importing and generating metadata for every file, and expect the Console to show compiler errors partway through that resolve themselves once package resolution finishes. It also means the first real compile is the first time this code has ever been checked by a C# compiler, so treat the checklist in this document — not just "hit Play" — as the actual bring-up procedure, not a formality. The architecture was designed and cross-checked carefully by hand (see `docs/TECHNICAL_ARCHITECTURE.md`), so if something is wrong it is far more likely to be a small, local compile nit than a structural design flaw. See `docs/TESTING_REPORT.md` for a precise account of what was and wasn't possible to verify without Unity available.

None of this should be alarming — it just means you are the first compiler and the first player. Budget a real first session (an hour, not five minutes) for bring-up.

## 1. Prerequisites

1. **Unity 2022.3 LTS, exact patch `2022.3.50f1`.** Check `TimeLoop/ProjectSettings/ProjectVersion.txt` in this repo — it pins `m_EditorVersion: 2022.3.50f1` (revision `a0d8f57d5a0c`). Install this exact version through Unity Hub (Installs > Install Editor > Archive, or search 2022.3.50f1 directly) rather than "whatever 2022.3.x is current" — LTS patches can differ enough in Input System/URP behavior to matter here. Make sure the **iOS Build Support** and/or **Android Build Support** modules are checked during install if you intend to do device builds later (Section 6); you can add them afterward from Hub too.
2. **Git**, to clone/pull this repository.
3. **An IDE with C# support** — JetBrains Rider (recommended, best Unity integration) or VS Code with the C# Dev Kit / ms-dotnettools.csharp extension and the Unity extension. Set your IDE of choice as Unity's external script editor under `Edit > Preferences > External Tools` after first opening the project.

You do **not** need the `dotnet` CLI installed separately — Unity ships its own bundled Mono/.NET toolchain and will compile the project's assemblies itself once opened.

## 2. Opening the project

1. `git clone`/`git pull` this repository as usual.
2. Launch **Unity Hub**, go to `Projects > Open`, and browse to **`Time-Loop/TimeLoop`** specifically — not the repository root. The repo root (`Time-Loop/`) also contains a sibling `docs/` folder with design documents; that folder is not part of the Unity project and Unity has nothing to do with it. The actual Unity project (the folder containing `Assets/`, `Packages/`, and `ProjectSettings/`) is `Time-Loop/TimeLoop`.
3. Hub should detect the project needs Unity `2022.3.50f1` and offer to open it with that version (install it first per Section 1 if you haven't). Confirm and let it open.
4. On first open, Unity will import every asset from scratch (there are no `.meta` files checked in yet) and resolve every package listed in `Packages/manifest.json`:
   - `com.unity.inputsystem` (1.7.0) — the new Input System; `PlayerController` depends on it directly (see Section 5).
   - `com.unity.textmeshpro` (3.0.6) — all UI text.
   - `com.unity.ugui` (1.0.0) — uGUI/Canvas UI.
   - `com.unity.render-pipelines.universal` (14.0.9) — URP, the rendering pipeline this project targets.
   - `com.unity.addressables` (1.21.19), `com.unity.timeline` (1.7.6), `com.unity.purchasing` (4.11.0) — Addressables, Timeline, and Unity IAP (used by `Assets/Scripts/Monetization`).
   - `com.unity.2d.sprite` / `com.unity.2d.tilemap`, `com.unity.test-framework` (1.1.33, for Test Runner — Section 4), and the standard `com.unity.modules.*` modules (ui, uielements, audio, particlesystem, animation, jsonserialize).
   - This whole process (asset import + package resolution + first domain compile) commonly takes several minutes, longer on the first run than any run after. **It is normal to see red compiler errors flash in the Console while packages are still resolving** — e.g. "type or namespace `InputAction` could not be found" before the Input System package has finished registering. Don't start debugging anything until the Editor is fully idle (the little spinner/progress bar in the bottom-right status bar is gone) and you've done a manual `Assets > Reimport All` if errors persist after that.

## 3. First-open checklist

Once the Editor is idle, work through these in order. None of these could be safely hand-authored as raw checked-in files without a compiler and the Editor's serializer available to validate them (a `.asset` YAML file or a `.unity` scene file with a typo or GUID mismatch fails silently or corrupts, rather than erroring at compile time the way code does) — so they're deliberately left as manual, Editor-driven steps rather than files in the repo.

1. **Create a `GameSettings` asset.** Right-click in the Project window (e.g. under `Assets/Resources` or wherever you want to keep it) and choose `Assets > Create > Time Loop > Game Settings`. This creates a `GameSettings` ScriptableObject asset (backing script: `Assets/Scripts/Core/GameSettings.cs`) exposing tick rate, max ticks per frame, the critical-time threshold, and visual tile-speed tuning. `TimeLoopManager` (`Assets/Scripts/Timeline/TimeLoopManager.cs`) has a `[SerializeField] private GameSettings _gameSettings` field that **must** be assigned in the Inspector — the game will `NullReferenceException` on `SecondsRemaining`/`LoadLevel` without it. Drag the asset onto every `TimeLoopManager` component you create (the scene builders in the next step create one; assign it there).
2. **Run the scene-builder menu items** to generate the two entry-point scenes, rather than trying to hand-build a `.unity` file:
   - `Time Loop > Build Scenes > Gameplay Scene`
   - `Time Loop > Build Scenes > Main Menu Scene`

   These are implemented under `Assets/Editor/SceneBuilders/` and construct the scene hierarchy (camera, canvas, `TimeLoopManager`, `PlayerController`, `PuzzleManager`, HUD, menu screens, etc.) and wire up the serialized references they can resolve automatically (prefabs and assets that exist at generation time). Save each scene when prompted (suggested locations: `Assets/Scenes/Gameplay.unity` and `Assets/Scenes/MainMenu.unity` — create the `Scenes` folder if the builder doesn't).
   - After running them, **check both scenes' hierarchies for anything the builder logged as unresolved** (Console messages and/or components with obviously empty/`None` object reference fields in the Inspector are the tell). The most likely gaps are UI prefab references under `Assets/Scripts/UI/Menus` (`MainMenuController`, `LevelSelectController`, `WorldSelectController`, `DailyChallengeController`, `InfiniteModeController`, `PauseMenuController`, `ResultsScreenController`, `SettingsController`) and `Assets/Scripts/UI/HUD` (`HUDController`, `TimerDisplay`, `LoopCounterDisplay`) whose child-object/button/text references depend on prefab instances the builder can only guess at. Hand-wire anything still empty by dragging the corresponding child object or prefab onto the field.
3. **Add both scenes to Build Settings.** `File > Build Settings…`, drag `Gameplay.unity` and `MainMenu.unity` into the "Scenes In Build" list (Main Menu first/at index 0, since that's what a build boots into).
4. **Expect and fix ordinary first-compile issues.** This is real, hand-written C# produced in one large authoring pass with no compiler in the loop to catch mistakes as they were written, so treat the Console as the actual first code review. Go through every error and warning it reports. Given how carefully the system boundaries and data flow were designed and cross-checked (`docs/TECHNICAL_ARCHITECTURE.md`), the realistic expectation is minor, local issues — a missing `using` directive, an accessibility mismatch, a `[SerializeField]`/`[System.Serializable]` nuance Unity's serializer is stricter about than plain C#, an analyzer warning about an unused variable — not a structural rethink of any system. If you do hit something bigger, `docs/TECHNICAL_ARCHITECTURE.md` documents the intended design per subsystem (see Section 7 below for which folders to trust more). Also check `docs/TESTING_REPORT.md` first — it documents exactly what was and wasn't feasible to check by static reading alone before this build pass, so it may already tell you where to look.

## 4. Running the automated tests

`Window > General > Test Runner` opens the Test Runner window, which has two tabs:

- **EditMode** — exercises `Assets/Tests/EditMode` (asmdef: `TimeLoop.Tests.EditMode`), which should cover the pure-C# logic in `Grid/`, `Timeline`, `Puzzle`, generation/solver code, etc. — anything that doesn't need a running scene.
- **PlayMode** — exercises `Assets/Tests/PlayMode` (asmdef: `TimeLoop.Tests.PlayMode`), which needs the Editor's Play Mode (and likely the Gameplay scene) to actually run MonoBehaviours.

Click **Run All** on each tab. Like everything else in this project, **these tests have never been executed** — there was no Unity Editor available to run the Unity Test Framework in the sandbox that wrote them. Running them for the first time here is not a nice-to-have follow-up step, it **is** the verification step for all of the deterministic-simulation logic (ghost replay determinism, tick resolution, win conditions, the procedural generator/solver) that the whole game depends on being bit-exact. If something fails, that's real, useful signal — treat a first red run as expected-but-informative, dig into the specific assertion, and check it against `docs/TECHNICAL_ARCHITECTURE.md` §5 (Simultaneous Multi-Actor Resolution Rules) and §6 (Level Data Format) if the failure is in simulation/level-parsing code specifically.

## 5. Running the game

Open the **Gameplay** scene (`Assets/Scenes/Gameplay.unity`, from Section 3) in the Editor and press **Play**. (The Main Menu scene is the real front door for a build, but Gameplay is the fastest way to sanity-check the core loop directly.)

Controls, confirmed against `Assets/Scripts/Actors/PlayerController.cs`:

- **In-editor / desktop (keyboard, via `Keyboard.current`):** Arrow keys or **WASD** to move one tile per press (Up/W, Down/S, Left/A, Right/D — each queues a single directional move, consumed one per simulation tick); **Space** or **E** to interact (push/use/activate whatever's on the current tile).
- **Touch / mobile simulator (via `Touchscreen.current`):** **swipe** in a direction (past a `_swipeThresholdPixels` = 40px threshold, measured from touch-down to touch-up) to move that direction; a **tap** (release within the 40px threshold) triggers interact — same action as Space/E.
- There's also an explicit programmatic entry point for on-screen D-pad/interact buttons and accessibility controls: `PlayerController.OnDirectionButtonPressed(Direction)` and `OnInteractButtonPressed()` — useful if you're wiring a touch HUD control by button rather than raw swipe/tap.

Input is buffered into a `LiveInputProvider`, and `TimeLoopManager` consumes exactly one queued command per fixed simulation tick (default 12 Hz, from `GameSettings.TicksPerSecond`) — see `docs/TECHNICAL_ARCHITECTURE.md` §4.2/§4.3 for why (this is also why fast repeated key taps are just queued, not applied instantly).

## 6. Building for device

General iOS/Android deployment (signing certificates, Xcode/Gradle install, provisioning profiles) is standard Unity process and not repeated here. What's specific to Time Loop:

**Critical for both platforms — Active Input Handling.** Because there is no checked-in `ProjectSettings.asset` yet (see the "before you start" note above), Unity will generate one with its own defaults the first time you open the project, and that default is **not** guaranteed to have the new Input System active. `PlayerController` calls `Keyboard.current` and `Touchscreen.current` directly from `UnityEngine.InputSystem` — it does **not** support the legacy `Input` manager at all. Go to `Edit > Project Settings > Player > Other Settings > Active Input Handling` and set it to **"Input System Package (New)"** (or at minimum "Both" — but prefer New-only to avoid the legacy manager silently no-op'ing input on some platforms). Unity will prompt you to restart the Editor after changing this — do so before testing Play mode or a device build. If you skip this, the symptom is total input dead-ness on device (and often in-editor too) with no compile error to point at it.

- **iOS:** Xcode (matching whatever version your installed Unity 2022.3.50f1 supports — check Unity's release notes for the recommended Xcode pairing) and an active Apple Developer account for signing/provisioning. Minimum target: **iOS 13**, per `docs/TECHNICAL_ARCHITECTURE.md` §1. Set this under `Player Settings > iOS > Other Settings > Target minimum iOS Version`.
- **Android:** Minimum API level **26** (Android 8.0), per the same spec. Set under `Player Settings > Android > Other Settings > Minimum API Level`. You'll need a keystore for signing release builds (`Player Settings > Android > Publishing Settings`, or `Build Settings > Player Settings` keystore manager) — create one if this is a fresh checkout with no existing keystore, and keep it out of source control.
- **Unity IAP / Monetization:** `com.unity.purchasing` (4.11.0) is already in `Packages/manifest.json` and `Assets/Scripts/Monetization` is wired against it, but actual store product IDs/accounts (App Store Connect, Google Play Console) are real-world integration work, not code — see `docs/ROADMAP.md` for what's still needed there.

## 7. If something doesn't compile

Start with `docs/TECHNICAL_ARCHITECTURE.md` — it documents the intended design and responsibilities of every system (file structure in §3, per-system reference in §4). Check `docs/TESTING_REPORT.md` too; it records what could and couldn't be verified without a Unity install, so it may already flag the exact area you've hit.

Not all folders carry equal risk, and it's worth calibrating your scrutiny accordingly:

- **`TimeLoop.Grid`, `Actors`, `Timeline`, `Interactables`, `Puzzle`, `Levels`** (folders: `Grid/`, `Actors/`, `Timeline/`, `Interactables/`, `Puzzle/`, `Levels/`) are the load-bearing core — the deterministic tick simulation, ghost replay, and puzzle/level logic everything else depends on. If something is subtly wrong here (not just a compile error, but wrong *behavior* — a ghost desyncing, a tick resolving in the wrong order), give it real scrutiny against `docs/TECHNICAL_ARCHITECTURE.md` §2, §4.1–4.5, and §5 before patching around it, since a shortcut fix here can quietly reintroduce the exact determinism bugs that architecture was designed to avoid.
- **`UI`, `Audio`, `Monetization`, `VFX`** are comparatively decoupled — they react to the core only through the `TimelineEvents` event surface (`Timeline/TimelineEvents.cs`) rather than reaching into simulation internals, per `docs/TECHNICAL_ARCHITECTURE.md`'s description of `TimeLoopManager`. These are safe to patch independently: a bug or missing reference in `HUDController` or `AudioManager` is very unlikely to have any bearing on simulation correctness, so fix it locally without worrying about ripple effects into the core systems above.
