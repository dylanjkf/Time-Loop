using TimeLoop.Actors;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Implemented by grid entities that react to actors. GridWorld.Step dispatches all three
    /// hooks in the same fixed, deterministic order used for movement (every active ghost,
    /// oldest-recorded first, then the live player). Entities that don't care about a given hook
    /// simply leave it as a no-op (see InteractableBase).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Called the tick an actor's resolved position becomes this entity's tile.</summary>
        void OnEnter(GridActor actor);

        /// <summary>Called the tick an actor's resolved position leaves this entity's tile.</summary>
        void OnExit(GridActor actor);

        /// <summary>Called when an actor issues InputCommand.Interact while standing on this entity's tile.</summary>
        void OnInteract(GridActor actor);
    }
}
