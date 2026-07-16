using TimeLoop.Grid;

namespace TimeLoop.Actors
{
    /// <summary>
    /// Buffers the most recent swipe/tap/keyboard input between simulation ticks (which run at
    /// GameSettings.TicksPerSecond, typically slower than the render frame rate) and hands it to
    /// TimeLoopManager once per tick, then clears. PlayerController queues into this every Update();
    /// TimelineRecorder records exactly what this returns each tick.
    /// </summary>
    public sealed class LiveInputProvider : IInputProvider
    {
        private Direction _queuedDirection = Direction.None;
        private bool _queuedInteract;

        public void QueueDirection(Direction direction) => _queuedDirection = direction;
        public void QueueInteract() => _queuedInteract = true;

        public InputCommand GetCommand(int tick)
        {
            var command = new InputCommand(_queuedDirection, _queuedInteract);
            _queuedDirection = Direction.None;
            _queuedInteract = false;
            return command;
        }
    }
}
