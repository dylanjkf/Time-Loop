using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Paired instant relocation (World 3+): stepping onto one pad immediately relocates the
    /// actor to its linked partner. LevelParser calls <see cref="LinkTo"/> once both pads in a
    /// pair exist. Deliberately a direct position set rather than a full re-dispatch through
    /// GridWorld's enter/exit pipeline, so landing on the partner pad cannot recursively
    /// ping-pong the actor back and forth within a single tick.
    /// </summary>
    public sealed class TeleportPad : InteractableBase
    {
        public string LinkedPadId { get; }
        private TeleportPad _linkedPad;

        public TeleportPad(string id, GridCoord position, string linkedPadId) : base(id, position)
        {
            LinkedPadId = linkedPadId;
        }

        public void LinkTo(TeleportPad pad) => _linkedPad = pad;

        public override void OnEnter(GridActor actor)
        {
            if (_linkedPad != null && actor.Position != _linkedPad.Position)
            {
                actor.SetPosition(_linkedPad.Position);
            }
        }
    }
}
