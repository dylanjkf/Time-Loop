using System.Collections.Generic;
using System.Text;
using TimeLoop.Grid;

namespace TimeLoop.Generation
{
    /// <summary>
    /// Builds the "N simultaneous plates behind a gated door" template family (see
    /// docs/PUZZLE_GENERATION.md for why this is the shipped v1 generation family and what's
    /// planned to broaden it) as raw .level GRID/LEGEND text — the exact same format
    /// hand-authored levels use, so LevelParser can't tell a generated level apart from a
    /// handcrafted one. A single vertical spine corridor with N alternating side-branch alcoves
    /// is trivially guaranteed connected and solvable by construction: SolutionSynthesizer
    /// computes the exact N+1-loop plan from the same geometry, and PuzzleSolver independently
    /// re-verifies it via search before a level is ever shown to a player.
    /// </summary>
    public static class EnvironmentBuilder
    {
        public sealed class GeneratedLayout
        {
            public int Width;
            public int Height;
            public string GridText;
            public string LegendText;
            public GridCoord Spawn;
            public List<GridCoord> PlatePositions;
            public GridCoord DoorPosition;
            public GridCoord GoalPosition;
        }

        public static GeneratedLayout Build(int plateCount, int difficultyTier)
        {
            var branchLength = 2 + difficultyTier;
            var height = 2 * plateCount + 5;
            var width = 2 * branchLength + 5;
            var centerX = width / 2;

            var grid = new char[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                grid[x, y] = '#';

            for (var y = 0; y < height; y++)
            {
                grid[centerX, y] = '.';
            }

            var spawn = new GridCoord(centerX, 0);
            grid[centerX, 0] = 'S';

            var goal = new GridCoord(centerX, height - 1);
            grid[centerX, height - 1] = 'G';

            var door = new GridCoord(centerX, height - 2);
            grid[centerX, height - 2] = 'D';

            var platePositions = new List<GridCoord>();
            var groupIds = new List<string>();

            for (var i = 0; i < plateCount; i++)
            {
                var branchY = 2 + i * 2;
                var side = i % 2 == 0 ? -1 : 1;

                for (var step = 1; step <= branchLength; step++)
                {
                    grid[centerX + side * step, branchY] = '.';
                }

                var plateX = centerX + side * branchLength;
                var plateChar = (char)('1' + i);
                grid[plateX, branchY] = plateChar;

                platePositions.Add(new GridCoord(plateX, branchY));
                groupIds.Add("p" + (i + 1));
            }

            var gridText = new StringBuilder();
            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    gridText.Append(grid[x, y]);
                }

                gridText.Append('\n');
            }

            var legend = new StringBuilder("S=spawn G=goal ");
            for (var i = 0; i < plateCount; i++)
            {
                legend.Append((char)('1' + i)).Append('=').Append("plate:").Append(groupIds[i]).Append(' ');
            }

            legend.Append('D').Append('=').Append("door:and:").Append(string.Join("+", groupIds));

            return new GeneratedLayout
            {
                Width = width,
                Height = height,
                GridText = gridText.ToString().TrimEnd('\n'),
                LegendText = legend.ToString(),
                Spawn = spawn,
                PlatePositions = platePositions,
                DoorPosition = door,
                GoalPosition = goal
            };
        }
    }
}
