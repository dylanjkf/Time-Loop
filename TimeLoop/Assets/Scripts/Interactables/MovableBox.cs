using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// A pushable crate. Push feasibility (destination tile free, not another box, not another
    /// actor) is resolved by MoveResolver, which special-cases boxes via GridWorld.GetBoxAt before
    /// applying the generic wall/solid-entity block check — see
    /// docs/TECHNICAL_ARCHITECTURE.md section 5. Deliberately does not implement IInteractable:
    /// boxes only ever move via a push, never via Interact or occupancy.
    /// </summary>
    public sealed class MovableBox : GridEntityBase
    {
        public override bool IsSolid => true;

        public MovableBox(string id, GridCoord position) : base(id, position)
        {
        }
    }
}
