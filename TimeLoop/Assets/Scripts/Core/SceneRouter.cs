using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeLoop.Core
{
    /// <summary>Thin, centralized wrapper around scene loading so every menu button goes through one place.</summary>
    public static class SceneRouter
    {
        public const string MainMenu = "MainMenu";
        public const string Gameplay = "Gameplay";

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);
        public static void LoadGameplay() => SceneManager.LoadScene(Gameplay);
        public static void ReloadCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
