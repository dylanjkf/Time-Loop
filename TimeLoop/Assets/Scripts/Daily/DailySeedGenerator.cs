using System;

namespace TimeLoop.Daily
{
    /// <summary>Derives a deterministic seed from the UTC calendar date so every player worldwide gets the same puzzle on the same day.</summary>
    public static class DailySeedGenerator
    {
        private static readonly DateTime Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int SeedForDate(DateTime utcDate) => (int)(utcDate.Date - Epoch).TotalDays;

        public static int SeedForToday() => SeedForDate(DateTime.UtcNow);

        /// <summary>The "TIME LOOP #N" puzzle number shown in share text — day 1 is the epoch date.</summary>
        public static int PuzzleNumberForDate(DateTime utcDate) => SeedForDate(utcDate) + 1;
    }
}
