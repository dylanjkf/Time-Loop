using TimeLoop.Actors;

namespace TimeLoop.Grid
{
    /// <summary>Pairs one actor (player or ghost) with its command for the current tick.</summary>
    public readonly struct ActorTickInput
    {
        public readonly GridActor Actor;
        public readonly InputCommand Command;

        public ActorTickInput(GridActor actor, InputCommand command)
        {
            Actor = actor;
            Command = command;
        }
    }

    public readonly struct BoxPush
    {
        public readonly Interactables.MovableBox Box;
        public readonly GridCoord NewPosition;

        public BoxPush(Interactables.MovableBox box, GridCoord newPosition)
        {
            Box = box;
            NewPosition = newPosition;
        }
    }
}
