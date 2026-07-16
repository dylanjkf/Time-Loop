using System;
using TimeLoop.Daily;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Drives the Daily Challenge menu screen: today's puzzle number, whether it has already
    /// been completed (official completion, not practice replays — see
    /// <see cref="DailyChallengeManager.HasCompletedToday"/>), the Play entry point, and the
    /// Wordle-style share result produced after a completion (see
    /// <see cref="ShareCardGenerator"/>).
    /// </summary>
    public sealed class DailyChallengeController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _puzzleNumberLabel;
        [SerializeField] private Button _playButton;
        [SerializeField] private GameObject _alreadyCompletedBadge;
        [SerializeField] private Button _shareButton;

        /// <summary>
        /// Read-only, select-copy display of the share text. There is no native share sheet in
        /// this project — a real build would swap this out for a platform share intent (e.g.
        /// UIActivityViewController on iOS, Intent.ACTION_SEND on Android) via a native plugin.
        /// Until then, the player can manually select and copy the text out of this label, and
        /// the share button below offers a best-effort clipboard copy as a stand-in.
        /// </summary>
        [SerializeField] private TextMeshProUGUI _shareText;

        /// <summary>
        /// Binds the screen to today's puzzle. Safe to call every time the screen is shown —
        /// listeners are cleared first so repeated Bind calls don't stack callbacks.
        /// </summary>
        public void Bind(DailyChallengeManager dailyManager, Action onPlayRequested)
        {
            if (dailyManager == null)
            {
                return;
            }

            if (_puzzleNumberLabel != null)
            {
                _puzzleNumberLabel.text = $"TIME LOOP #{dailyManager.TodaysPuzzleNumber}";
            }

            if (_alreadyCompletedBadge != null)
            {
                _alreadyCompletedBadge.SetActive(dailyManager.HasCompletedToday);
            }

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => onPlayRequested?.Invoke());
            }

            // No share result exists yet at bind time — it only appears once the caller invokes
            // ShowShareResult (typically right after a completed run of today's puzzle).
            if (_shareButton != null)
            {
                _shareButton.onClick.RemoveAllListeners();
                _shareButton.onClick.AddListener(CopyShareTextToClipboard);
                _shareButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reveals the share result produced by <see cref="ShareCardGenerator"/> after a
        /// completed run of today's puzzle.
        /// </summary>
        public void ShowShareResult(string shareText)
        {
            if (_shareText != null)
            {
                _shareText.text = shareText;
            }

            if (_shareButton != null)
            {
                _shareButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// PLATFORM INTEGRATION POINT: with a native share plugin installed, this would launch
        /// the OS share sheet with <see cref="_shareText"/>'s contents instead. Without one, the
        /// best we can offer is copying the text to the system clipboard so the player can paste
        /// it into whatever app they want to share to.
        /// </summary>
        private void CopyShareTextToClipboard()
        {
            if (_shareText == null)
            {
                return;
            }

            GUIUtility.systemCopyBuffer = _shareText.text;
        }
    }
}
