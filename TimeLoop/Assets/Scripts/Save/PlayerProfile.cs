using System.Linq;
using TimeLoop.Puzzle;

namespace TimeLoop.Save
{
    /// <summary>
    /// Runtime convenience wrapper around the persisted SaveData blob — gives gameplay code a
    /// typed API instead of poking List&lt;LevelProgressRecord&gt; directly, while Raw stays the
    /// single object handed back to SaveSystem.Save.
    /// </summary>
    public class PlayerProfile
    {
        public SaveData Raw { get; }

        private PlayerProfile(SaveData raw)
        {
            Raw = raw;
        }

        public static PlayerProfile LoadFromDisk() => new PlayerProfile(SaveSystem.Load());

        /// <summary>
        /// Design choice: FewestLoops/FewestTicks are tracked independently of BestStars (each is
        /// its own best-ever value) rather than gated on a matching star count — a low-loop replay
        /// and a high-star run are both worth remembering even if they're different attempts.
        /// </summary>
        public void RecordLevelResult(LevelResult result)
        {
            var record = Raw.LevelProgress.FirstOrDefault(r => r.LevelId == result.LevelId);
            var stars = (int)result.Stars;

            if (record == null)
            {
                record = new LevelProgressRecord
                {
                    LevelId = result.LevelId,
                    BestStars = stars,
                    FewestLoops = result.LoopsUsed,
                    FewestTicks = result.TicksElapsed
                };
                Raw.LevelProgress.Add(record);
                return;
            }

            if (stars > record.BestStars) record.BestStars = stars;
            if (result.LoopsUsed < record.FewestLoops) record.FewestLoops = result.LoopsUsed;
            if (result.TicksElapsed < record.FewestTicks) record.FewestTicks = result.TicksElapsed;
        }

        public int TotalStars() => Raw.LevelProgress.Sum(r => r.BestStars);

        public bool IsLevelCompleted(string levelId) => Raw.LevelProgress.Any(r => r.LevelId == levelId);

        public void Persist() => SaveSystem.Save(Raw);
    }
}
