using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Grid;
using TimeLoop.Interactables;
using TimeLoop.Levels;
using TimeLoop.Timeline;
using UnityEngine.TestTools;

namespace TimeLoop.Tests.PlayMode
{
    /// <summary>
    /// The single most important test in this codebase: it proves the entire premise of TIME LOOP —
    /// that a ghost, driven purely by a RecordedInputProvider replaying a RecordedTimeline through
    /// GridWorld.Step, reproduces a live run's outcome exactly. No MonoBehaviours, ActorVisual, or
    /// TimeLoopManager are involved on purpose: this isolates the deterministic core (GridWorld +
    /// GridActor + MoveResolver + RecordedTimeline/RecordedInputProvider) that GhostAgent and the
    /// live player both sit on top of, per GridActor's own doc comment ("there is no separate
    /// 'replay' code path to desync from the original run").
    ///
    /// Two independent hand-authored paths are exercised (one ending with the switch left ON, one
    /// with it toggled back OFF) so this isn't just "one lucky path happens to replay correctly."
    /// </summary>
    public sealed class GhostReplayAccuracyTests
    {
        private const string SwitchId = "switch_a";

        // A tiny, fully open 3x3 fixture: a spawn tile, a switch one tile off the spawn column, and
        // open floor everywhere else — just enough room for "a few moves plus one Interact on a
        // switch," as required, with plenty of headroom to hand-author two distinct paths.
        //
        // Text row 0 ("...") is the topmost row and maps to the highest Y (LevelParser.BuildWorld
        // assigns y = gridRows.Count - 1 - rowIndex), so this fixture lays out as:
        //   y=2: (0,2) (1,2) (2,2)   all floor
        //   y=1: (0,1) (1,1)=switch  (2,1) floor
        //   y=0: (0,0)=spawn (1,0) (2,0) floor
        private const string FixtureLevelSource =
            "# Hand-built fixture for GhostReplayAccuracyTests.\n" +
            "NAME: Ghost Replay Fixture\n" +
            "WORLD: 0\n" +
            "GRID:\n" +
            "...\n" +
            ".W.\n" +
            "S..\n" +
            "LEGEND:\n" +
            "S=spawn W=switch:a\n";

        [UnityTest]
        public IEnumerator GhostReplay_PathA_WalksOntoSwitchTogglesItOnEndsFacingLeft()
        {
            // Right, Up (onto the switch), Interact (toggle ON), Up, Left.
            var commands = new List<InputCommand>
            {
                new InputCommand(Direction.Right, false),
                new InputCommand(Direction.Up, false),
                new InputCommand(Direction.None, true),
                new InputCommand(Direction.Up, false),
                new InputCommand(Direction.Left, false),
            };

            yield return RunAndAssertGhostMatchesLive(
                commands,
                expectedFinalPosition: new GridCoord(0, 2),
                expectedFinalFacing: Direction.Left,
                expectedFinalSwitchState: true);
        }

        [UnityTest]
        public IEnumerator GhostReplay_PathB_DoubleTogglesSwitchOffAndReturnsToSpawn()
        {
            // Up, Right (onto the switch), Interact (ON), Interact again (OFF), Down, Left.
            var commands = new List<InputCommand>
            {
                new InputCommand(Direction.Up, false),
                new InputCommand(Direction.Right, false),
                new InputCommand(Direction.None, true),
                new InputCommand(Direction.None, true),
                new InputCommand(Direction.Down, false),
                new InputCommand(Direction.Left, false),
            };

            yield return RunAndAssertGhostMatchesLive(
                commands,
                expectedFinalPosition: new GridCoord(0, 0),
                expectedFinalFacing: Direction.Left,
                expectedFinalSwitchState: false);
        }

        /// <summary>
        /// Drives a live GridActor through <paramref name="commands"/> tick-by-tick via direct
        /// GridWorld.Step calls (recording each command into a RecordedTimeline exactly like
        /// TimelineRecorder does), then replays that exact RecordedTimeline through a fresh,
        /// independently-built GridWorld (same source text, brand-new entity instances) driving a
        /// second, ghost GridActor — then asserts the two runs land on identical Position, Facing,
        /// and switch state.
        /// </summary>
        private static IEnumerator RunAndAssertGhostMatchesLive(
            IReadOnlyList<InputCommand> commands,
            GridCoord expectedFinalPosition,
            Direction expectedFinalFacing,
            bool expectedFinalSwitchState)
        {
            var levelDefinition = LevelParser.Parse("ghost_replay_fixture", FixtureLevelSource);
            var spawn = levelDefinition.SpawnPosition;

            // --- Live run: a real GridActor, stepped tick-by-tick, recorded as it goes. ---
            var liveWorld = levelDefinition.BuildWorld();
            var liveActor = new GridActor("player", spawn);
            var recorder = new TimelineRecorder();
            recorder.BeginNewRecording(0);

            for (var tick = 0; tick < commands.Count; tick++)
            {
                var command = commands[tick];
                recorder.RecordTick(tick, command);

                var orderedInputs = new List<ActorTickInput> { new ActorTickInput(liveActor, command) };
                liveWorld.Step(orderedInputs, tick);

                yield return null;
            }

            var recordedTimeline = recorder.Current;

            // Sanity check on the recording itself before trusting it to drive the ghost.
            Assert.AreEqual(commands.Count, recordedTimeline.Frames.Count);
            for (var i = 0; i < commands.Count; i++)
            {
                Assert.AreEqual(commands[i], recordedTimeline.GetCommandAtTick(i),
                    $"Recorded tick {i} should exactly equal the command that was actually stepped.");
            }

            // --- Ghost replay: a FRESH, independently-built world (same source, new entities), a
            // brand-new ghost GridActor, driven only by a RecordedInputProvider over the recording
            // above. No code path here is shared with the live run above except GridWorld.Step
            // itself — which is exactly the point. ---
            var ghostWorld = levelDefinition.BuildWorld();
            var ghostActor = new GridActor("ghost_0", spawn, isGhost: true);
            var ghostProvider = new RecordedInputProvider(recordedTimeline);

            for (var tick = 0; tick < commands.Count; tick++)
            {
                var command = ghostProvider.GetCommand(tick);
                var orderedInputs = new List<ActorTickInput> { new ActorTickInput(ghostActor, command) };
                ghostWorld.Step(orderedInputs, tick);

                yield return null;
            }

            // The core guarantee: ghost reproduces the live run's final Position and Facing exactly.
            Assert.AreEqual(liveActor.Position, ghostActor.Position,
                "Ghost's final Position must exactly reproduce the live run's final Position.");
            Assert.AreEqual(liveActor.Facing, ghostActor.Facing,
                "Ghost's final Facing must exactly reproduce the live run's final Facing.");

            // Cross-check against the hand-predicted outcome for this specific path, so the test
            // would fail loudly if the fixture's geometry/assumptions ever silently drift.
            Assert.AreEqual(expectedFinalPosition, liveActor.Position, "Live run did not land where this path predicted.");
            Assert.AreEqual(expectedFinalFacing, liveActor.Facing, "Live run's facing did not match this path's prediction.");

            // Any world state the run mutated (here: the switch) must also match between the two
            // worlds — the ghost isn't just visually in the same place, it left the level in the
            // same logical state a real second playthrough of the recording would.
            var liveSwitch = liveWorld.GetEntity<Switch>(SwitchId);
            var ghostSwitch = ghostWorld.GetEntity<Switch>(SwitchId);
            Assert.NotNull(liveSwitch, "Fixture should contain switch_a in the live world.");
            Assert.NotNull(ghostSwitch, "Fixture should contain switch_a in the fresh ghost world.");
            Assert.AreEqual(liveSwitch.IsActivated, ghostSwitch.IsActivated,
                "The switch's toggled state must match between the live run and the ghost replay.");
            Assert.AreEqual(expectedFinalSwitchState, ghostSwitch.IsActivated,
                "The switch ended up in an unexpected state for this hand-authored path.");
        }
    }
}
