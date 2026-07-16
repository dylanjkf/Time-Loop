using System.Collections.Generic;
using System.Linq;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Opens (becomes non-solid) when its gating condition over one or more Switches/PressurePlates
    /// is satisfied. LevelParser wires up <see cref="Sources"/> from the LEGEND after every entity
    /// in a level exists, since a door can reference switches/plates defined anywhere in the grid.
    /// </summary>
    public sealed class Door : GridEntityBase
    {
        public enum LogicMode
        {
            And,
            Or
        }

        public IReadOnlyList<IActivatable> Sources => _sources;
        public bool IsOpen { get; private set; }
        public override bool IsSolid => !IsOpen;

        private readonly List<IActivatable> _sources;
        private readonly LogicMode _logic;

        public Door(string id, GridCoord position, IEnumerable<IActivatable> sources, LogicMode logic) : base(id, position)
        {
            _sources = sources.ToList();
            _logic = logic;

            foreach (var source in _sources)
            {
                source.OnActivationChanged += _ => Recompute();
            }

            Recompute();
        }

        private void Recompute()
        {
            if (_sources.Count == 0)
            {
                IsOpen = true;
                return;
            }

            IsOpen = _logic == LogicMode.And
                ? _sources.All(s => s.IsActivated)
                : _sources.Any(s => s.IsActivated);
        }

        public override void ResetToInitial()
        {
            base.ResetToInitial();
            Recompute();
        }
    }
}
