using System;
using System.Collections.Generic;
using TMPro;
using TimeLoop.Levels;
using TimeLoop.Monetization;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Populates a world's level-select screen with one button per level, showing the level name,
    /// up to 3 best-run star glyphs, and a lock indicator for levels the player hasn't unlocked
    /// via premium purchase yet (see PremiumUnlock.IsLevelAccessible). Locked levels stay visible
    /// and tappable-looking (never disabled) so players can see what premium unlocks — but tapping
    /// one does not invoke the selection callback, since routing to gameplay or an upsell screen
    /// for a locked level is left to the caller.
    /// </summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        private const int MaxStarGlyphs = 3;
        private const string LockedPrefix = "\U0001F512 "; // "🔒 "
        private const char FilledStarGlyph = '★'; // ★
        private const char EmptyStarGlyph = '☆'; // ☆

        [SerializeField] private Transform _levelButtonContainer;
        [SerializeField] private GameObject _levelButtonPrefab;

        /// <summary>
        /// Instantiates one button per level into <see cref="_levelButtonContainer"/>. Any buttons
        /// instantiated by a previous call are destroyed first, so this is safe to call again
        /// (e.g. after a purchase changes which levels are accessible).
        /// </summary>
        /// <param name="levels">Levels to list, in the order they should appear.</param>
        /// <param name="bestStarsByLevelId">Best stars (0-3) earned per level id; missing entries count as 0.</param>
        /// <param name="isPremiumUnlocked">Whether the player owns the premium unlock entitlement.</param>
        /// <param name="onLevelSelected">Invoked with the chosen level when an accessible level's button is tapped.</param>
        public void Populate(
            IReadOnlyList<LevelDefinition> levels,
            IReadOnlyDictionary<string, int> bestStarsByLevelId,
            bool isPremiumUnlocked,
            Action<LevelDefinition> onLevelSelected)
        {
            if (levels == null || _levelButtonContainer == null || _levelButtonPrefab == null) return;

            ClearContainer();

            foreach (var level in levels)
            {
                if (level == null) continue;

                var buttonInstance = Instantiate(_levelButtonPrefab, _levelButtonContainer);
                buttonInstance.SetActive(true);

                var isAccessible = PremiumUnlock.IsLevelAccessible(level, isPremiumUnlocked);

                var label = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    var bestStars = bestStarsByLevelId != null && bestStarsByLevelId.TryGetValue(level.LevelId, out var stars)
                        ? Mathf.Clamp(stars, 0, MaxStarGlyphs)
                        : 0;
                    var starGlyphs = new string(FilledStarGlyph, bestStars) + new string(EmptyStarGlyph, MaxStarGlyphs - bestStars);
                    var lockPrefix = isAccessible ? string.Empty : LockedPrefix;
                    label.text = $"{lockPrefix}{level.Name}\n{starGlyphs}";
                }

                var button = buttonInstance.GetComponentInChildren<Button>();
                if (button == null) continue;

                var capturedLevel = level;
                button.onClick.AddListener(() =>
                {
                    if (!isAccessible) return;
                    onLevelSelected?.Invoke(capturedLevel);
                });
            }
        }

        private void ClearContainer()
        {
            for (var i = _levelButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_levelButtonContainer.GetChild(i).gameObject);
            }
        }
    }
}
