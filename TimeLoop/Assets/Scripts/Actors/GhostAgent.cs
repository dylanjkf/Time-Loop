using TimeLoop.Grid;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.Actors
{
    /// <summary>
    /// A translucent replay of one completed loop. A ghost is not a video clip — it is a second
    /// GridActor driven by a RecordedInputProvider through the exact same deterministic
    /// simulation as the live player (GridWorld.Step doesn't know or care whether an
    /// ActorTickInput came from a human or a recording), which is what guarantees perfect,
    /// trustworthy fidelity every single loop. See docs/TECHNICAL_ARCHITECTURE.md section 4.2.
    /// </summary>
    [RequireComponent(typeof(ActorVisual))]
    public sealed class GhostAgent : MonoBehaviour
    {
        [SerializeField] private Renderer[] _ghostRenderers;
        [SerializeField] private float _ghostAlpha = 0.35f;

        public GridActor Actor { get; private set; }
        public RecordedInputProvider InputProvider { get; private set; }
        public RecordedTimeline Timeline { get; private set; }

        private ActorVisual _visual;

        public void Initialize(string id, GridCoord spawnPosition, RecordedTimeline timeline)
        {
            Actor = new GridActor(id, spawnPosition, isGhost: true);
            Timeline = timeline;
            InputProvider = new RecordedInputProvider(timeline);
            _visual = GetComponent<ActorVisual>();
            _visual.Bind(Actor);
            ApplyGhostMaterialAlpha();
        }

        /// <summary>Ghosts reset to the same spawn tile as the player at the start of every new loop.</summary>
        public void ResetForNewLoop()
        {
            Actor.ResetToSpawn();
            _visual.Bind(Actor);
        }

        private void ApplyGhostMaterialAlpha()
        {
            if (_ghostRenderers == null) return;

            foreach (var renderer in _ghostRenderers)
            {
                if (renderer == null) continue;
                var color = renderer.material.color;
                color.a = _ghostAlpha;
                renderer.material.color = color;
            }
        }

        private void Update()
        {
            _visual?.Tick(Time.deltaTime);
        }
    }
}
