using TimeLoop.Grid;
using UnityEngine;

namespace TimeLoop.Actors
{
    /// <summary>
    /// Smoothly interpolates a transform toward its GridActor's logical tile position every
    /// frame, so the underlying simulation can stay tick-locked (deterministic) while on-screen
    /// motion reads as fluid, premium movement (docs/GAME_DESIGN_DOCUMENT.md section 3). Shared by
    /// PlayerController and GhostAgent so the player and every ghost move identically on screen —
    /// the ghost's uncanny fidelity depends on this being the exact same visual code path.
    ///
    /// Ground plane is XZ with Y reserved for height (jump squash/stretch, VFX lift); the camera
    /// is expected to look down at an angle, matching the Monument Valley-esque premium framing
    /// described in the design brief.
    /// </summary>
    public sealed class ActorVisual : MonoBehaviour
    {
        [SerializeField] private float _tilesPerSecond = 6f;
        [SerializeField] private float _worldUnitsPerTile = 1f;
        [SerializeField] private float _turnDegreesPerSecond = 720f;

        private GridActor _actor;

        public GridActor BoundActor => _actor;

        public void Bind(GridActor actor)
        {
            _actor = actor;
            transform.position = GridToWorld(actor.Position);
            transform.rotation = Quaternion.Euler(0f, FacingToYaw(actor.Facing), 0f);
        }

        public void Tick(float deltaTime)
        {
            if (_actor == null) return;

            var target = GridToWorld(_actor.Position);
            var maxDelta = _tilesPerSecond * _worldUnitsPerTile * deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, maxDelta);

            var targetRotation = Quaternion.Euler(0f, FacingToYaw(_actor.Facing), 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnDegreesPerSecond * deltaTime);
        }

        public Vector3 GridToWorld(GridCoord coord) =>
            new Vector3(coord.X * _worldUnitsPerTile, 0f, coord.Y * _worldUnitsPerTile);

        private static float FacingToYaw(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 0f;
                case Direction.Right: return 90f;
                case Direction.Down: return 180f;
                case Direction.Left: return 270f;
                default: return 0f;
            }
        }
    }
}
