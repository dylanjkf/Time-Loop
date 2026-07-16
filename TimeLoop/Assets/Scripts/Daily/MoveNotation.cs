using System.Linq;
using System.Text;
using TimeLoop.Timeline;

namespace TimeLoop.Daily
{
    /// <summary>Encodes RecordedTimelines to the arrow-emoji notation used in Daily Challenge share cards (e.g. "⬆️⬆️➡️").</summary>
    public static class MoveNotation
    {
        public static string EncodeTimeline(RecordedTimeline timeline) =>
            string.Concat(timeline.MoveSequence().Select(d => d.ToArrowGlyph()));

        public static string EncodeAllTimelines(System.Collections.Generic.IReadOnlyList<RecordedTimeline> ghostTimelines, RecordedTimeline finalTimeline)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < ghostTimelines.Count; i++)
            {
                sb.AppendLine($"Timeline {i + 1}:");
                sb.AppendLine(EncodeTimeline(ghostTimelines[i]));
            }

            sb.AppendLine($"Timeline {ghostTimelines.Count + 1}:");
            sb.AppendLine(EncodeTimeline(finalTimeline));

            return sb.ToString().TrimEnd();
        }
    }
}
