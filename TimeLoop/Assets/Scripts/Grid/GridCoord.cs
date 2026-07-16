using System;

namespace TimeLoop.Grid
{
    /// <summary>
    /// Integer grid coordinate. The entire simulation is expressed in these, never in world-space
    /// floats, so that two runs given the same inputs always land on the same tile — see
    /// docs/TECHNICAL_ARCHITECTURE.md section 2 for why this determinism guarantee is the whole game.
    /// </summary>
    [Serializable]
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly GridCoord Zero = new GridCoord(0, 0);

        public GridCoord Offset(Direction direction) => this + direction.ToOffset();

        public int ManhattanDistance(GridCoord other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(GridCoord a, GridCoord b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString() => $"({X}, {Y})";
    }
}
