using TimeLoop.UI.Menus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.EditorTools
{
    /// <summary>
    /// Programmatically constructs the main menu scene instead of hand-authoring fragile .unity
    /// YAML. Builds a Canvas holding a bare MainMenuController - the actual buttons are
    /// art-dependent and are left for the designer to add (and wire into the controller's
    /// serialized Button fields) by hand in the Editor.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/MainMenu.unity";

        [MenuItem("Time Loop/Build Scenes/Main Menu Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            var controllerGo = new GameObject("MainMenuController");
            controllerGo.transform.SetParent(canvasGo.transform, false);
            controllerGo.AddComponent<MainMenuController>();

            Debug.LogWarning(
                "MainMenuSceneBuilder: created an empty 'MainMenuController' GameObject under 'Main Menu Canvas' " +
                "with no child buttons - those are art-dependent. Add the Campaign/Daily Challenge/Infinite " +
                "Mode/Settings buttons by hand in the Editor and assign them to MainMenuController's serialized " +
                "Button fields.");

            EnsureFolderExists(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"MainMenuSceneBuilder: saved main menu scene to {ScenePath}.");
        }

        private static void EnsureFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

            var parts = assetFolderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
