using System;
using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// A permanent on/off toggle: pressing Interact while standing on it flips the state, which
    /// then holds regardless of who's standing there afterward. Contrast with PressurePlate,
    /// which needs continuous occupancy — use PressurePlate for "must be held simultaneously"
    /// puzzles (the brief's canonical two-switch-door example) and Switch for "toggle once,
    /// permanently" puzzles.
    /// </summary>
    public sealed class Switch : InteractableBase, ISnapshotable, IActivatable
    {
        public bool IsActivated { get; private set; }
        public event Action<IActivatable> OnActivationChanged;

        private readonly bool _initialActivated;

        public Switch(string id, GridCoord position, bool initialActivated = false) : base(id, position)
        {
            _initialActivated = initialActivated;
            IsActivated = initialActivated;
        }

        public override void OnInteract(GridActor actor)
        {
            IsActivated = !IsActivated;
            OnActivationChanged?.Invoke(this);
        }

        public override void ResetToInitial()
        {
            base.ResetToInitial();
            if (IsActivated != _initialActivated)
            {
                IsActivated = _initialActivated;
                OnActivationChanged?.Invoke(this);
            }
        }

        public object CaptureSnapshot() => IsActivated;

        public void RestoreSnapshot(object snapshot)
        {
            var value = (bool)snapshot;
            if (value != IsActivated)
            {
                IsActivated = value;
                OnActivationChanged?.Invoke(this);
            }
        }
    }
}
