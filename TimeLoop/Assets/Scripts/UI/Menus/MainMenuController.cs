using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Top-level main menu screen. Purely presentational: it only translates button taps into
    /// intent events and never loads scenes, activates other screens, or otherwise knows that any
    /// other part of the app exists. A separate app-bootstrap script (outside this component) is
    /// expected to subscribe to these events and route to the appropriate flow — world select for
    /// campaign, the daily challenge screen, infinite mode, or settings.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _campaignButton;
        [SerializeField] private Button _dailyChallengeButton;
        [SerializeField] private Button _infiniteModeButton;
        [SerializeField] private Button _settingsButton;

        public event Action OnCampaignRequested;
        public event Action OnDailyChallengeRequested;
        public event Action OnInfiniteModeRequested;
        public event Action OnSettingsRequested;

        private void Start()
        {
            if (_campaignButton != null)
            {
                _campaignButton.onClick.AddListener(() => OnCampaignRequested?.Invoke());
            }

            if (_dailyChallengeButton != null)
            {
                _dailyChallengeButton.onClick.AddListener(() => OnDailyChallengeRequested?.Invoke());
            }

            if (_infiniteModeButton != null)
            {
                _infiniteModeButton.onClick.AddListener(() => OnInfiniteModeRequested?.Invoke());
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => OnSettingsRequested?.Invoke());
            }
        }
    }
}
