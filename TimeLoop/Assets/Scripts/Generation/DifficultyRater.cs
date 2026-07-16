namespace TimeLoop.Generation
{
    /// <summary>
    /// Step 5 of the generation pipeline: a simple, transparent weighted score rather than a
    /// black box, so the number shown to players (and used to pick the next Infinite Mode puzzle)
    /// is explainable. See docs/PUZZLE_GENERATION.md.
    /// </summary>
    public static class DifficultyRater
    {
        public static int Rate(int plateCount, int width, int height, int finalLoopTicks)
        {
            var timelineComplexity = plateCount * 25;
            var spatialComplexity = (width * height) / 4;
            var precisionComplexity = finalLoopTicks;

            return timelineComplexity + spatialComplexity + precisionComplexity;
        }
    }
}
