using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>Shared IGridEntity plumbing: id, position, and reset-to-authored-initial-position.</summary>
    public abstract class GridEntityBase : IGridEntity
    {
        public string Id { get; }
        public GridCoord Position { get; private set; }
        protected readonly GridCoord InitialPosition;

        public virtual bool IsSolid => false;

        protected GridEntityBase(string id, GridCoord initialPosition)
        {
            Id = id;
            Position = initialPosition;
            InitialPosition = initialPosition;
        }

        public void SetPosition(GridCoord position) => Position = position;

        public virtual void ResetToInitial() => Position = InitialPosition;
    }

    /// <summary>
    /// Base for every entity that reacts to actors. Subclasses override only the hook(s) they
    /// care about; the rest stay harmless no-ops (docs/TECHNICAL_ARCHITECTURE.md section 4.4).
    /// </summary>
    public abstract class InteractableBase : GridEntityBase, IInteractable
    {
        protected InteractableBase(string id, GridCoord initialPosition) : base(id, initialPosition)
        {
        }

        public virtual void OnEnter(GridActor actor)
        {
        }

        public virtual void OnExit(GridActor actor)
        {
        }

        public virtual void OnInteract(GridActor actor)
        {
        }
    }
}
