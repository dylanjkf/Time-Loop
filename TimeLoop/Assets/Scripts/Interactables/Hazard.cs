using System;
using TimeLoop.Actors;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Instantly resets the current loop the moment the live player steps onto it — see
    /// docs/TECHNICAL_ARCHITECTURE.md section 5, rule 7. Never affects ghosts: a ghost's recorded
    /// timeline is, by construction, a run that already avoided every hazard (a run that touched
    /// one would have been discarded and re-recorded before it could ever become a ghost).
    /// TimeLoopManager subscribes to <see cref="OnPlayerHit"/> for every hazard in a loaded level.
    /// </summary>
    public sealed class Hazard : InteractableBase
    {
        public event Action<GridActor> OnPlayerHit;

        public Hazard(string id, GridCoord position) : base(id, position)
        {
        }

        public override void OnEnter(GridActor actor)
        {
            if (!actor.IsGhost)
            {
                OnPlayerHit?.Invoke(actor);
            }
        }
    }
}
