using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Core;
using TimeLoop.Grid;
using TimeLoop.Timeline;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeLoop.Tests.PlayMode
{
    /// <summary>
    /// Verifies that TimelineRecorder + LiveInputProvider together produce a RecordedTimeline that
    /// is an exact, in-order transcript of what was queued each tick — no drops, no reordering, no
    /// spurious extra frames. This is the recording half of the ghost pipeline: GhostReplayAccuracyTests
    /// covers the playback half (RecordedInputProvider driving a second GridActor to the same result).
    /// </summary>
    public sealed class TimelineRecorderPlayModeTests
    {
        private GameObject _playerGo;
        private PlayerController _playerController;
        private GameSettings _gameSettings;

        [SetUp]
        public void SetUp()
        {
            // Build the same two pieces TimeLoopManager.LoadLevel wires together for the live
            // player — a GameSettings asset instance and a PlayerController on a fresh GameObject
            // (which requires an ActorVisual sibling component) — without needing a full
            // TimeLoopManager, since this test targets the TimelineRecorder/LiveInputProvider
            // contract directly rather than the frame-rate-dependent Update() tick loop.
            _gameSettings = ScriptableObject.CreateInstance<GameSettings>();

            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<ActorVisual>();
            _playerController = _playerGo.AddComponent<PlayerController>();
            _playerController.Initialize("player", new GridCoord(0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            if (_gameSettings != null) Object.DestroyImmediate(_gameSettings);
        }

        [UnityTest]
        public IEnumerator RecordedTimeline_ExactlyMatchesQueuedInputSequence_InOrder()
        {
            var recorder = new TimelineRecorder();
            recorder.BeginNewRecording(0);

            // A hand-scripted sequence covering plain moves, an idle tick, an interact-only tick,
            // and a move+interact tick on the same frame — the full vocabulary of InputCommand.
            var scripted = new List<InputCommand>
            {
                new InputCommand(Direction.Up, false),
                new InputCommand(Direction.Up, false),
                new InputCommand(Direction.Right, false),
                InputCommand.Idle,
                new InputCommand(Direction.None, true),
                new InputCommand(Direction.Down, false),
                new InputCommand(Direction.Left, true),
            };

            for (var tick = 0; tick < scripted.Count; tick++)
            {
                var scriptedCommand = scripted[tick];

                // Queue exactly like PlayerController would in response to input this render frame.
                if (scriptedCommand.Move != Direction.None)
                {
                    _playerController.InputProvider.QueueDirection(scriptedCommand.Move);
                }

                if (scriptedCommand.Interact)
                {
                    _playerController.InputProvider.QueueInteract();
                }

                // Advance one real frame, matching how TimeLoopManager's tick loop is driven by
                // Update() rather than by the recorder itself.
                yield return null;

                // Consume + record exactly as TimeLoopManager.RunTick does:
                //   var playerCommand = _playerController.InputProvider.GetCommand(CurrentTick);
                //   _recorder.RecordTick(CurrentTick, playerCommand);
                var consumedCommand = _playerController.InputProvider.GetCommand(tick);
                recorder.RecordTick(tick, consumedCommand);
            }

            var timeline = recorder.Current;

            Assert.AreEqual(scripted.Count, timeline.Frames.Count,
                "Every simulated tick should produce exactly one recorded frame.");
            Assert.AreEqual(scripted.Count, timeline.TotalTicks,
                "TotalTicks should equal the highest recorded tick + 1.");

            for (var i = 0; i < scripted.Count; i++)
            {
                Assert.AreEqual(i, timeline.Frames[i].Tick, $"Frame {i} should be tagged with tick index {i}.");
                Assert.AreEqual(scripted[i], timeline.Frames[i].Command,
                    $"Frame {i}'s recorded command should exactly match what was queued for that tick.");
                Assert.AreEqual(scripted[i], timeline.GetCommandAtTick(i),
                    $"GetCommandAtTick({i}) should agree with Frames[{i}].");
            }

            var expectedMoveSequence = scripted.Where(c => c.Move != Direction.None).Select(c => c.Move).ToList();
            var actualMoveSequence = timeline.MoveSequence().ToList();
            CollectionAssert.AreEqual(expectedMoveSequence, actualMoveSequence,
                "MoveSequence() should yield only the queued directions, in the exact order they were queued, " +
                "skipping idle and interact-only ticks.");

            var expectedMoveCount = scripted.Count(c => !c.IsIdle);
            Assert.AreEqual(expectedMoveCount, timeline.MoveCount,
                "MoveCount should count every tick that was a real move and/or interact, not just direction moves.");
        }

        [Test]
        public void RecordedTimeline_GetCommandAtTick_HoldsIdleForeverBeyondRecordedRange()
        {
            // This is the exact contract RecordedInputProvider (and therefore every ghost) relies
            // on: once past the recording's length, a ghost holds its final resting tile forever.
            var recorder = new TimelineRecorder();
            recorder.BeginNewRecording(0);
            recorder.RecordTick(0, new InputCommand(Direction.Up, false));
            recorder.RecordTick(1, new InputCommand(Direction.Right, false));

            var timeline = recorder.Current;

            Assert.AreEqual(2, timeline.Frames.Count);
            Assert.AreEqual(InputCommand.Idle, timeline.GetCommandAtTick(2), "One tick past the end should be Idle.");
            Assert.AreEqual(InputCommand.Idle, timeline.GetCommandAtTick(1000), "Far past the end should still be Idle.");
            Assert.AreEqual(InputCommand.Idle, timeline.GetCommandAtTick(-1), "A negative tick should be Idle, not throw.");
        }
    }
}
