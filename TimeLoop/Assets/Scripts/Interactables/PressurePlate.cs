using System;
using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Auto-activates while any actor stands on it, releases the instant the last one steps off.
    /// This is the entity behind the brief's canonical example: "a door requires two switches
    /// pressed simultaneously... the player cannot stand on both... ghost activates plate A,
    /// player activates plate B." Supports multiple simultaneous occupants (player + ghost could
    /// both stand on the same plate) via a reference count.
    /// </summary>
    public sealed class PressurePlate : InteractableBase, ISnapshotable, IActivatable
    {
        public bool IsActivated => _occupantCount > 0;
        public event Action<IActivatable> OnActivationChanged;

        private int _occupantCount;

        public PressurePlate(string id, GridCoord position) : base(id, position)
        {
        }

        public override void OnEnter(GridActor actor)
        {
            var wasActive = IsActivated;
            _occupantCount++;
            if (IsActivated != wasActive) OnActivationChanged?.Invoke(this);
        }

        public override void OnExit(GridActor actor)
        {
            var wasActive = IsActivated;
            _occupantCount = Math.Max(0, _occupantCount - 1);
            if (IsActivated != wasActive) OnActivationChanged?.Invoke(this);
        }

        public override void ResetToInitial()
        {
            base.ResetToInitial();
            var wasActive = IsActivated;
            _occupantCount = 0;
            if (wasActive) OnActivationChanged?.Invoke(this);
        }

        public object CaptureSnapshot() => _occupantCount;

        public void RestoreSnapshot(object snapshot)
        {
            var wasActive = IsActivated;
            _occupantCount = (int)snapshot;
            if (IsActivated != wasActive) OnActivationChanged?.Invoke(this);
        }
    }
}
