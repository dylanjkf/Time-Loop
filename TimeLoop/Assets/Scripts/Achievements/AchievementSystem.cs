using System;
using System.Linq;
using TimeLoop.Puzzle;
using TimeLoop.Save;

namespace TimeLoop.Achievements
{
    /// <summary>
    /// Thin rules layer over PlayerProfile: watches gameplay events and unlocks the fixed
    /// AchievementDatabase entries when their conditions are met. Not a generic event bus — each
    /// Notify* method encodes exactly one achievement's trigger condition.
    /// </summary>
    public class AchievementSystem
    {
        public event Action<AchievementDefinition> OnAchievementUnlocked;

        private readonly PlayerProfile _profile;

        public AchievementSystem(PlayerProfile profile)
        {
            _profile = profile;
        }

        public void NotifyLoopCompleted(int totalLoopsEverCompletedByPlayer)
        {
            if (totalLoopsEverCompletedByPlayer == 1)
            {
                Unlock("first_timeline");
            }
        }

        public void NotifyLevelCompleted(
            LevelResult result,
            bool isOptimalSolution,
            float secondsRemainingAtCompletion,
            int totalWorld4LevelsCompleted,
            int totalWorld4LevelCount)
        {
            if (isOptimalSolution)
            {
                Unlock("perfect_prediction");
            }

            if (secondsRemainingAtCompletion < 1f)
            {
                Unlock("split_second");
            }

            if (totalWorld4LevelsCompleted >= totalWorld4LevelCount)
            {
                Unlock("time_master");
            }
        }

        public void NotifyGhostCooperation()
        {
            _profile.Raw.GhostCooperationCount++;

            if (_profile.Raw.GhostCooperationCount >= 100)
            {
                Unlock("future_self");
            }

            _profile.Persist();
        }

        public void NotifyDailyStreak(int consecutiveDays)
        {
            if (consecutiveDays >= 7)
            {
                Unlock("streak");
            }
        }

        private void Unlock(string achievementId)
        {
            if (_profile.Raw.UnlockedAchievementIds.Contains(achievementId))
            {
                return;
            }

            _profile.Raw.UnlockedAchievementIds.Add(achievementId);

            var definition = AchievementDatabase.All.First(a => a.Id == achievementId);
            OnAchievementUnlocked?.Invoke(definition);

            _profile.Persist();
        }
    }
}
