using TimeLoop.Levels;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.VFX
{
    /// <summary>
    /// Reads how much of the current loop's countdown remains and scales a shimmering particle
    /// distortion plus the scene's ambient light to telegraph mounting time pressure — pure
    /// built-in ParticleSystem/Light tuning, no shaders, so it is cheap enough to run on every
    /// mobile tier the game targets.
    /// </summary>
    public sealed class ClockDistortionEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _distortionParticles;
        [SerializeField] private Light _ambientLight;

        [Header("Particle response")]
        [SerializeField] private float _minEmissionRate = 0f;
        [SerializeField] private float _maxEmissionRate = 40f;

        [Header("Light response")]
        [SerializeField] private float _minLightIntensity = 1f;
        [SerializeField] private float _maxLightIntensity = 2.2f;
        [SerializeField] private Color _calmLightColor = Color.white;
        [SerializeField] private Color _criticalLightColor = new Color(1f, 0.25f, 0.2f);

        private TimeLoopManager _manager;
        private LevelDefinition _lastSeenLevel;
        private float _totalLoopSeconds;

        /// <summary>
        /// Directly scales the distortion particles' emission rate and the ambient light's
        /// intensity/color from a normalized time-pressure value: 0 means plenty of time
        /// remaining, 1 means critical. Values outside [0,1] are clamped.
        /// </summary>
        public void SetIntensity(float normalizedTimePressure)
        {
            var pressure = Mathf.Clamp01(normalizedTimePressure);

            if (_distortionParticles != null)
            {
                var emission = _distortionParticles.emission;
                emission.rateOverTime = Mathf.Lerp(_minEmissionRate, _maxEmissionRate, pressure);
            }

            if (_ambientLight != null)
            {
                _ambientLight.intensity = Mathf.Lerp(_minLightIntensity, _maxLightIntensity, pressure);
                _ambientLight.color = Color.Lerp(_calmLightColor, _criticalLightColor, pressure);
            }
        }

        /// <summary>
        /// Wires this effect up to the manager driving the countdown; every Update() afterwards
        /// polls manager.SecondsRemaining relative to manager.CurrentLevel.LoopSeconds and feeds
        /// the resulting normalized pressure into SetIntensity. Safe to call with manager == null
        /// (no-op — also resets the effect back to zero intensity).
        /// </summary>
        public void SubscribeToTimeLoopManager(TimeLoopManager manager)
        {
            _manager = manager;
            _lastSeenLevel = null;
            _totalLoopSeconds = 0f;

            if (_manager == null)
            {
                SetIntensity(0f);
            }
        }

        private void Update()
        {
            if (_manager == null) return;

            var level = _manager.CurrentLevel;
            if (level != _lastSeenLevel)
            {
                _lastSeenLevel = level;
                _totalLoopSeconds = level != null ? level.LoopSeconds : 0f;
            }

            if (_totalLoopSeconds <= 0f)
            {
                SetIntensity(0f);
                return;
            }

            var pressure = 1f - (_manager.SecondsRemaining / _totalLoopSeconds);
            SetIntensity(pressure);
        }
    }
}
