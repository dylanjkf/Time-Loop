using TimeLoop.Actors;
using UnityEngine;

namespace TimeLoop.VFX
{
    /// <summary>
    /// Sits on the same GameObject as a <see cref="GhostAgent"/> and keeps its ParticleSystem's
    /// trail emission enabled for as long as the ghost is active in the scene, giving every ghost
    /// replay a lightweight "temporal residue" trail using nothing but the built-in particle
    /// system (see TimeLoop.Actors.GhostAgent for why ghosts are visually distinct replays, not
    /// video clips).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class GhostTrailEffect : MonoBehaviour
    {
        /// <summary>
        /// Configures the attached ParticleSystem's main.startLifetime in the Inspector — how long
        /// each trail particle survives before fading out of existence.
        /// </summary>
        [SerializeField] private float _trailFadeSeconds = 0.5f;

        private ParticleSystem _particles;
        private GhostAgent _ghost;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            ApplyStartLifetime();
        }

        /// <summary>
        /// Binds this trail to the ghost sharing its GameObject and turns emission on; call once
        /// right after GhostAgent.Initialize. Passing null clears the binding and stops emission.
        /// </summary>
        public void Bind(GhostAgent ghost)
        {
            _ghost = ghost;
            ApplyStartLifetime();

            if (_particles == null) return;

            var emission = _particles.emission;
            emission.enabled = _ghost != null;

            if (_ghost != null && !_particles.isPlaying)
            {
                _particles.Play();
            }
        }

        private void Update()
        {
            if (_particles == null) return;

            var ghostActive = _ghost != null && _ghost.gameObject.activeInHierarchy;
            var emission = _particles.emission;
            if (emission.enabled != ghostActive)
            {
                emission.enabled = ghostActive;
            }
        }

        private void ApplyStartLifetime()
        {
            if (_particles == null) return;

            var main = _particles.main;
            main.startLifetime = _trailFadeSeconds;
        }
    }
}
