using System;

namespace TimeLoop.Grid
{
    /// <summary>
    /// The single atomic unit of play: one tick's worth of player intent. This is what
    /// TimelineRecorder records and RecordedInputProvider replays — never a transform sample.
    /// </summary>
    [Serializable]
    public readonly struct InputCommand : IEquatable<InputCommand>
    {
        public readonly Direction Move;
        public readonly bool Interact;

        public InputCommand(Direction move, bool interact)
        {
            Move = move;
            Interact = interact;
        }

        public static readonly InputCommand Idle = new InputCommand(Direction.None, false);

        public bool IsIdle => Move == Direction.None && !Interact;

        public bool Equals(InputCommand other) => Move == other.Move && Interact == other.Interact;
        public override bool Equals(object obj) => obj is InputCommand other && Equals(other);
        public override int GetHashCode() => ((int)Move << 1) ^ (Interact ? 1 : 0);
        public override string ToString() => Interact ? $"{Move}+Interact" : Move.ToString();
    }
}
