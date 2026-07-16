using System;
using System.Collections.Generic;

namespace TimeLoop.Save
{
    /// <summary>
    /// The single JSON blob persisted to disk via SaveSystem. Every field must stay
    /// JsonUtility-serializable (public, no Dictionary, no interfaces) — see LevelProgressRecord.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public List<LevelProgressRecord> LevelProgress = new();
        public List<int> CompletedDailyPuzzleNumbers = new();
        public List<string> UnlockedAchievementIds = new();
        public List<string> UnlockedThemes = new();
        public bool MusicEnabled = true;
        public bool SoundEnabled = true;
        public bool IsPremiumUnlocked = false;
        public string SelectedTheme = "Default";

        /// <summary>Running counter of ghost-assisted level completions, incremented once per completed level per active ghost — feeds the "Future Self" achievement.</summary>
        public int GhostCooperationCount = 0;
    }
}
