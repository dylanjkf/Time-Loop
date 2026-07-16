using System;
using System.IO;
using UnityEngine;

namespace TimeLoop.Save
{
    /// <summary>
    /// Fully offline JSON persistence for SaveData — no cloud dependency, so play never blocks on
    /// network access. Load() is deliberately failure-proof: a missing or corrupt save file just
    /// yields a fresh SaveData rather than throwing, so a bad file can never brick the app.
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveFileName = "timeloop_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(SaveData data)
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }

        public static SaveData Load()
        {
            try
            {
                var path = FilePath;
                if (!File.Exists(path))
                {
                    return new SaveData();
                }

                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }
    }
}
