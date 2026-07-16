using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Core;
using TimeLoop.Grid;
using TimeLoop.Levels;
using TimeLoop.Puzzle;
using TimeLoop.Timeline;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeLoop.Tests.PlayMode
{
    /// <summary>
    /// End-to-end PlayMode test that drives the real TimeLoopManager/PuzzleManager through the
    /// canonical solution documented in Assets/Resources/Levels/World1/1-02.level's header comment:
    /// "Your ghost holds plate A; you press plate B yourself and walk straight through before
    /// releasing it." Loop 1 walks the player onto plate A and lets the fixed-duration loop time
    /// out (freezing that run into a ghost that will hold plate A for the rest of the attempt);
    /// loop 2 walks the player straight up through plate B and the now-open AND-gated door onto the
    /// goal. Asserts PuzzleManager.OnLevelResult fires with LoopsUsed == 2, matching the level's own
    /// PAR_LOOPS: 2.
    ///
    /// The .level file is read with System.IO.File.ReadAllText rather than Resources.Load, since a
    /// raw-text read doesn't depend on the TextAsset importer having run — see LevelLoader's
    /// Resources.LoadAll<TextAsset> usage for contrast. This assumes the test is run in-editor (or
    /// via Unity's -runTests batch mode), where Application.dataPath still points at the project's
    /// real Assets folder on disk; it will not find the source file in a build player.
    ///
    /// TimeLoopManager exposes no manual-tick/editor-only setup helper, so every field it needs
    /// (_gameSettings/_playerController/_puzzleManager) is wired via reflection onto a plain
    /// GameObject built at test time, matching the shape TimeLoopManager.LoadLevel expects — the
    /// same approach GhostReplayAccuracyTests and TimelineRecorderPlayModeTests use to avoid
    /// depending on SerializedObject (this asmdef references UnityEngine.TestRunner /
    /// UnityEditor.TestRunner only, not the UnityEditor assembly SerializedObject lives in).
    /// </summary>
    public sealed class LevelCompletionPlayModeTests
    {
        private GameObject _root;
        private TimeLoopManager _manager;
        private PlayerController _playerController;
        private PuzzleManager _puzzleManager;
        private GameSettings _gameSettings;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;

            _gameSettings = ScriptableObject.CreateInstance<GameSettings>();

            // GameSettings.TicksPerSecond is left at its authored default (12) so the level's
            // LOOP_SECONDS/PAR_TICKS header values keep meaning what the level designer wrote.
            // MaxTicksPerFrame — purely a per-frame safety cap against a "spiral of death", not a
            // gameplay value — is raised so the many idle ticks of loop 1's ~20-second countdown
            // don't require thousands of real Update() frames to drain. Combined with the elevated
            // Time.timeScale set in the test body below, this keeps the test's real wall-clock
            // duration to a handful of seconds instead of ~20+ real seconds per loop.
            SetPrivateField(_gameSettings, "_maxTicksPerFrame", 200);

            _root = new GameObject("TimeLoopManagerTestRoot");

            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(_root.transform);
            playerGo.AddComponent<ActorVisual>();
            _playerController = playerGo.AddComponent<PlayerController>();

            var puzzleGo = new GameObject("PuzzleManager");
            puzzleGo.transform.SetParent(_root.transform);
            _puzzleManager = puzzleGo.AddComponent<PuzzleManager>();

            _manager = _root.AddComponent<TimeLoopManager>();
            SetPrivateField(_manager, "_gameSettings", _gameSettings);
            SetPrivateField(_manager, "_playerController", _playerController);
            SetPrivateField(_manager, "_puzzleManager", _puzzleManager);
            // _ghostPrefab is intentionally left null: TimeLoopManager.SpawnGhostFromTimeline()
            // falls back to building a bare ActorVisual + GhostAgent GameObject itself when no
            // prefab is assigned — exactly the path this test exercises.
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            if (_root != null) Object.DestroyImmediate(_root);
            if (_gameSettings != null) Object.DestroyImmediate(_gameSettings);
        }

        [UnityTest]
        public IEnumerator TwoPlatesLevel_CanonicalGhostSolution_CompletesInTwoLoops()
        {
            var levelPath = Path.Combine(Application.dataPath, "Resources", "Levels", "World1", "1-02.level");
            Assert.IsTrue(File.Exists(levelPath), $"Expected to find the level file at {levelPath}.");

            var sourceText = File.ReadAllText(levelPath);
            var levelDefinition = LevelParser.Parse("1-02", sourceText);

            // 1-02's GRID (see the file's own comments) lays out as a vertical corridor at x=4 from
            // spawn up through two floor tiles, plate B, the AND door, and the goal, with plate A
            // reachable via a short side branch at y=1: spawn(4,0) -> (4,1) -> (3,1) -> plateA(2,1).
            var plateAWaypoint = new GridCoord(4, 1);
            var plateAPosition = new GridCoord(2, 1);
            var goalPosition = new GridCoord(4, 5);

            LevelResult result = null;
            _puzzleManager.OnLevelResult += r => result = r;

            _manager.LoadLevel(levelDefinition);

            // TimeLoopManager.Update() is driven by real Time.deltaTime; at the level's authored
            // LOOP_SECONDS: 20 and the default 12 ticks/sec, loop 1 alone would otherwise require
            // ~20 real seconds of frames just to time out. Restored in TearDown.
            Time.timeScale = 100f;

            // --- Loop 1: walk onto plate A, then let the fixed-duration loop expire so this
            // recording gets frozen into a ghost that holds plate A for the rest of the attempt. ---
            yield return MoveUntil(_playerController, Direction.Up, plateAWaypoint, maxFrames: 500);
            yield return MoveUntil(_playerController, Direction.Left, plateAPosition, maxFrames: 500);

            var waitFrames = 0;
            while (_manager.ActiveGhosts.Count == 0)
            {
                yield return null;
                waitFrames++;
                Assert.Less(waitFrames, 5000,
                    "Loop 1 never timed out into a ghost — has LOOP_SECONDS or GameSettings.TicksPerSecond changed?");
            }

            Assert.AreEqual(1, _manager.CurrentLoopIndex, "Loop 1 should have ended and loop 2 (index 1) begun.");
            Assert.AreEqual(1, _manager.ActiveGhosts.Count, "Exactly one ghost (holding plate A) should now be active.");

            // --- Loop 2: walk straight up through plate B and the now-open AND door onto the goal,
            // while the ghost spawned above holds plate A the whole time. ---
            yield return MoveUntil(_playerController, Direction.Up, goalPosition, maxFrames: 1000);

            waitFrames = 0;
            while (result == null)
            {
                yield return null;
                waitFrames++;
                Assert.Less(waitFrames, 500, "PuzzleManager.OnLevelResult never fired after the player reached the goal tile.");
            }

            Assert.AreEqual(levelDefinition.LevelId, result.LevelId);
            Assert.AreEqual(2, result.LoopsUsed,
                "The documented solution (ghost holds plate A, then press plate B and walk through) " +
                "should take exactly 2 loops, matching the level's own PAR_LOOPS: 2.");
        }

        /// <summary>
        /// Re-queues <paramref name="direction"/> into the live player's LiveInputProvider every
        /// frame until its GridActor reaches <paramref name="target"/>. This is safe even though
        /// TimeLoopManager.Update() may process several ticks per frame (bounded by
        /// GameSettings.MaxTicksPerFrame): LiveInputProvider is a single-slot buffer that clears
        /// itself the instant a tick consumes it, so at most one real step is ever taken per queued
        /// call — any extra ticks batched into the same frame simply consume Idle. Any door-timing
        /// dependency (a plate must be active for a full tick before the door counts as open) is
        /// therefore never racy: activation always happens at the end of the Step that triggered it,
        /// strictly before the next tick's movement is resolved.
        /// </summary>
        private static IEnumerator MoveUntil(PlayerController player, Direction direction, GridCoord target, int maxFrames)
        {
            var frames = 0;
            while (player.Actor.Position != target)
            {
                player.InputProvider.QueueDirection(direction);
                yield return null;
                frames++;
                Assert.Less(frames, maxFrames, $"Player did not reach {target} moving {direction} within {maxFrames} frames.");
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name} — has it been renamed?");
            field.SetValue(target, value);
        }
    }
}
