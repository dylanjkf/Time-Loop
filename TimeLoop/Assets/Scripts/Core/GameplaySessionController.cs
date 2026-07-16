using System.Linq;
using TimeLoop.Levels;
using TimeLoop.Puzzle;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.Core
{
    /// <summary>
    /// Wires one gameplay scene's TimeLoopManager + PuzzleManager to GameManager's persistent
    /// systems — the only place gameplay results cross into cross-scene save/achievement/audio
    /// state. Sits in the Gameplay scene alongside TimeLoopManager and PuzzleManager; nothing else
    /// needs a direct reference to GameManager during actual play.
    /// </summary>
    public sealed class GameplaySessionController : MonoBehaviour
    {
        [SerializeField] private TimeLoopManager _timeLoopManager;
        [SerializeField] private PuzzleManager _puzzleManager;

        private LevelDefinition _currentLevel;
        private int _totalLoopsEverCompletedThisSession;

        public void StartLevel(LevelDefinition level)
        {
            _currentLevel = level;
            _timeLoopManager.LoadLevel(level);
            GameManager.Instance?.Audio?.PlayMusicForWorld(level.World);
        }

        private void OnEnable()
        {
            if (_timeLoopManager != null)
            {
                _timeLoopManager.Events.OnLoopEnd += HandleLoopEnd;
            }

            if (_puzzleManager != null)
            {
                _puzzleManager.OnLevelResult += HandleLevelResult;
            }
        }

        private void OnDisable()
        {
            if (_timeLoopManager != null)
            {
                _timeLoopManager.Events.OnLoopEnd -= HandleLoopEnd;
            }

            if (_puzzleManager != null)
            {
                _puzzleManager.OnLevelResult -= HandleLevelResult;
            }
        }

        private void HandleLoopEnd(RecordedTimeline timeline)
        {
            _totalLoopsEverCompletedThisSession++;
            GameManager.Instance?.Achievements.NotifyLoopCompleted(_totalLoopsEverCompletedThisSession);
        }

        private void HandleLevelResult(LevelResult result)
        {
            var manager = GameManager.Instance;
            if (manager == null || _currentLevel == null) return;

            manager.Profile.RecordLevelResult(result);

            foreach (var _ in _timeLoopManager.ActiveGhosts)
            {
                manager.Achievements.NotifyGhostCooperation();
            }

            var isOptimal = !result.UsedHint
                             && result.LoopsUsed <= _currentLevel.ParLoops
                             && result.TicksElapsed <= _currentLevel.ParTicks;

            var world4Levels = manager.LevelDatabase.LevelsInWorld(4);
            var world4Completed = world4Levels.Count(l => manager.Profile.IsLevelCompleted(l.LevelId));

            manager.Achievements.NotifyLevelCompleted(
                result,
                isOptimal,
                _timeLoopManager.SecondsRemaining,
                world4Completed,
                world4Levels.Count);

            manager.Profile.Persist();
        }
    }
}
