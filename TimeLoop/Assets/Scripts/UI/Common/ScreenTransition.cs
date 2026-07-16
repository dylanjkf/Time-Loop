using System.Collections;
using UnityEngine;

namespace TimeLoop.UI.Common
{
    /// <summary>
    /// Fades a full-screen menu panel in and out via its <see cref="CanvasGroup"/>. The panel is
    /// only made interactable/raycast-blocking once it is fully visible, and it is made
    /// non-interactable the instant a fade-out begins, so players can never click through a
    /// half-faded screen or interact with one that is on its way out.
    /// </summary>
    public sealed class ScreenTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeSeconds = 0.25f;

        /// <summary>
        /// Fades the panel from its current alpha up to fully opaque. Interaction stays disabled
        /// for the duration of the fade and is only re-enabled once alpha reaches 1.
        /// </summary>
        public IEnumerator FadeIn()
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var startAlpha = _canvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = _fadeSeconds > 0f ? Mathf.Clamp01(elapsed / _fadeSeconds) : 1f;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        /// <summary>
        /// Fades the panel from its current alpha down to fully transparent. Interaction is
        /// disabled immediately, before the fade even starts, so the panel stops accepting input
        /// the moment it begins disappearing rather than lingering as interactable while faint.
        /// </summary>
        public IEnumerator FadeOut()
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var startAlpha = _canvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = _fadeSeconds > 0f ? Mathf.Clamp01(elapsed / _fadeSeconds) : 1f;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }

        /// <summary>Skips the fade animation entirely, snapping straight to a shown or hidden state.</summary>
        public void SetImmediate(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }
    }
}
