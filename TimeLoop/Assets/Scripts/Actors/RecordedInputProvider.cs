using TimeLoop.Grid;
using TimeLoop.Timeline;

namespace TimeLoop.Actors
{
    /// <summary>
    /// Replays a completed RecordedTimeline tick-for-tick. Once past the recording's length this
    /// returns InputCommand.Idle forever — the ghost simply holds its final resting tile for the
    /// rest of the current (longer) loop, which is exactly what "hold a plate open" cooperation
    /// puzzles rely on (see docs/GAME_DESIGN_DOCUMENT.md section 4.3, "Hold-and-pass").
    /// </summary>
    public sealed class RecordedInputProvider : IInputProvider
    {
        private readonly RecordedTimeline _timeline;

        public RecordedInputProvider(RecordedTimeline timeline)
        {
            _timeline = timeline;
        }

        public InputCommand GetCommand(int tick) => _timeline.GetCommandAtTick(tick);
    }
}
