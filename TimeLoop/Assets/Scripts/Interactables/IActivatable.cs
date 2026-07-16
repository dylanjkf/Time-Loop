using System;

namespace TimeLoop.Interactables
{
    /// <summary>Common contract for anything a Door can gate on — Switch and PressurePlate both implement this.</summary>
    public interface IActivatable
    {
        string Id { get; }
        bool IsActivated { get; }
        event Action<IActivatable> OnActivationChanged;
    }
}
