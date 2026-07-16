using System;
using System.Collections.Generic;
using TimeLoop.Generation;
using TimeLoop.Levels;

namespace TimeLoop.Daily
{
    /// <summary>
    /// Wordle-style daily mode: every player gets the same procedurally generated puzzle, derived
    /// from the UTC calendar date via DailySeedGenerator. One official completion counts per day;
    /// unlimited practice replays are still allowed afterward. See
    /// docs/TECHNICAL_ARCHITECTURE.md section 4.7.
    /// </summary>
    public sealed class DailyChallengeManager
    {
        private const int DailyDifficultyTier = 1;

        private readonly GeneratedLevelCache _cache = new GeneratedLevelCache();
        private readonly HashSet<int> _completedPuzzleNumbers = new HashSet<int>();

        public int TodaysPuzzleNumber => DailySeedGenerator.PuzzleNumberForDate(DateTime.UtcNow);

        public bool HasCompletedToday => _completedPuzzleNumbers.Contains(TodaysPuzzleNumber);

        public LevelDefinition GetTodaysLevel()
        {
            var seed = DailySeedGenerator.SeedForToday();
            var result = _cache.GetOrGenerate(seed, DailyDifficultyTier);
            return result.Verified ? result.Level : null;
        }

        /// <summary>Call once, the first time today's puzzle is completed — subsequent completions are practice only.</summary>
        public void MarkTodaysCompletion() => _completedPuzzleNumbers.Add(TodaysPuzzleNumber);

        /// <summary>Restores completed-day history from SaveData on load.</summary>
        public void RestoreCompletionHistory(IEnumerable<int> puzzleNumbers)
        {
            _completedPuzzleNumbers.Clear();
            foreach (var n in puzzleNumbers)
            {
                _completedPuzzleNumbers.Add(n);
            }
        }

        public IReadOnlyCollection<int> CompletionHistory => _completedPuzzleNumbers;
    }
}
