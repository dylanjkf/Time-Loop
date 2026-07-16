using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeLoop.Levels
{
    /// <summary>
    /// Indexes every campaign level by world and reports progress against the full campaign
    /// target counts documented in docs/ROADMAP.md (150+ levels; this delivery ships a
    /// representative, solver-verified slice — see that doc for exactly how many per world).
    /// Progression UI reads this rather than hardcoding level counts, so it stays correct as more
    /// .level files are added with zero code changes.
    /// </summary>
    public sealed class LevelDatabase
    {
        private static readonly Dictionary<int, int> FullCampaignTargetPerWorld = new Dictionary<int, int>
        {
            { 1, 20 },
            { 2, 30 },
            { 3, 50 },
            { 4, 50 }
        };

        private readonly Dictionary<int, List<LevelDefinition>> _levelsByWorld;

        public LevelDatabase(IEnumerable<LevelDefinition> levels)
        {
            _levelsByWorld = levels
                .GroupBy(l => l.World)
                .ToDictionary(g => g.Key, g => g.OrderBy(l => l.LevelId).ToList());
        }

        public IReadOnlyList<LevelDefinition> LevelsInWorld(int world) =>
            _levelsByWorld.TryGetValue(world, out var list) ? list : new List<LevelDefinition>();

        public LevelDefinition GetLevel(string levelId) =>
            _levelsByWorld.Values.SelectMany(l => l).FirstOrDefault(l => l.LevelId == levelId);

        public int TotalLevelCount => _levelsByWorld.Values.Sum(l => l.Count);

        public int FullCampaignTarget => FullCampaignTargetPerWorld.Values.Sum();

        public int FullCampaignTargetForWorld(int world) =>
            FullCampaignTargetPerWorld.TryGetValue(world, out var target) ? target : 0;

        /// <summary>Fraction (0-1) of the *shipped* levels in this world that a completed-level-id set has cleared.</summary>
        public float WorldProgress(int world, ISet<string> completedLevelIds)
        {
            var levels = LevelsInWorld(world);
            if (levels.Count == 0) return 0f;
            return levels.Count(l => completedLevelIds.Contains(l.LevelId)) / (float)levels.Count;
        }

        /// <summary>
        /// Soft world-gating (docs/GAME_DESIGN_DOCUMENT.md section 6): a world unlocks once the
        /// player has earned at least half the maximum possible stars in the previous world,
        /// rather than requiring 100% completion.
        /// </summary>
        public bool IsWorldUnlocked(int world, int totalStarsEarnedInPreviousWorld)
        {
            if (world <= 1) return true;

            var previousWorldLevelCount = LevelsInWorld(world - 1).Count;
            var requiredStars = (int)Math.Ceiling(previousWorldLevelCount * 3 * 0.5);
            return totalStarsEarnedInPreviousWorld >= requiredStars;
        }
    }
}
