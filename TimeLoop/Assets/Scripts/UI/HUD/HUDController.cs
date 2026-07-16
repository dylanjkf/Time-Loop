using System;
using System.Collections;
using TimeLoop.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.HUD
{
    /// <summary>
    /// Top-level owner of the in-level HUD: wires the timer/loop-counter displays and the
    /// reset/split-timeline/pause buttons to a TimeLoopManager, and flashes an overlay whenever a
    /// hazard discards the current attempt. Deliberately does not know about the pause menu
    /// itself — it only raises <see cref="OnPauseRequested"/> so a separate PauseMenuController
    /// can own actually showing that panel, keeping the HUD decoupled from menu systems.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        private const float HazardFlashHalfDuration = 0.125f;

        [SerializeField] private TimerDisplay _timerDisplay;
        [SerializeField] private LoopCounterDisplay _loopCounterDisplay;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _splitTimelineButton;
        [SerializeField] private Image _hazardFlashOverlay;

        /// <summary>Raised when the player taps the pause button. The HUD does not show the pause menu itself.</summary>
        public event Action OnPauseRequested;

        private TimeLoopManager _manager;
        private Coroutine _hazardFlashCoroutine;

        /// <summary>
        /// Wires every HUD element to the given loop manager. Safe to call again with a new
        /// manager (or null) — any previous subscriptions/listeners are torn down first.
        /// </summary>
        public void Bind(TimeLoopManager manager)
        {
            Unbind();

            _manager = manager;

            if (_timerDisplay != null)
            {
                _timerDisplay.Bind(manager);
            }

            if (_loopCounterDisplay != null)
            {
                _loopCounterDisplay.Bind(manager);
            }

            if (_manager == null)
            {
                return;
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(() => _manager.ResetCurrentLoop());
            }

            if (_splitTimelineButton != null)
            {
                _splitTimelineButton.onClick.RemoveAllListeners();
                _splitTimelineButton.onClick.AddListener(() => _manager.RequestManualLoopSplit());
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(() => OnPauseRequested?.Invoke());
            }

            _manager.Events.OnHazardReset += HandleHazardReset;
            _manager.Events.OnLoopStart += HandleLoopStart;

            RefreshSplitTimelineButton();
        }

        private void Unbind()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
            }

            if (_splitTimelineButton != null)
            {
                _splitTimelineButton.onClick.RemoveAllListeners();
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }

            if (_manager != null)
            {
                _manager.Events.OnHazardReset -= HandleHazardReset;
                _manager.Events.OnLoopStart -= HandleLoopStart;
            }

            if (_hazardFlashCoroutine != null)
            {
                StopCoroutine(_hazardFlashCoroutine);
                _hazardFlashCoroutine = null;
            }

            _manager = null;
        }

        /// <summary>
        /// Re-checks whether the current level allows manually splitting the timeline early.
        /// Called on every TimelineEvents.OnLoopStart (in addition to right after Bind), since a
        /// new level can load with a different AllowsManualSplit value and OnLoopStart fires at
        /// the start of every level too, not just when a loop resets mid-level.
        /// </summary>
        private void HandleLoopStart(int loopIndex) => RefreshSplitTimelineButton();

        private void RefreshSplitTimelineButton()
        {
            if (_splitTimelineButton == null || _manager == null)
            {
                return;
            }

            var allowsManualSplit = _manager.CurrentLevel != null && _manager.CurrentLevel.AllowsManualSplit;
            _splitTimelineButton.gameObject.SetActive(allowsManualSplit);
            _splitTimelineButton.interactable = allowsManualSplit;
        }

        private void HandleHazardReset()
        {
            if (_hazardFlashOverlay == null)
            {
                return;
            }

            if (_hazardFlashCoroutine != null)
            {
                StopCoroutine(_hazardFlashCoroutine);
            }

            _hazardFlashCoroutine = StartCoroutine(FlashHazardOverlay());
        }

        private IEnumerator FlashHazardOverlay()
        {
            var baseColor = _hazardFlashOverlay.color;
            var restingAlpha = baseColor.a;

            yield return FadeOverlayAlpha(baseColor, restingAlpha, 1f, HazardFlashHalfDuration);
            yield return FadeOverlayAlpha(baseColor, 1f, restingAlpha, HazardFlashHalfDuration);

            _hazardFlashOverlay.color = new Color(baseColor.r, baseColor.g, baseColor.b, restingAlpha);
            _hazardFlashCoroutine = null;
        }

        private IEnumerator FadeOverlayAlpha(Color baseColor, float fromAlpha, float toAlpha, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                _hazardFlashOverlay.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
