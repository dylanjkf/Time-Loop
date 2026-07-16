using System.Collections.Generic;

namespace TimeLoop.Grid
{
    /// <summary>Implemented by entities with mutable state beyond position (Switch on/off, Door open/closed).</summary>
    public interface ISnapshotable
    {
        object CaptureSnapshot();
        void RestoreSnapshot(object snapshot);
    }

    /// <summary>
    /// An immutable capture of every entity's position and custom state at one instant. Used by
    /// PuzzleSolver to snapshot/restore state cheaply while searching many hypothetical futures
    /// (see docs/PUZZLE_GENERATION.md). For the everyday "reset world to level start" case at the
    /// top of every loop, GridWorld.ResetToInitial() is used instead — it is simpler (each entity
    /// already remembers its own authored initial state) and doesn't need this snapshot at all.
    /// </summary>
    public sealed class GridWorldState
    {
        private readonly Dictionary<string, GridCoord> _entityPositions;
        private readonly Dictionary<string, object> _entitySnapshots;

        private GridWorldState(Dictionary<string, GridCoord> positions, Dictionary<string, object> snapshots)
        {
            _entityPositions = positions;
            _entitySnapshots = snapshots;
        }

        public static GridWorldState Capture(GridWorld world)
        {
            var positions = new Dictionary<string, GridCoord>();
            var snapshots = new Dictionary<string, object>();

            foreach (var entity in world.AllEntities)
            {
                positions[entity.Id] = entity.Position;
                if (entity is ISnapshotable snapshotable)
                {
                    snapshots[entity.Id] = snapshotable.CaptureSnapshot();
                }
            }

            return new GridWorldState(positions, snapshots);
        }

        public void Restore(GridWorld world)
        {
            foreach (var entity in world.AllEntities)
            {
                if (_entityPositions.TryGetValue(entity.Id, out var pos))
                {
                    world.MoveEntityTo(entity, pos);
                }

                if (entity is ISnapshotable snapshotable && _entitySnapshots.TryGetValue(entity.Id, out var snapshot))
                {
                    snapshotable.RestoreSnapshot(snapshot);
                }
            }
        }
    }
}
