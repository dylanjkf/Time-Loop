using System.Collections.Generic;
using TimeLoop.Grid;
using TimeLoop.Timeline;

namespace TimeLoop.Generation
{
    /// <summary>
    /// Step 1 of the generation pipeline (docs/PUZZLE_GENERATION.md): computes the exact N+1-loop
    /// solution for an EnvironmentBuilder layout analytically. The spine-and-branch geometry makes
    /// the minimal path trivial to derive directly (walk up the spine, then along the branch) — no
    /// search needed here. PuzzleSolver is reserved for independently re-verifying this
    /// construction afterward via genuine BFS, which is what actually satisfies the brief's
    /// "create an AI solver to verify every generated puzzle" requirement.
    /// </summary>
    public static class SolutionSynthesizer
    {
        public sealed class SynthesizedSolution
        {
            public readonly List<RecordedTimeline> GhostLoopTimelines = new List<RecordedTimeline>();
            public List<InputCommand> FinalLoopMoves = new List<InputCommand>();
        }

        public static SynthesizedSolution Synthesize(EnvironmentBuilder.GeneratedLayout layout)
        {
            var solution = new SynthesizedSolution();

            for (var i = 0; i < layout.PlatePositions.Count; i++)
            {
                var timeline = new RecordedTimeline(i);
                var tick = 0;

                var plate = layout.PlatePositions[i];
                var branchY = plate.Y;
                var sideDirection = plate.X > layout.Spawn.X ? Direction.Right : Direction.Left;

                for (var step = 0; step < branchY; step++)
                {
                    timeline.Append(tick++, new InputCommand(Direction.Up, false));
                }

                var branchLength = System.Math.Abs(plate.X - layout.Spawn.X);
                for (var step = 0; step < branchLength; step++)
                {
                    timeline.Append(tick++, new InputCommand(sideDirection, false));
                }

                solution.GhostLoopTimelines.Add(timeline);
            }

            var finalMoves = new List<InputCommand>();
            for (var step = 0; step < layout.GoalPosition.Y; step++)
            {
                finalMoves.Add(new InputCommand(Direction.Up, false));
            }

            solution.FinalLoopMoves = finalMoves;
            return solution;
        }
    }
}
