using System.Globalization;
using TimeLoop.Levels;

namespace TimeLoop.Monetization
{
    /// <summary>
    /// Central gating logic for what's accessible in the free tier vs. behind the
    /// <see cref="IAPManager.PremiumProductId"/> unlock. Pure/static and stateless — callers
    /// supply the current entitlement flag (typically IAPManager.IsPremiumUnlocked) on every call
    /// rather than this class caching anything, so there's a single source of truth.
    ///
    /// Free tier: all of World 1, plus World 2 levels numbered 30 or lower (by the numeric
    /// suffix of LevelId, e.g. "2-05" -> 5). Everything else — the rest of World 2, and every
    /// later world — requires the premium unlock.
    /// </summary>
    public static class PremiumUnlock
    {
        /// <summary>Highest World-2 level number (the numeric suffix of LevelId) still included in the free tier.</summary>
        private const int FreeWorld2LevelCap = 30;

        public static bool IsLevelAccessible(LevelDefinition level, bool isPremiumUnlocked)
        {
            // Premium unlocks everything, so short-circuit before touching LevelId at all.
            if (isPremiumUnlocked)
            {
                return true;
            }

            if (level == null)
            {
                return false;
            }

            if (level.World == 1)
            {
                return true;
            }

            if (level.World == 2)
            {
                // Defensive parse: a malformed/unexpected LevelId is treated as requiring
                // premium rather than throwing or accidentally granting free access.
                return TryParseLevelNumberSuffix(level.LevelId, out var levelNumber)
                    && levelNumber <= FreeWorld2LevelCap;
            }

            // World 3+ is entirely premium-gated.
            return false;
        }

        /// <summary>Infinite Mode is a premium-only feature.</summary>
        public static bool IsInfiniteModeAccessible(bool isPremiumUnlocked) => isPremiumUnlocked;

        /// <summary>Daily Challenge is always free for everyone, regardless of premium status.</summary>
        public static bool IsDailyChallengeAccessible() => true;

        /// <summary>
        /// Parses the numeric suffix after the last '-' in a LevelId like "2-05" (-> 5).
        /// Returns false (rather than throwing) for null/empty input, a missing '-', a trailing
        /// '-' with nothing after it, or a non-numeric suffix.
        /// </summary>
        private static bool TryParseLevelNumberSuffix(string levelId, out int levelNumber)
        {
            levelNumber = 0;

            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            var dashIndex = levelId.LastIndexOf('-');
            if (dashIndex < 0 || dashIndex == levelId.Length - 1)
            {
                return false;
            }

            var suffixText = levelId.Substring(dashIndex + 1);
            return int.TryParse(suffixText, NumberStyles.Integer, CultureInfo.InvariantCulture, out levelNumber);
        }
    }
}
