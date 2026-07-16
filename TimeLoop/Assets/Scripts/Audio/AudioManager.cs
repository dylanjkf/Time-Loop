using TimeLoop.Actors;
using TimeLoop.Timeline;
using UnityEngine;

namespace TimeLoop.Audio
{
    /// <summary>
    /// The single audio entry point for the whole game: sparse, ambient, futuristic music per
    /// world and one-shot SFX for footsteps, switches, doors, loop transitions, and goals. Like
    /// every other peripheral system it only ever reacts to <see cref="TimelineEvents"/> and to
    /// explicit calls from UI — it never reaches into PlayerController, GhostAgent, GridWorld, or
    /// any other gameplay internals directly. See docs/TECHNICAL_ARCHITECTURE.md section 4.3.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        /// <summary>Indexed to match MusicTrackId's declaration order (MainMenu, World1, World2, ...).</summary>
        [SerializeField] private AudioClip[] _musicClipsByTrackId;

        /// <summary>Indexed to match SFXId's declaration order (Footstep, SwitchToggle, ...).</summary>
        [SerializeField] private AudioClip[] _sfxClipsById;

        private TimelineEvents _subscribedEvents;

        /// <summary>Plays a one-shot SFX. No-op if the clip array is missing, too short, or the clip itself is unassigned.</summary>
        public void PlaySfx(SFXId id)
        {
            var clip = GetClip(_sfxClipsById, (int)id);
            if (clip == null || _sfxSource == null) return;

            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Maps a 1-based world number (1-4) to its music track, falling back to MainMenu for anything else.</summary>
        public void PlayMusicForWorld(int world)
        {
            var trackId = world switch
            {
                1 => MusicTrackId.World1,
                2 => MusicTrackId.World2,
                3 => MusicTrackId.World3,
                4 => MusicTrackId.World4,
                _ => MusicTrackId.MainMenu
            };

            PlayMusic(trackId);
        }

        /// <summary>Switches the music source to the given track and plays it. No-op if the clip is missing/unassigned.</summary>
        public void PlayMusic(MusicTrackId id)
        {
            var clip = GetClip(_musicClipsByTrackId, (int)id);
            if (clip == null || _musicSource == null) return;

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource == null) return;

            _musicSource.Stop();
        }

        /// <summary>Mutes/unmutes music without touching playback position, so it can resume seamlessly when re-enabled.</summary>
        public void SetMusicEnabled(bool enabled)
        {
            if (_musicSource == null) return;

            _musicSource.mute = !enabled;
        }

        public void SetSoundEnabled(bool enabled)
        {
            if (_sfxSource == null) return;

            _sfxSource.mute = !enabled;
        }

        /// <summary>
        /// Wires this manager up to the loop's event channel. Safe to call with null (no-op) and
        /// safe to call again with a new instance — any previous subscription is torn down first.
        /// </summary>
        public void SubscribeToTimeline(TimelineEvents events)
        {
            if (_subscribedEvents != null)
            {
                _subscribedEvents.OnLoopEnd -= HandleLoopEnd;
                _subscribedEvents.OnGhostSpawned -= HandleGhostSpawned;
                _subscribedEvents.OnTimeCritical -= HandleTimeCritical;
            }

            _subscribedEvents = events;

            if (_subscribedEvents == null) return;

            _subscribedEvents.OnLoopEnd += HandleLoopEnd;
            _subscribedEvents.OnGhostSpawned += HandleGhostSpawned;
            _subscribedEvents.OnTimeCritical += HandleTimeCritical;
        }

        private void HandleLoopEnd(RecordedTimeline timeline) => PlaySfx(SFXId.LoopTransition);

        private void HandleGhostSpawned(GhostAgent ghost) => PlaySfx(SFXId.GhostSpawn);

        private void HandleTimeCritical(float secondsRemaining) => PlaySfx(SFXId.TimeCritical);

        private static AudioClip GetClip(AudioClip[] clips, int index)
        {
            if (clips == null || index < 0 || index >= clips.Length) return null;

            return clips[index];
        }

        private void OnDestroy()
        {
            SubscribeToTimeline(null);
        }
    }
}
