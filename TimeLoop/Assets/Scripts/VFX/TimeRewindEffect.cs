using System.Collections;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.VFX
{
    /// <summary>
    /// Plays a brief full-screen flash whenever a loop ends, giving players a clean visual beat
    /// that separates "recording just finished" from "world is resetting for the next loop" —
    /// implemented purely as a CanvasGroup alpha fade so it costs nothing on lower-end devices.
    /// </summary>
    public sealed class TimeRewindEffect : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _flashOverlay;
        [SerializeField] private float _flashDurationSeconds = 0.3f;
        [SerializeField] private float _flashPeakAlpha = 0.6f;

        private TimelineEvents _subscribedEvents;
        private Coroutine _flashRoutine;

        /// <summary>
        /// Wires this effect up to the loop's event channel. Safe to call with events == null
        /// (no-op) and safe to call again with a new instance — any previous subscription is torn
        /// down first.
        /// </summary>
        public void SubscribeToTimeline(TimelineEvents events)
        {
            if (_subscribedEvents != null)
            {
                _subscribedEvents.OnLoopEnd -= HandleLoopEnd;
            }

            _subscribedEvents = events;

            if (_subscribedEvents == null) return;

            _subscribedEvents.OnLoopEnd += HandleLoopEnd;
        }

        private void HandleLoopEnd(RecordedTimeline completedTimeline) => PlayFlash();

        private void PlayFlash()
        {
            if (_flashOverlay == null) return;

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            var halfDuration = _flashDurationSeconds * 0.5f;

            yield return Fade(0f, _flashPeakAlpha, halfDuration);
            yield return Fade(_flashPeakAlpha, 0f, halfDuration);

            _flashRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _flashOverlay.alpha = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _flashOverlay.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            _flashOverlay.alpha = to;
        }

        private void OnDestroy()
        {
            SubscribeToTimeline(null);
        }
    }
}
