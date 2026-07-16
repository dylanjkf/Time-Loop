using TimeLoop.Grid;

namespace TimeLoop.Timeline
{
    /// <summary>
    /// Appends the live player's command to an in-progress RecordedTimeline every tick.
    /// TimeLoopManager owns the currently-recording instance and swaps in a fresh one at the
    /// start of every loop, then hands the completed RecordedTimeline off to a new GhostAgent.
    /// </summary>
    public sealed class TimelineRecorder
    {
        public RecordedTimeline Current { get; private set; }

        public RecordedTimeline BeginNewRecording(int loopIndex)
        {
            Current = new RecordedTimeline(loopIndex);
            return Current;
        }

        public void RecordTick(int tick, InputCommand command) => Current?.Append(tick, command);
    }
}
