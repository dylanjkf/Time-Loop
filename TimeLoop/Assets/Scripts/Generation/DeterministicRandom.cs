namespace TimeLoop.Generation
{
    /// <summary>
    /// A small, dependency-free xorshift32 PRNG. Used instead of System.Random or
    /// UnityEngine.Random because procedural generation must produce bit-identical output across
    /// iOS/Android/Editor for the same seed — required for Daily Challenge, where every player
    /// worldwide must get the same puzzle — and neither built-in generator is contractually
    /// guaranteed stable across .NET runtimes/versions/platforms.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(int seed)
        {
            var s = unchecked((uint)seed);
            _state = s == 0 ? 1u : s;
        }

        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>Uniform in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public bool NextBool() => (NextUInt() & 1) == 0;
    }
}
