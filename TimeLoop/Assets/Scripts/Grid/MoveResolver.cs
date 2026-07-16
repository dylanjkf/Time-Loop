using System.Collections.Generic;
using System.Linq;
using TimeLoop.Actors;

namespace TimeLoop.Grid
{
    /// <summary>Result of resolving one tick's worth of simultaneous actor moves.</summary>
    public sealed class MoveResolution
    {
        public readonly Dictionary<GridActor, GridCoord> ActorDestinations;
        public readonly List<BoxPush> BoxPushes;

        public MoveResolution(Dictionary<GridActor, GridCoord> destinations, List<BoxPush> boxPushes)
        {
            ActorDestinations = destinations;
            BoxPushes = boxPushes;
        }
    }

    /// <summary>
    /// Implements the simultaneous multi-actor resolution rules documented in
    /// docs/TECHNICAL_ARCHITECTURE.md section 5. Actors are processed in a fixed order (ghosts
    /// oldest-recorded first, then the live player — the caller is responsible for ordering
    /// <paramref name="orderedInputs"/> this way) so the outcome is 100% reproducible every loop.
    /// When two or more actors intend the same destination tile, the earliest-in-order one claims
    /// it and the rest bounce back to their current tile — deliberately NOT "everybody bounces."
    /// Every actor starts every loop from the exact same spawn tile, so "several actors all take
    /// the same first step out of spawn" is the common case, not an edge case; a symmetric bounce
    /// there would have every co-located actor retry, and fail, the identical move forever (a
    /// ghost always reissues the same recorded command at the same tick index regardless of
    /// outcome). First-in-order-wins instead funnels them into a single-file queue that resolves
    /// itself within one tick per actor, which is also the intuitive real-world reading of
    /// "bumping into your own ghost."
    ///
    /// Known, deliberate limitation: only a single box may be pushed per tick per actor, and
    /// multi-box chain pushes (box-pushes-box) are not supported — levels are authored/generated
    /// around this constraint. Rotational 3+ actor swaps (A takes B's tile, B takes C's tile, C
    /// takes A's tile, all at once) are also not supported; such configurations are rare in
    /// practice and are excluded from generated content by PuzzleSolver's validation pass.
    /// </summary>
    public static class MoveResolver
    {
        private const int MaxFixedPointIterations = 16;

        public static MoveResolution Resolve(GridWorld world, IReadOnlyList<ActorTickInput> orderedInputs)
        {
            var currentPos = new Dictionary<GridActor, GridCoord>();
            var orderIndex = new Dictionary<GridActor, int>();
            for (var i = 0; i < orderedInputs.Count; i++)
            {
                currentPos[orderedInputs[i].Actor] = orderedInputs[i].Actor.Position;
                orderIndex[orderedInputs[i].Actor] = i;
            }

            var intents = new Dictionary<GridActor, GridCoord>();
            var pendingBoxPushes = new Dictionary<GridActor, BoxPush>();

            // Pass 1: per-actor feasibility against static geometry and (at most one) box push.
            // Independent of other actors' choices.
            foreach (var input in orderedInputs)
            {
                var actor = input.Actor;
                var here = currentPos[actor];
                var dest = input.Command.Move == Direction.None ? here : here.Offset(input.Command.Move);

                if (dest == here)
                {
                    intents[actor] = here;
                    continue;
                }

                if (world.IsBlockedIgnoringBoxes(dest))
                {
                    intents[actor] = here;
                    continue;
                }

                var box = world.GetBoxAt(dest);
                if (box != null)
                {
                    var boxDest = dest.Offset(input.Command.Move);
                    var boxHasRoom = !world.IsBlockedIgnoringBoxes(boxDest)
                                      && world.GetBoxAt(boxDest) == null
                                      && !currentPos.Values.Contains(boxDest);

                    if (!boxHasRoom)
                    {
                        intents[actor] = here;
                        continue;
                    }

                    pendingBoxPushes[actor] = new BoxPush(box, boxDest);
                    intents[actor] = dest;
                    continue;
                }

                intents[actor] = dest;
            }

            // Pass 2: fixed-point actor-vs-actor resolution. Bounded by actor count, converges fast.
            bool changed;
            var iterations = 0;
            do
            {
                changed = false;
                iterations++;

                // Rule: two or more actors targeting the same new tile — the one earliest in the
                // fixed order (oldest ghost first, live player last) claims it; the rest bounce
                // back to their start. This deliberately is NOT symmetric "everybody bounces":
                // every loop, every ghost (and the live player) starts from the exact same spawn
                // tile, so the extremely common case of "several actors all take the same first
                // step out of spawn on tick 0" needs to resolve to a single-file queue (the
                // earliest-recorded ghost keeps moving, freeing its old tile a tick later) rather
                // than a standoff where nobody involved ever manages to move.
                var contested = intents
                    .Where(kv => kv.Value != currentPos[kv.Key])
                    .GroupBy(kv => kv.Value)
                    .Where(g => g.Count() > 1);

                foreach (var group in contested)
                {
                    var winner = group.OrderBy(kv => orderIndex[kv.Key]).First().Key;
                    foreach (var kv in group)
                    {
                        if (kv.Key != winner && intents[kv.Key] != currentPos[kv.Key])
                        {
                            intents[kv.Key] = currentPos[kv.Key];
                            pendingBoxPushes.Remove(kv.Key);
                            changed = true;
                        }
                    }
                }

                // Rule: an actor cannot move into a tile another actor currently occupies unless
                // that other actor is itself vacating it this tick.
                foreach (var actor in intents.Keys.ToList())
                {
                    var dest = intents[actor];
                    if (dest == currentPos[actor]) continue;

                    foreach (var other in orderedInputs.Select(i => i.Actor))
                    {
                        if (other == actor) continue;
                        if (currentPos[other] != dest) continue;
                        if (intents[other] == currentPos[other])
                        {
                            intents[actor] = currentPos[actor];
                            pendingBoxPushes.Remove(actor);
                            changed = true;
                        }
                    }
                }
            }
            while (changed && iterations < MaxFixedPointIterations);

            var finalBoxPushes = orderedInputs
                .Select(i => i.Actor)
                .Where(a => pendingBoxPushes.ContainsKey(a) && intents[a] != currentPos[a])
                .Select(a => pendingBoxPushes[a])
                .ToList();

            return new MoveResolution(intents, finalBoxPushes);
        }
    }
}
