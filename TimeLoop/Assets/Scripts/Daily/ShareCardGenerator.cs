using System.Collections.Generic;
using TimeLoop.Puzzle;
using TimeLoop.Timeline;

namespace TimeLoop.Daily
{
    /// <summary>Composes the shareable Daily Challenge result text, Wordle-style (docs/GAME_DESIGN_DOCUMENT.md section 6).</summary>
    public static class ShareCardGenerator
    {
        public static string BuildShareText(
            int puzzleNumber,
            int timelinesUsed,
            StarRating stars,
            IReadOnlyList<RecordedTimeline> ghostTimelines,
            RecordedTimeline finalTimeline)
        {
            var starCount = (int)stars;
            var starGlyphs = new string('⭐', starCount) + new string('☆', 3 - starCount);

            return
                $"TIME LOOP #{puzzleNumber}\n" +
                $"Solved in {timelinesUsed} timeline{(timelinesUsed == 1 ? "" : "s")}\n" +
                $"{starGlyphs}\n\n" +
                MoveNotation.EncodeAllTimelines(ghostTimelines, finalTimeline);
        }
    }
}
