namespace TimeLoop.Grid
{
    /// <summary>
    /// Implemented by every static/interactive occupant of the grid (switches, doors, boxes,
    /// hazards, goals, platforms, teleport pads). Player and ghost actors are NOT IGridEntity —
    /// they are tracked separately by TimeLoopManager and fed directly into MoveResolver, so the
    /// world's entity registry only ever holds level geometry, never actors. See
    /// docs/TECHNICAL_ARCHITECTURE.md section 4.1.
    /// </summary>
    public interface IGridEntity
    {
        string Id { get; }
        GridCoord Position { get; }

        /// <summary>Blocks movement into this tile (e.g. a closed Door). False for walkable interactables.</summary>
        bool IsSolid { get; }

        void SetPosition(GridCoord position);

        /// <summary>Restores this entity to the state it was constructed with, called at the start of every loop.</summary>
        void ResetToInitial();
    }

    /// <summary>Implemented by entities that need to advance on their own each tick (e.g. MovingPlatform).</summary>
    public interface IWorldTickable
    {
        void OnWorldTick(int tickNumber, GridWorld world);
    }
}
