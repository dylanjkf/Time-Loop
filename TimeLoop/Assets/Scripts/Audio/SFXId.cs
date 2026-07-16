namespace TimeLoop.Audio
{
    /// <summary>
    /// Identifies every one-shot sound effect in the game. Indexes into
    /// <see cref="AudioManager"/>'s SFX clip array must match this enum's declaration order.
    /// </summary>
    public enum SFXId
    {
        Footstep,
        SwitchToggle,
        PlateActivate,
        DoorOpen,
        BoxPush,
        LoopTransition,
        GhostSpawn,
        GoalReached,
        HazardHit,
        TimeCritical,
        ButtonClick,
        UIConfirm
    }
}
