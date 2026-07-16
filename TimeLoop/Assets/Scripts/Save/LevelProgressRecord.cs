using System;

namespace TimeLoop.Save
{
    /// <summary>
    /// Best-ever result for a single level. Plain, JsonUtility-friendly fields only — no
    /// Dictionary — so SaveData can serialize a list of these directly.
    /// </summary>
    [Serializable]
    public class LevelProgressRecord
    {
        public string LevelId;
        public int BestStars;
        public int FewestLoops;
        public int FewestTicks;
    }
}
