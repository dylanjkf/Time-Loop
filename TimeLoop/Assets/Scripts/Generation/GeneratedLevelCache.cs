using System.Collections.Generic;

namespace TimeLoop.Generation
{
    /// <summary>
    /// In-memory cache of generated+verified levels, keyed by (seed, difficultyTier), so Infinite
    /// Mode and Daily Challenge never regenerate the same puzzle twice in a session. If a seed
    /// fails verification (should be rare — the shipped template family is connected by
    /// construction) a few deterministic seed variants are tried before giving up, so a single
    /// unlucky seed can't surface a broken puzzle to a player.
    /// </summary>
    public sealed class GeneratedLevelCache
    {
        private const int MaxVerificationRetries = 3;

        private readonly Dictionary<(int seed, int tier), ProceduralGenerator.GeneratedResult> _cache =
            new Dictionary<(int, int), ProceduralGenerator.GeneratedResult>();

        public ProceduralGenerator.GeneratedResult GetOrGenerate(int seed, int difficultyTier)
        {
            var key = (seed, difficultyTier);
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            ProceduralGenerator.GeneratedResult result = null;
            for (var attempt = 0; attempt < MaxVerificationRetries; attempt++)
            {
                result = ProceduralGenerator.Generate(seed + attempt, difficultyTier);
                if (result.Verified) break;
            }

            _cache[key] = result;
            return result;
        }

        public void Clear() => _cache.Clear();
    }
}
