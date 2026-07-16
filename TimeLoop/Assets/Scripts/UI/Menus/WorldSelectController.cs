using System;
using System.Collections.Generic;
using TMPro;
using TimeLoop.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Populates the campaign world-select screen with one button per world (1-4), showing the
    /// world's name and how many of its full campaign target level count have shipped so far,
    /// and locking worlds the player hasn't yet earned enough stars to unlock (see
    /// LevelDatabase.IsWorldUnlocked). Purely presentational — instantiation and labelling only;
    /// routing to the level-select screen for the chosen world is left to the caller via the
    /// onWorldSelected callback.
    /// </summary>
    public sealed class WorldSelectController : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<int, string> WorldNames = new Dictionary<int, string>
        {
            { 1, "World 1: Understanding Time" },
            { 2, "World 2: Multiple Timelines" },
            { 3, "World 3: Advanced Time Manipulation" },
            { 4, "World 4: Expert Challenges" }
        };

        private const int FirstWorld = 1;
        private const int LastWorld = 4;

        [SerializeField] private Transform _worldButtonContainer;
        [SerializeField] private GameObject _worldButtonPrefab;

        /// <summary>
        /// Instantiates one button per world 1-4 into <see cref="_worldButtonContainer"/>. Any
        /// buttons instantiated by a previous call are destroyed first, so this is safe to call
        /// again (e.g. when the player earns more stars and revisits the screen).
        /// </summary>
        /// <param name="database">Source of per-world level counts, campaign targets, and unlock rules.</param>
        /// <param name="starsEarnedPerWorld">
        /// Total stars earned per world, keyed by the world index consumed by
        /// LevelDatabase.IsWorldUnlocked (world - 1, i.e. the previous world relative to the one
        /// being unlocked).
        /// </param>
        /// <param name="onWorldSelected">Invoked with the 1-based world number when its button is tapped.</param>
        public void Populate(
            LevelDatabase database,
            IReadOnlyDictionary<int, int> starsEarnedPerWorld,
            Action<int> onWorldSelected)
        {
            if (database == null || _worldButtonContainer == null || _worldButtonPrefab == null) return;

            ClearContainer();

            for (var world = FirstWorld; world <= LastWorld; world++)
            {
                var buttonInstance = Instantiate(_worldButtonPrefab, _worldButtonContainer);
                buttonInstance.SetActive(true);

                var label = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    var worldName = WorldNames.TryGetValue(world, out var name) ? name : $"World {world}";
                    var shippedCount = database.LevelsInWorld(world).Count;
                    var target = database.FullCampaignTargetForWorld(world);
                    label.text = $"{worldName}\n{shippedCount}/{target}";
                }

                var button = buttonInstance.GetComponentInChildren<Button>();
                if (button == null) continue;

                var previousWorldStars = starsEarnedPerWorld != null && starsEarnedPerWorld.TryGetValue(world - 1, out var stars)
                    ? stars
                    : 0;
                button.interactable = database.IsWorldUnlocked(world, previousWorldStars);

                var capturedWorld = world;
                button.onClick.AddListener(() => onWorldSelected?.Invoke(capturedWorld));
            }
        }

        private void ClearContainer()
        {
            for (var i = _worldButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_worldButtonContainer.GetChild(i).gameObject);
            }
        }
    }
}
