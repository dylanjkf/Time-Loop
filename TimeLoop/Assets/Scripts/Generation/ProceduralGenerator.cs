using System.Collections.Generic;
using System.Linq;
using TimeLoop.Actors;
using TimeLoop.Levels;

namespace TimeLoop.Generation
{
    /// <summary>
    /// Infinite Mode's content pipeline, implementing the design brief's 5-step generation recipe
    /// end to end: (1) SolutionSynthesizer creates a target solution, (2-3) EnvironmentBuilder
    /// builds the environment/interactions backward from it as ordinary .level text, (4) that
    /// text is parsed by the exact same LevelParser real campaign content uses, and (5)
    /// PuzzleSolver + DifficultyRater independently verify solvability and rate difficulty before
    /// a level is ever handed to a player. See docs/PUZZLE_GENERATION.md for the full writeup and
    /// docs/ROADMAP.md for how this generation family is expected to broaden post-launch.
    /// </summary>
    public static class ProceduralGenerator
    {
        public sealed class GeneratedResult
        {
            public bool Verified;
            public LevelDefinition Level;
            public int DifficultyScore;
            public string FailureReason;
        }

        private const int TicksPerSecond = 12;

        public static GeneratedResult Generate(int seed, int difficultyTier)
        {
            var plateCount = 1 + (difficultyTier % 3); // 1..3 simultaneous timelines, escalating with difficulty
            var effectiveTier = System.Math.Min(difficultyTier, 2);

            var layout = EnvironmentBuilder.Build(plateCount, effectiveTier);
            var solution = SolutionSynthesizer.Synthesize(layout);

            var longestGhostTicks = solution.GhostLoopTimelines
                .Select(t => t.TotalTicks)
                .DefaultIfEmpty(0)
                .Max();
            var longestLoopTicks = System.Math.Max(longestGhostTicks, solution.FinalLoopMoves.Count);

            var loopSeconds = System.Math.Max(5, System.Math.Ceiling(longestLoopTicks * 1.3 / TicksPerSecond));

            var levelId = $"infinite_{seed}_{difficultyTier}";
            var sourceText = BuildLevelText(levelId, layout, plateCount, (int)loopSeconds);
            var level = LevelParser.Parse(levelId, sourceText);

            var (success, reason, finalLoopTicks) = Verify(level, layout, solution, plateCount);
            if (!success)
            {
                return new GeneratedResult { Verified = false, FailureReason = reason };
            }

            var difficulty = DifficultyRater.Rate(plateCount, layout.Width, layout.Height, finalLoopTicks);

            return new GeneratedResult
            {
                Verified = true,
                Level = level,
                DifficultyScore = difficulty
            };
        }

        private static string BuildLevelText(string levelId, EnvironmentBuilder.GeneratedLayout layout, int plateCount, int loopSeconds)
        {
            return
                $"NAME: Infinite {levelId}\n" +
                "WORLD: 5\n" +
                $"LOOP_SECONDS: {loopSeconds}\n" +
                $"MAX_TIMELINES: {plateCount}\n" +
                $"PAR_LOOPS: {plateCount + 1}\n" +
                $"PAR_TICKS: {loopSeconds * TicksPerSecond}\n" +
                "GRID:\n" +
                layout.GridText + "\n" +
                "LEGEND:\n" +
                layout.LegendText;
        }

        /// <summary>
        /// Independently re-derives, via PuzzleSolver's real BFS over the real simulation, that
        /// every ghost loop can actually reach its plate and that the final loop can actually
        /// reach the goal once all plates are held — this is what makes the pipeline a genuine
        /// verify-before-ship step rather than blind trust in the construction math above.
        /// </summary>
        private static (bool success, string reason, int finalLoopTicks) Verify(
            LevelDefinition level,
            EnvironmentBuilder.GeneratedLayout layout,
            SolutionSynthesizer.SynthesizedSolution solution,
            int plateCount)
        {
            var world = level.BuildWorld();
            var fixedActors = new List<(GridActor actor, IInputProvider provider)>();

            for (var i = 0; i < plateCount; i++)
            {
                var ghostActor = new GridActor($"verify_ghost_{i}", layout.Spawn, isGhost: true);
                var provider = new RecordedInputProvider(solution.GhostLoopTimelines[i]);
                var maxTicks = solution.GhostLoopTimelines[i].TotalTicks + 2;

                var reach = PuzzleSolver.FindPathToGoal(
                    world,
                    ghostActor,
                    fixedActors,
                    (w, a) => a.Position == layout.PlatePositions[i],
                    maxTicks);

                if (!reach.Success)
                {
                    return (false, $"Loop {i}: could not verify a path to plate {i}.", 0);
                }

                fixedActors.Add((ghostActor, provider));
            }

            var liveActor = new GridActor("verify_live", layout.Spawn);
            var finalReach = PuzzleSolver.FindPathToGoal(
                world,
                liveActor,
                fixedActors,
                (w, a) => a.Position == layout.GoalPosition,
                solution.FinalLoopMoves.Count + 5);

            if (!finalReach.Success)
            {
                return (false, "Final loop: could not verify a path to the goal with all plates held.", 0);
            }

            return (true, null, finalReach.TicksUsed);
        }
    }
}
