using TimeLoop.Actors;
using TimeLoop.Grid;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.VFX
{
    /// <summary>
    /// Marks the exact tile a new ghost is born on with a one-shot particle burst, giving the
    /// "timeline split" moment (a completed loop becoming a ghost — see
    /// TimeLoop.Actors.GhostAgent) a clear, readable visual anchor.
    /// </summary>
    public sealed class TimelineSplitEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _splitBurst;
        [SerializeField] private float _worldUnitsPerTile = 1f;

        private TimelineEvents _subscribedEvents;

        /// <summary>
        /// Wires this effect up to the loop's event channel. Safe to call with events == null
        /// (no-op) and safe to call again with a new instance — any previous subscription is torn
        /// down first.
        /// </summary>
        public void SubscribeToTimeline(TimelineEvents events)
        {
            if (_subscribedEvents != null)
            {
                _subscribedEvents.OnGhostSpawned -= HandleGhostSpawned;
            }

            _subscribedEvents = events;

            if (_subscribedEvents == null) return;

            _subscribedEvents.OnGhostSpawned += HandleGhostSpawned;
        }

        private void HandleGhostSpawned(GhostAgent ghost)
        {
            if (ghost == null || ghost.Actor == null || _splitBurst == null) return;

            _splitBurst.transform.position = GridToWorld(ghost.Actor.Position);
            _splitBurst.Play();
        }

        // Same GridCoord-to-Vector3 convention as TimeLoop.Actors.ActorVisual.GridToWorld: ground
        // plane is XZ with Y reserved for height.
        private Vector3 GridToWorld(GridCoord coord) =>
            new Vector3(coord.X * _worldUnitsPerTile, 0f, coord.Y * _worldUnitsPerTile);

        private void OnDestroy()
        {
            SubscribeToTimeline(null);
        }
    }
}
