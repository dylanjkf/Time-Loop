using System;
using System.Linq;
using TimeLoop.Interactables;
using TimeLoop.Levels;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.Puzzle
{
    /// <summary>
    /// Checks the active level's WinCondition every tick and computes the star rating once it's
    /// met. Wired up by TimeLoopManager.LoadLevel. See docs/TECHNICAL_ARCHITECTURE.md section 4.5.
    /// </summary>
    public sealed class PuzzleManager : MonoBehaviour
    {
        public event Action<LevelResult> OnLevelResult;

        private WinCondition _winCondition;
        private bool _hintUsed;

        public void Initialize(TimeLoopManager manager, LevelDefinition level)
        {
            _winCondition = level.BuildWinCondition();
            _hintUsed = false;
        }

        /// <summary>Called by the hint system (World 2+) — caps the level at one star (see GDD section 8).</summary>
        public void NotifyHintUsed() => _hintUsed = true;

        public bool CheckWinCondition(TimeLoopManager manager)
        {
            var world = manager.World;
            var playerPos = manager.PlayerActor.Position;

            foreach (var goalId in _winCondition.RequiredGoalIds)
            {
                var goal = world.GetEntity<Goal>(goalId);
                if (goal == null) return false;

                var occupiedByPlayer = goal.Position == playerPos;
                var occupiedByGhost = manager.ActiveGhosts.Any(g => g.Actor.Position == goal.Position);

                if (!occupiedByPlayer && !occupiedByGhost) return false;
            }

            return true;
        }

        public void OnLevelCompleted(TimeLoopManager manager)
        {
            var loopsUsed = manager.TotalLoopsUsedThisAttempt;
            var ticksElapsed = manager.CurrentTick;
            var stars = ComputeStars(loopsUsed, ticksElapsed);

            var result = new LevelResult(manager.CurrentLevel.LevelId, loopsUsed, ticksElapsed, _hintUsed, stars);
            OnLevelResult?.Invoke(result);
        }

        private StarRating ComputeStars(int loopsUsed, int ticksElapsed)
        {
            if (_hintUsed) return StarRating.One;
            if (loopsUsed <= _winCondition.ParLoops && ticksElapsed <= _winCondition.ParTicks) return StarRating.Three;
            return StarRating.Two;
        }
    }
}
