namespace TimeLoop.Audio
{
    /// <summary>
    /// Identifies every ambient music track in the game. Indexes into
    /// <see cref="AudioManager"/>'s music clip array must match this enum's declaration order.
    /// </summary>
    public enum MusicTrackId
    {
        MainMenu,
        World1,
        World2,
        World3,
        World4,
        Infinite,
        DailyChallenge
    }
}
