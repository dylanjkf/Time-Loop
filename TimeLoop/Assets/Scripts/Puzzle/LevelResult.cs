namespace TimeLoop.Puzzle
{
    public enum StarRating
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 3
    }

    /// <summary>Outcome of one completed level attempt — the input to star scoring, save progress, and achievements.</summary>
    public sealed class LevelResult
    {
        public string LevelId { get; }
        public int LoopsUsed { get; }
        public int TicksElapsed { get; }
        public bool UsedHint { get; }
        public StarRating Stars { get; }

        public LevelResult(string levelId, int loopsUsed, int ticksElapsed, bool usedHint, StarRating stars)
        {
            LevelId = levelId;
            LoopsUsed = loopsUsed;
            TicksElapsed = ticksElapsed;
            UsedHint = usedHint;
            Stars = stars;
        }
    }
}
