using System.Collections.Generic;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Generation;
using TimeLoop.Grid;

namespace TimeLoop.Tests.EditMode
{
    [TestFixture]
    public class PuzzleSolverTests
    {
        private static GridWorld MakeCorridor(int width, int height, params GridCoord[] walls)
        {
            var tiles = new TileType[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                tiles[x, y] = TileType.Empty;

            foreach (var wall in walls)
            {
                tiles[wall.X, wall.Y] = TileType.Wall;
            }

            return new GridWorld(width, height, tiles);
        }

        [Test]
        public void FindPathToGoal_StraightCorridorNoObstacles_SucceedsWithMinimalMoveCount()
        {
            // A single-row, 5-tile-wide corridor with no obstacles: (0,0) .. (4,0), all Empty.
            var world = MakeCorridor(5, 1);
            var spawn = new GridCoord(0, 0);
            var goal = new GridCoord(4, 0);
            var actor = new GridActor("player", spawn);

            var fixedActors = new List<(GridActor actor, IInputProvider provider)>();

            bool IsGoalReached(GridWorld w, GridActor a) => a.Position == goal;

            var result = PuzzleSolver.FindPathToGoal(world, actor, fixedActors, IsGoalReached, maxTicks: 10);

            Assert.IsTrue(result.Success);

            // Manhattan distance from (0,0) to (4,0) is |4-0| + |0-0| = 4, and with no obstacles in
            // an open 1-wide corridor the only way to close that distance is 4 single-step Right
            // moves, so the shortest solution is exactly 4 ticks.
            Assert.AreEqual(4, result.Moves.Count);
            Assert.AreEqual(4, result.TicksUsed);
        }

        [Test]
        public void FindPathToGoal_GoalWalledOff_FailsWithinSmallTickBudget()
        {
            // A 3-tile-wide single row with a Wall splitting it in half: Empty(0,0) | Wall(1,0) |
            // Empty(2,0). With only one row, there is no way around the wall, so the goal at (2,0)
            // can never be reached from spawn (0,0).
            var wall = new GridCoord(1, 0);
            var world = MakeCorridor(3, 1, wall);
            var spawn = new GridCoord(0, 0);
            var goal = new GridCoord(2, 0);
            var actor = new GridActor("player", spawn);

            var fixedActors = new List<(GridActor actor, IInputProvider provider)>();

            bool IsGoalReached(GridWorld w, GridActor a) => a.Position == goal;

            var result = PuzzleSolver.FindPathToGoal(world, actor, fixedActors, IsGoalReached, maxTicks: 20);

            Assert.IsFalse(result.Success);
        }
    }
}
