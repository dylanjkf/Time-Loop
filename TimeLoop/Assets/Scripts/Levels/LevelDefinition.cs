using System.Collections.Generic;
using System.Globalization;
using TimeLoop.Grid;
using TimeLoop.Puzzle;

namespace TimeLoop.Levels
{
    /// <summary>
    /// Immutable, typed view of a parsed .level asset. Stores the original source text so
    /// <see cref="BuildWorld"/> can hand back a brand-new, pristine GridWorld (with fresh entity
    /// instances) every time a level is (re)loaded — see docs/TECHNICAL_ARCHITECTURE.md section 6.
    /// </summary>
    public sealed class LevelDefinition
    {
        public string LevelId { get; }
        public string Name { get; }
        public int World { get; }
        public float LoopSeconds { get; }
        public int MaxTimelines { get; }
        public int ParLoops { get; }
        public int ParTicks { get; }
        public GridCoord SpawnPosition { get; }
        public int Width { get; }
        public int Height { get; }

        /// <summary>True for every level in this delivery — a fixed countdown always bounds a loop (docs/GAME_DESIGN_DOCUMENT.md section 4.2).</summary>
        public bool UsesFixedTimer => true;

        /// <summary>From World 2 onward, players may also end a loop early via the "Split Timeline" button.</summary>
        public bool AllowsManualSplit => World >= 2;

        private readonly string _sourceText;
        private readonly IReadOnlyList<string> _goalIds;

        internal LevelDefinition(
            string levelId,
            string sourceText,
            IReadOnlyDictionary<string, string> header,
            GridCoord spawnPosition,
            IReadOnlyList<string> goalIds,
            int width,
            int height)
        {
            LevelId = levelId;
            _sourceText = sourceText;
            _goalIds = goalIds;
            SpawnPosition = spawnPosition;
            Width = width;
            Height = height;

            Name = header.TryGetValue("NAME", out var name) ? name : levelId;
            World = ParseIntOr(header, "WORLD", 1);
            LoopSeconds = ParseFloatOr(header, "LOOP_SECONDS", 20f);
            MaxTimelines = ParseIntOr(header, "MAX_TIMELINES", 1);
            ParLoops = ParseIntOr(header, "PAR_LOOPS", MaxTimelines + 1);
            ParTicks = ParseIntOr(header, "PAR_TICKS", 200);
        }

        /// <summary>Constructs a fresh GridWorld with brand-new entity instances, ready for a new attempt.</summary>
        public GridWorld BuildWorld() => LevelParser.BuildWorld(_sourceText);

        public WinCondition BuildWinCondition() => new WinCondition(_goalIds, ParLoops, ParTicks);

        private static int ParseIntOr(IReadOnlyDictionary<string, string> header, string key, int fallback) =>
            header.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        private static float ParseFloatOr(IReadOnlyDictionary<string, string> header, string key, float fallback) =>
            header.TryGetValue(key, out var raw) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
    }
}
