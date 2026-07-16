using TimeLoop.Actors;
using TimeLoop.Puzzle;
using TimeLoop.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.EditorTools
{
    /// <summary>
    /// Programmatically constructs the gameplay scene instead of hand-authoring fragile .unity
    /// YAML. Wires up TimeLoopManager's PlayerController/PuzzleManager references via
    /// SerializedObject; the GameSettings asset and ghost prefab are left for the designer to
    /// assign by hand (see the logged instructions below).
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/Gameplay.unity";

        private static readonly Vector3 CameraPosition = new Vector3(4f, 10f, -4f);
        private static readonly Vector3 CameraEulerAngles = new Vector3(55f, 0f, 0f);

        [MenuItem("Time Loop/Build Scenes/Gameplay Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            RepositionMainCamera();

            var gameManagersGo = new GameObject("GameManagers");
            var timeLoopManager = gameManagersGo.AddComponent<TimeLoopManager>();
            var puzzleManager = gameManagersGo.AddComponent<PuzzleManager>();

            var playerGo = new GameObject("Player");
            playerGo.AddComponent<ActorVisual>();
            var playerController = playerGo.AddComponent<PlayerController>();

            WireUpTimeLoopManager(timeLoopManager, playerController, puzzleManager);

            BuildHudCanvasPlaceholder();

            EnsureFolderExists(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"GameplaySceneBuilder: saved gameplay scene to {ScenePath}.");
        }

        private static void RepositionMainCamera()
        {
            var mainCameraGo = GameObject.Find("Main Camera");
            if (mainCameraGo == null)
            {
                Debug.LogWarning(
                    "GameplaySceneBuilder: no 'Main Camera' GameObject was found in the default scene setup; " +
                    "skipping camera reposition.");
                return;
            }

            mainCameraGo.transform.position = CameraPosition;
            mainCameraGo.transform.rotation = Quaternion.Euler(CameraEulerAngles);
        }

        private static void WireUpTimeLoopManager(
            TimeLoopManager timeLoopManager,
            PlayerController playerController,
            PuzzleManager puzzleManager)
        {
            var serializedManager = new SerializedObject(timeLoopManager);

            var playerControllerProperty = serializedManager.FindProperty("_playerController");
            if (playerControllerProperty != null)
            {
                playerControllerProperty.objectReferenceValue = playerController;
            }
            else
            {
                Debug.LogError(
                    "GameplaySceneBuilder: TimeLoopManager no longer has a '_playerController' serialized field; " +
                    "the scene builder needs updating to match.");
            }

            var puzzleManagerProperty = serializedManager.FindProperty("_puzzleManager");
            if (puzzleManagerProperty != null)
            {
                puzzleManagerProperty.objectReferenceValue = puzzleManager;
            }
            else
            {
                Debug.LogError(
                    "GameplaySceneBuilder: TimeLoopManager no longer has a '_puzzleManager' serialized field; " +
                    "the scene builder needs updating to match.");
            }

            serializedManager.ApplyModifiedProperties();

            // _gameSettings and _ghostPrefab are intentionally left unassigned - see the log below.
            Debug.LogWarning(
                "GameplaySceneBuilder: the TimeLoopManager on 'GameManagers' still needs its Game Settings field " +
                "assigned by hand. Create an asset via Assets > Create > Time Loop > Game Settings (or reuse an " +
                "existing one) and drag it onto the '_gameSettings' field before this scene can run. The ghost " +
                "prefab field was left empty too - TimeLoopManager falls back to constructing a plain ghost " +
                "GameObject (ActorVisual + GhostAgent) at runtime whenever no prefab is assigned, so assigning one " +
                "is optional and only needed if you want a custom ghost visual.");
        }

        private static void BuildHudCanvasPlaceholder()
        {
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            Debug.Log(
                "GameplaySceneBuilder: created an empty 'HUD Canvas' placeholder - HUD prefabs (timer, buttons, " +
                "star readout, etc.) should be added underneath it by hand in the Editor.");
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
