using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Drives the Infinite Mode menu screen. Infinite Mode is premium-only, so this screen has
    /// two mutually exclusive states: an accessible state with a difficulty slider and a
    /// Generate button, or a locked state showing an upsell overlay. The difficulty slider's
    /// range (0-2) matches <see cref="TimeLoop.Generation.ProceduralGenerator"/>'s
    /// difficultyTier parameter exactly — 0 = Easy (1 plate), 1 = Medium (2 plates),
    /// 2 = Hard (3 plates).
    /// </summary>
    public sealed class InfiniteModeController : MonoBehaviour
    {
        [SerializeField] private Slider _difficultySlider;
        [SerializeField] private TextMeshProUGUI _difficultyLabel;
        [SerializeField] private Button _generateButton;
        [SerializeField] private Button _lockedOverlayButton;
        [SerializeField] private GameObject _lockedOverlay;

        /// <summary>
        /// Binds the screen for the given premium state. Safe to call every time the screen is
        /// shown — listeners are cleared first so repeated Bind calls don't stack callbacks.
        /// </summary>
        /// <param name="isPremiumUnlocked">Whether Infinite Mode is unlocked for this player.</param>
        /// <param name="onGenerateRequested">
        /// Invoked with the chosen difficultyTier (0/1/2) when the player taps Generate.
        /// </param>
        /// <param name="onUpsellRequested">Invoked when a locked-out player taps the overlay's button.</param>
        public void Bind(bool isPremiumUnlocked, Action<int> onGenerateRequested, Action onUpsellRequested)
        {
            if (_difficultySlider != null)
            {
                _difficultySlider.minValue = 0;
                _difficultySlider.maxValue = 2;
                _difficultySlider.wholeNumbers = true;
                _difficultySlider.onValueChanged.RemoveAllListeners();
            }

            if (_generateButton != null)
            {
                _generateButton.onClick.RemoveAllListeners();
            }

            if (_lockedOverlayButton != null)
            {
                _lockedOverlayButton.onClick.RemoveAllListeners();
            }

            UpdateDifficultyLabel(_difficultySlider != null ? _difficultySlider.value : 0f);

            if (isPremiumUnlocked)
            {
                if (_lockedOverlay != null)
                {
                    _lockedOverlay.SetActive(false);
                }

                if (_generateButton != null)
                {
                    _generateButton.onClick.AddListener(() =>
                    {
                        var difficultyTier = _difficultySlider != null ? (int)_difficultySlider.value : 0;
                        onGenerateRequested?.Invoke(difficultyTier);
                    });
                }

                if (_difficultySlider != null)
                {
                    _difficultySlider.onValueChanged.AddListener(UpdateDifficultyLabel);
                }
            }
            else
            {
                if (_lockedOverlay != null)
                {
                    _lockedOverlay.SetActive(true);
                }

                if (_lockedOverlayButton != null)
                {
                    _lockedOverlayButton.onClick.AddListener(() => onUpsellRequested?.Invoke());
                }
            }
        }

        private void UpdateDifficultyLabel(float sliderValue)
        {
            if (_difficultyLabel == null)
            {
                return;
            }

            var difficultyTier = Mathf.RoundToInt(sliderValue);
            _difficultyLabel.text = difficultyTier switch
            {
                0 => "Easy",
                1 => "Medium",
                _ => "Hard",
            };
        }
    }
}
