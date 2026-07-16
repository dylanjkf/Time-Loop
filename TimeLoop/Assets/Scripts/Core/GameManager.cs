using TimeLoop.Achievements;
using TimeLoop.Audio;
using TimeLoop.Daily;
using TimeLoop.Levels;
using TimeLoop.Monetization;
using TimeLoop.Save;
using UnityEngine;

namespace TimeLoop.Core
{
    /// <summary>
    /// The single boot-time owner of every cross-scene manager singleton, created once in the
    /// first-loaded scene (DontDestroyOnLoad) and exposed via <see cref="Instance"/>. Nobody else
    /// constructs a PlayerProfile/AudioManager/IAPManager/etc. directly — every menu, HUD, and
    /// gameplay screen reaches persistent state through this one place.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private AudioManager _audioManager;

        public PlayerProfile Profile { get; private set; }
        public AchievementSystem Achievements { get; private set; }
        public IAPManager IAP { get; private set; }
        public AdManager Ads { get; private set; }
        public ThemeUnlockSystem Themes { get; private set; }
        public DailyChallengeManager DailyChallenge { get; private set; }
        public LevelDatabase LevelDatabase { get; private set; }
        public AudioManager Audio => _audioManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Profile = PlayerProfile.LoadFromDisk();
            Achievements = new AchievementSystem(Profile);
            IAP = new IAPManager(Profile.Raw.IsPremiumUnlocked);
            Ads = new AdManager { AdsRemoved = Profile.Raw.IsPremiumUnlocked };
            Themes = new ThemeUnlockSystem(Profile.Raw.UnlockedThemes);
            DailyChallenge = new DailyChallengeManager();
            DailyChallenge.RestoreCompletionHistory(Profile.Raw.CompletedDailyPuzzleNumbers);
            LevelDatabase = new LevelDatabase(LevelLoader.LoadAllWorlds());

            IAP.OnPurchaseCompleted += HandlePremiumPurchased;

            if (_audioManager != null)
            {
                _audioManager.SetMusicEnabled(Profile.Raw.MusicEnabled);
                _audioManager.SetSoundEnabled(Profile.Raw.SoundEnabled);
            }
        }

        private void HandlePremiumPurchased()
        {
            Profile.Raw.IsPremiumUnlocked = true;
            Ads.AdsRemoved = true;
            Profile.Persist();
        }

        /// <summary>Call after Themes.Unlock/SetActiveTheme so the choice survives a restart.</summary>
        public void PersistThemeSelection()
        {
            Profile.Raw.UnlockedThemes = Themes.GetUnlockedThemeNames();
            Profile.Raw.SelectedTheme = Themes.ActiveTheme.ToString();
            Profile.Persist();
        }

        /// <summary>Call once the first time today's Daily Challenge is completed.</summary>
        public void RecordDailyCompletion()
        {
            DailyChallenge.MarkTodaysCompletion();
            Profile.Raw.CompletedDailyPuzzleNumbers = new System.Collections.Generic.List<int>(DailyChallenge.CompletionHistory);
            Profile.Persist();
        }

        private void OnApplicationQuit() => Profile?.Persist();

        private void OnApplicationPause(bool paused)
        {
            if (paused) Profile?.Persist();
        }

        private void OnDestroy()
        {
            if (IAP != null)
            {
                IAP.OnPurchaseCompleted -= HandlePremiumPurchased;
            }
        }
    }
}
