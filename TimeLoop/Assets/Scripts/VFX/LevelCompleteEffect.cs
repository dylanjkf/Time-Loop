using System.Collections;
using TimeLoop.Puzzle;
using UnityEngine;

namespace TimeLoop.VFX
{
    /// <summary>
    /// Celebrates a solved level with a completion particle burst and a brief ambient light
    /// pulse — the payoff beat for PuzzleManager.OnLevelResult (see
    /// TimeLoop.Puzzle.PuzzleManager).
    /// </summary>
    public sealed class LevelCompleteEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _completionBurst;
        [SerializeField] private Light _completionLight;
        [SerializeField] private float _lightPulseSeconds = 1f;
        [SerializeField] private float _lightPulseIntensityBoost = 3f;

        private PuzzleManager _subscribedManager;
        private Coroutine _pulseRoutine;

        /// <summary>
        /// Wires this effect up to the given level's puzzle manager. Safe to call with
        /// puzzleManager == null (no-op) and safe to call again with a new instance — any
        /// previous subscription is torn down first.
        /// </summary>
        public void SubscribeToPuzzleManager(PuzzleManager puzzleManager)
        {
            if (_subscribedManager != null)
            {
                _subscribedManager.OnLevelResult -= HandleLevelResult;
            }

            _subscribedManager = puzzleManager;

            if (_subscribedManager == null) return;

            _subscribedManager.OnLevelResult += HandleLevelResult;
        }

        private void HandleLevelResult(LevelResult result) => PlayCompletionEffect();

        private void PlayCompletionEffect()
        {
            if (_completionBurst != null)
            {
                _completionBurst.Play();
            }

            if (_completionLight == null) return;

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseLightRoutine());
        }

        private IEnumerator PulseLightRoutine()
        {
            var originalIntensity = _completionLight.intensity;
            var peakIntensity = originalIntensity + _lightPulseIntensityBoost;
            var halfDuration = _lightPulseSeconds * 0.5f;

            var t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                _completionLight.intensity = Mathf.Lerp(originalIntensity, peakIntensity, halfDuration <= 0f ? 1f : t / halfDuration);
                yield return null;
            }
            _completionLight.intensity = peakIntensity;

            t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                _completionLight.intensity = Mathf.Lerp(peakIntensity, originalIntensity, halfDuration <= 0f ? 1f : t / halfDuration);
                yield return null;
            }
            _completionLight.intensity = originalIntensity;

            _pulseRoutine = null;
        }

        private void OnDestroy()
        {
            SubscribeToPuzzleManager(null);
        }
    }
}
