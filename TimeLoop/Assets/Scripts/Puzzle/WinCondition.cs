using System.Collections.Generic;

namespace TimeLoop.Puzzle
{
    /// <summary>
    /// Win/star thresholds for a level, built from its .level header (PAR_LOOPS/PAR_TICKS) and
    /// its goal tile ids. A level is won once the live player (or, for multi-goal World 3+
    /// levels, any combination of the player and active ghosts) occupies every required goal
    /// simultaneously on the same tick.
    /// </summary>
    public sealed class WinCondition
    {
        public IReadOnlyList<string> RequiredGoalIds { get; }
        public int ParLoops { get; }
        public int ParTicks { get; }

        public WinCondition(IReadOnlyList<string> requiredGoalIds, int parLoops, int parTicks)
        {
            RequiredGoalIds = requiredGoalIds;
            ParLoops = parLoops;
            ParTicks = parTicks;
        }
    }
}
