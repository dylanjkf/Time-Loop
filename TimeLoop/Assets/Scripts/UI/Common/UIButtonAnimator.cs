using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TimeLoop.UI.Common
{
    /// <summary>
    /// Drop this on any Button (alongside its Button component) for a simple tactile
    /// press-down/release scale animation. Uses unscaled time so buttons still animate on
    /// screens that run with <see cref="Time.timeScale"/> at zero, such as the pause menu.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _pressedScale = 0.94f;
        [SerializeField] private float _animationSeconds = 0.08f;

        private RectTransform _rectTransform;
        private Coroutine _scaleRoutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            // A pointer-exit/up event is never guaranteed to arrive if the button is disabled
            // mid-press (e.g. the panel it's on gets hidden), so reset state defensively here
            // rather than leaving the button visually stuck at its pressed-down scale.
            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
                _scaleRoutine = null;
            }

            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(Vector3.one * _pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(Vector3.one);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(Vector3.one);
        }

        private void AnimateTo(Vector3 targetScale)
        {
            if (_rectTransform == null)
            {
                return;
            }

            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
            }

            _scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale)
        {
            var startScale = _rectTransform.localScale;
            var elapsed = 0f;

            while (elapsed < _animationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = _animationSeconds > 0f ? Mathf.Clamp01(elapsed / _animationSeconds) : 1f;
                _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            _rectTransform.localScale = targetScale;
            _scaleRoutine = null;
        }
    }
}
