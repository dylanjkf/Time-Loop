using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimeLoop.Levels
{
    /// <summary>Loads .level TextAssets from Resources/Levels/WorldN/ and parses them into LevelDefinitions.</summary>
    public static class LevelLoader
    {
        public static List<LevelDefinition> LoadWorld(int worldNumber)
        {
            var folder = $"Levels/World{worldNumber}";
            var assets = Resources.LoadAll<TextAsset>(folder);

            return assets
                .Select(asset => LevelParser.Parse(asset.name, asset.text))
                .OrderBy(l => l.LevelId)
                .ToList();
        }

        public static List<LevelDefinition> LoadAllWorlds(int worldCount = 4)
        {
            var all = new List<LevelDefinition>();
            for (var world = 1; world <= worldCount; world++)
            {
                all.AddRange(LoadWorld(world));
            }

            return all;
        }
    }
}
