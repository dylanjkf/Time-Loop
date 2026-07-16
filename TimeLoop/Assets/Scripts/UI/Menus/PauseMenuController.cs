using System;
using TimeLoop.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Drives the in-level pause overlay. Pausing is implemented by setting
    /// <see cref="Time.timeScale"/> to zero, which is a safe way to freeze
    /// <see cref="TimeLoopManager"/> because its tick accumulator advances via
    /// <see cref="Time.deltaTime"/> in Update — with timeScale at zero that delta is
    /// itself zero, so no ticks run while the panel is shown.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartLevelButton;
        [SerializeField] private Button _quitToMenuButton;

        /// <summary>Shows the pause panel, freezes gameplay time, and wires up the panel's buttons.</summary>
        public void Show(TimeLoopManager manager, Action onQuitToMenu)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            Time.timeScale = 0f;

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(Hide);
            }

            if (_restartLevelButton != null)
            {
                _restartLevelButton.onClick.RemoveAllListeners();
                _restartLevelButton.onClick.AddListener(() =>
                {
                    manager.RestartLevel();
                    Hide();
                });
            }

            if (_quitToMenuButton != null)
            {
                _quitToMenuButton.onClick.RemoveAllListeners();
                _quitToMenuButton.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f;
                    onQuitToMenu?.Invoke();
                });
            }
        }

        /// <summary>Hides the pause panel and restores normal gameplay time.</summary>
        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            Time.timeScale = 1f;
        }
    }
}
