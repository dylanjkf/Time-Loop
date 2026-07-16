using System.Collections.Generic;

namespace TimeLoop.Achievements
{
    /// <summary>Fixed, hardcoded catalog of every achievement in the game — see docs/GAME_DESIGN_DOCUMENT.md for design intent.</summary>
    public static class AchievementDatabase
    {
        public static IReadOnlyList<AchievementDefinition> All { get; } = new List<AchievementDefinition>
        {
            new AchievementDefinition("first_timeline", "First Timeline", "Complete your first loop."),
            new AchievementDefinition("future_self", "Future Self", "Cooperate with a ghost 100 times."),
            new AchievementDefinition("perfect_prediction", "Perfect Prediction", "Complete a level with zero wasted moves."),
            new AchievementDefinition("time_master", "Time Master", "Complete every Expert (World 4) level."),
            new AchievementDefinition("split_second", "Split Second", "Solve a level with under 1 second of the countdown remaining."),
            new AchievementDefinition("streak", "Streak", "Complete 7 consecutive Daily Challenges.")
        };
    }
}
