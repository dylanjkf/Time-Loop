using System.Collections.Generic;
using System.Linq;
using TimeLoop.Grid;

namespace TimeLoop.Timeline
{
    /// <summary>
    /// The full recorded input stream for one completed loop, plus the metadata the rest of the
    /// game needs about it (star scoring, Daily Challenge notation, achievements). This — not a
    /// list of transforms — is the entire definition of a "ghost" (docs/TECHNICAL_ARCHITECTURE.md
    /// section 2).
    /// </summary>
    public sealed class RecordedTimeline
    {
        public int LoopIndex { get; }
        private readonly List<InputFrame> _frames = new List<InputFrame>();

        public RecordedTimeline(int loopIndex)
        {
            LoopIndex = loopIndex;
        }

        public IReadOnlyList<InputFrame> Frames => _frames;

        /// <summary>Highest tick this timeline actually has a recorded command for.</summary>
        public int TotalTicks => _frames.Count == 0 ? 0 : _frames[_frames.Count - 1].Tick + 1;

        /// <summary>Number of ticks where the player issued a real move or interact (used for scoring/notation).</summary>
        public int MoveCount => _frames.Count(f => !f.Command.IsIdle);

        public void Append(int tick, InputCommand command) => _frames.Add(new InputFrame(tick, command));

        /// <summary>Returns Idle for any tick beyond the recording — a ghost holds its final tile forever after that.</summary>
        public InputCommand GetCommandAtTick(int tick)
        {
            if (tick < 0 || tick >= _frames.Count) return InputCommand.Idle;
            return _frames[tick].Command;
        }

        /// <summary>Ordered list of the non-idle moves only — the sequence the Daily Challenge notation encodes.</summary>
        public IEnumerable<Direction> MoveSequence() => _frames
            .Where(f => f.Command.Move != Direction.None)
            .Select(f => f.Command.Move);
    }
}
