using System;
using System.Collections.Generic;
using System.Linq;
using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Generation
{
    /// <summary>
    /// The AI solver required by the design brief: a genuine breadth-first search over reachable
    /// (tick, position) states that drives the *real* GridWorld.Step/MoveResolver at every node —
    /// not an approximate model — so "the solver says this is solvable" and "the game says this
    /// is solvable" can never disagree. Used two ways:
    ///  1. By ProceduralGenerator, to verify every generated level actually has a valid solution
    ///     before it's ever shown to a player, and to find the minimal solution for difficulty
    ///     rating and PAR_LOOPS/PAR_TICKS.
    ///  2. As a content-QA tool for hand-authored levels (docs/TESTING_REPORT.md).
    ///
    /// Scope: solves one actor's path per call, given every other actor's future inputs are
    /// already fixed (ghosts are deterministic recordings, so this is exactly what's needed to
    /// verify a single loop's approach once earlier loops are decided). It does not, by itself,
    /// search the combinatorial space of "what should every earlier loop's recording be" —
    /// ProceduralGenerator supplies those directly, by construction, from
    /// <see cref="SolutionSynthesizer"/>, and this solver independently verifies the result.
    /// </summary>
    public static class PuzzleSolver
    {
        public sealed class SearchResult
        {
            public bool Success;
            public List<InputCommand> Moves = new List<InputCommand>();
            public int TicksUsed;
        }

        private sealed class SearchNode
        {
            public readonly int Tick;
            public readonly GridCoord Position;
            public readonly SearchNode Parent;
            public readonly InputCommand CommandTaken;
            public readonly FullSnapshot Snapshot;

            public SearchNode(int tick, GridCoord position, SearchNode parent, InputCommand commandTaken, FullSnapshot snapshot)
            {
                Tick = tick;
                Position = position;
                Parent = parent;
                CommandTaken = commandTaken;
                Snapshot = snapshot;
            }
        }

        private sealed class FullSnapshot
        {
            public GridWorldState WorldState;
            public Dictionary<GridActor, GridCoord> ActorPositions;

            public static FullSnapshot Capture(GridWorld world, IEnumerable<GridActor> actors)
            {
                return new FullSnapshot
                {
                    WorldState = GridWorldState.Capture(world),
                    ActorPositions = actors.ToDictionary(a => a, a => a.Position)
                };
            }

            public void Restore(GridWorld world)
            {
                WorldState.Restore(world);
                foreach (var kvp in ActorPositions)
                {
                    kvp.Key.SetPosition(kvp.Value);
                }
            }
        }

        private static readonly (Direction dir, bool interact)[] CandidateCommands =
        {
            (Direction.None, false),
            (Direction.Up, false),
            (Direction.Down, false),
            (Direction.Left, false),
            (Direction.Right, false),
            (Direction.None, true),
            (Direction.Up, true),
            (Direction.Down, true),
            (Direction.Left, true),
            (Direction.Right, true),
        };

        /// <summary>
        /// Breadth-first search for the shortest tick-sequence that brings <paramref name="searchActor"/>
        /// to a tile satisfying <paramref name="isGoalReached"/>, within <paramref name="maxTicks"/>,
        /// given every entry in <paramref name="fixedActors"/> (typically already-recorded ghosts)
        /// supplies its own command deterministically each tick. The world is left exactly as it
        /// was found once the search returns (success or not) — this may be called on a scratch
        /// world built solely for verification, or defensively on a live one.
        /// </summary>
        public static SearchResult FindPathToGoal(
            GridWorld world,
            GridActor searchActor,
            IReadOnlyList<(GridActor actor, IInputProvider provider)> fixedActors,
            Func<GridWorld, GridActor, bool> isGoalReached,
            int maxTicks,
            int startTick = 0)
        {
            var allActors = fixedActors.Select(f => f.actor).Append(searchActor).ToList();
            var startSnapshot = FullSnapshot.Capture(world, allActors);

            var visited = new HashSet<(int tick, GridCoord pos)> { (startTick, searchActor.Position) };
            var queue = new Queue<SearchNode>();
            var startNode = new SearchNode(startTick, searchActor.Position, null, InputCommand.Idle, startSnapshot);
            queue.Enqueue(startNode);

            SearchNode goalNode = null;

            if (isGoalReached(world, searchActor))
            {
                goalNode = startNode;
            }

            while (goalNode == null && queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.Tick - startTick >= maxTicks) continue;

                foreach (var (dir, interact) in CandidateCommands)
                {
                    node.Snapshot.Restore(world);

                    var command = new InputCommand(dir, interact);
                    var orderedInputs = new List<ActorTickInput>();
                    foreach (var (fixedActor, provider) in fixedActors)
                    {
                        orderedInputs.Add(new ActorTickInput(fixedActor, provider.GetCommand(node.Tick)));
                    }
                    orderedInputs.Add(new ActorTickInput(searchActor, command));

                    world.Step(orderedInputs, node.Tick);

                    var newTick = node.Tick + 1;
                    var newPos = searchActor.Position;
                    var key = (newTick, newPos);

                    if (visited.Contains(key)) continue;
                    visited.Add(key);

                    var childSnapshot = FullSnapshot.Capture(world, allActors);
                    var childNode = new SearchNode(newTick, newPos, node, command, childSnapshot);

                    if (isGoalReached(world, searchActor))
                    {
                        goalNode = childNode;
                        break;
                    }

                    queue.Enqueue(childNode);
                }
            }

            startSnapshot.Restore(world);

            if (goalNode == null)
            {
                return new SearchResult { Success = false };
            }

            var moves = new List<InputCommand>();
            var cursor = goalNode;
            while (cursor.Parent != null)
            {
                moves.Insert(0, cursor.CommandTaken);
                cursor = cursor.Parent;
            }

            return new SearchResult { Success = true, Moves = moves, TicksUsed = moves.Count };
        }
    }
}
