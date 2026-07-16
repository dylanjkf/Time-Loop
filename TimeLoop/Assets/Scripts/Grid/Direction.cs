namespace TimeLoop.Grid
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExtensions
    {
        public static GridCoord ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new GridCoord(0, 1);
                case Direction.Down: return new GridCoord(0, -1);
                case Direction.Left: return new GridCoord(-1, 0);
                case Direction.Right: return new GridCoord(1, 0);
                default: return GridCoord.Zero;
            }
        }

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        /// <summary>Arrow glyph used by Daily Challenge share cards (MoveNotation) and the in-editor level tool.</summary>
        public static string ToArrowGlyph(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return "⬆️";
                case Direction.Down: return "⬇️";
                case Direction.Left: return "⬅️";
                case Direction.Right: return "➡️";
                default: return "⏺️";
            }
        }
    }
}
