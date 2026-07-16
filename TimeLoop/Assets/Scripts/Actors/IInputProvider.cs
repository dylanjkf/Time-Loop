using TimeLoop.Grid;

namespace TimeLoop.Actors
{
    /// <summary>Supplies one InputCommand per simulation tick, for either a live or a recorded actor.</summary>
    public interface IInputProvider
    {
        InputCommand GetCommand(int tick);
    }
}
