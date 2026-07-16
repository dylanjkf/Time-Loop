using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>Marks a tile that PuzzleManager's WinCondition checks the live player (and optionally specific ghosts) must occupy.</summary>
    public sealed class Goal : GridEntityBase
    {
        public Goal(string id, GridCoord position) : base(id, position)
        {
        }
    }
}
