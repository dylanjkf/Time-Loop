using TimeLoop.Grid;

namespace TimeLoop.Timeline
{
    /// <summary>One recorded tick: which simulation tick it was, and the command issued on it.</summary>
    public readonly struct InputFrame
    {
        public readonly int Tick;
        public readonly InputCommand Command;

        public InputFrame(int tick, InputCommand command)
        {
            Tick = tick;
            Command = command;
        }
    }
}
