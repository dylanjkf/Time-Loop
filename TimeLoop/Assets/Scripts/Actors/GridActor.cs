using TimeLoop.Grid;

namespace TimeLoop.Actors
{
    /// <summary>
    /// Pure-logic per-actor simulation core — no MonoBehaviour, no Unity API calls. A ghost is
    /// nothing more than a second instance of this exact class fed a RecordedInputProvider
    /// instead of a LiveInputProvider; there is no separate "replay" code path to desync from the
    /// original run. See docs/TECHNICAL_ARCHITECTURE.md section 2.
    /// </summary>
    public sealed class GridActor
    {
        public string Id { get; }
        public GridCoord Position { get; private set; }
        public Direction Facing { get; private set; } = Direction.Down;

        /// <summary>
        /// False for the live player, true for every ghost. Hazard uses this to apply
        /// docs/TECHNICAL_ARCHITECTURE.md section 5 rule 7 — hazards only ever reset the live
        /// player; a ghost is an immutable record of a run that, by construction, never touched one.
        /// </summary>
        public bool IsGhost { get; }

        private readonly GridCoord _spawnPosition;

        public GridActor(string id, GridCoord spawnPosition, bool isGhost = false)
        {
            Id = id;
            Position = spawnPosition;
            _spawnPosition = spawnPosition;
            IsGhost = isGhost;
        }

        /// <summary>Applied by GridWorld.Step once a tick's movement has been resolved.</summary>
        public void SetPosition(GridCoord position)
        {
            if (position != Position)
            {
                var derivedFacing = DeriveFacing(position - Position);
                if (derivedFacing.HasValue)
                {
                    Facing = derivedFacing.Value;
                }
            }

            Position = position;
        }

        public void ResetToSpawn()
        {
            Position = _spawnPosition;
            Facing = Direction.Down;
        }

        private static Direction? DeriveFacing(GridCoord delta)
        {
            if (delta == new GridCoord(0, 1)) return Direction.Up;
            if (delta == new GridCoord(0, -1)) return Direction.Down;
            if (delta == new GridCoord(-1, 0)) return Direction.Left;
            if (delta == new GridCoord(1, 0)) return Direction.Right;
            return null;
        }
    }
}
