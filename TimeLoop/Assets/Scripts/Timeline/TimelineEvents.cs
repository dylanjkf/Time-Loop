using System;
using TimeLoop.Actors;

namespace TimeLoop.Timeline
{
    /// <summary>
    /// The only channel other systems (UI, Audio, VFX, Achievements) use to react to the time-loop
    /// mechanic — nothing reaches into TimeLoopManager's internals directly, which keeps every
    /// downstream system fully decoupled from the core mechanic's implementation and safe to
    /// build/iterate on in parallel.
    /// </summary>
    public sealed class TimelineEvents
    {
        /// <summary>Fired once a new loop's recording begins, carrying the new loop's index (0-based).</summary>
        public event Action<int> OnLoopStart;

        /// <summary>Fired the instant a loop ends, carrying the just-completed recording.</summary>
        public event Action<RecordedTimeline> OnLoopEnd;

        /// <summary>Fired once the ghost for a just-completed loop has been spawned into the scene.</summary>
        public event Action<GhostAgent> OnGhostSpawned;

        /// <summary>Fired once when the countdown crosses GameSettings.CriticalTimeThreshold, carrying seconds remaining.</summary>
        public event Action<float> OnTimeCritical;

        /// <summary>Fired every simulation tick, carrying the tick number — HUD timer/loop displays subscribe to this.</summary>
        public event Action<int> OnTick;

        /// <summary>Fired when the live player's current attempt is discarded by a Hazard (see Interactables/Hazard.cs).</summary>
        public event Action OnHazardReset;

        public void RaiseLoopStart(int loopIndex) => OnLoopStart?.Invoke(loopIndex);
        public void RaiseLoopEnd(RecordedTimeline timeline) => OnLoopEnd?.Invoke(timeline);
        public void RaiseGhostSpawned(GhostAgent ghost) => OnGhostSpawned?.Invoke(ghost);
        public void RaiseTimeCritical(float secondsRemaining) => OnTimeCritical?.Invoke(secondsRemaining);
        public void RaiseTick(int tick) => OnTick?.Invoke(tick);
        public void RaiseHazardReset() => OnHazardReset?.Invoke();
    }
}
