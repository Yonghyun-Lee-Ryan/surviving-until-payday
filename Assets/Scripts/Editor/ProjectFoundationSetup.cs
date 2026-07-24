using System.IO;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 1: 폴더, Scene, Canvas, Build Settings를 한 번에 구성한다.
    /// </summary>
    public static class ProjectFoundationSetup
    {
        private const string ScenesRoot = "Assets/Scenes";
        private const string MenuPath = "Tools/Surviving Until Payday/Setup Project Foundation";

        private static readonly string[] RequiredFolders =
        {
            "Assets/Art",
            "Assets/Audio",
            "Assets/Data",
            "Assets/Data/Events",
            "Assets/Data/Jobs",
            "Assets/Data/Traits",
            "Assets/Data/Endings",
            "Assets/Prefabs",
            "Assets/Scenes",
            "Assets/Scripts",
            "Assets/Scripts/Core",
            "Assets/Scripts/Data",
            "Assets/Scripts/Events",
            "Assets/Scripts/UI",
            "Assets/Scripts/Save",
            "Assets/Scripts/Ads",
            "Assets/Scripts/Analytics",
            "Assets/Scripts/Debug",
            "Assets/Scripts/Editor",
            "Assets/Tests"
        };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            EnsureFolders();
            ApplyPortraitPlayerSettings();

            var bootstrapPath = CreateBootstrapScene();
            var mainMenuPath = CreateUiScene(
                SceneNames.MainMenu,
                "월급날까지 살아남기",
                "게임 시작",
                typeof(MainMenuController),
                "startGameButton");
            var gamePath = CreateUiScene(
                SceneNames.Game,
                "Game (임시)",
                "임시 종료",
                typeof(GameSceneController),
                "tempEndButton");
            var resultPath = CreateUiScene(
                SceneNames.Result,
                "Result (임시)",
                "메인 메뉴로",
                typeof(ResultSceneController),
                "backToMenuButton");

            ConfigureBuildSettings(bootstrapPath, mainMenuPath, gamePath, resultPath);
            RemoveSampleSceneIfPresent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(bootstrapPath);
            Debug.Log(
                "[ProjectFoundationSetup] Complete.\n" +
                "1) Play Mode로 Bootstrap → MainMenu 이동을 확인하세요.\n" +
                "2) 게임 시작 → Game, 임시 종료 → Result를 확인하세요.");
        }

        private static void EnsureFolders()
        {
            foreach (var folder in RequiredFolders)
            {
                EnsureFolder(folder);
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                Debug.LogError($"[ProjectFoundationSetup] Invalid folder path: {assetPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void ApplyPortraitPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.Android.renderOutsideSafeArea = true;
        }

        private static string CreateBootstrapScene()
        {
            var scenePath = $"{ScenesRoot}/{SceneNames.Bootstrap}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigureMainCamera(Camera.main, new Color(0.12f, 0.14f, 0.18f));

            var appRootObject = new GameObject("AppRoot");
            appRootObject.AddComponent<PortraitOrientationLocker>();
            var appRoot = appRootObject.AddComponent<AppRoot>();

            var sceneLoaderObject = new GameObject("SceneLoader");
            sceneLoaderObject.transform.SetParent(appRootObject.transform, false);
            var sceneLoader = sceneLoaderObject.AddComponent<SceneLoader>();

            var so = new SerializedObject(appRoot);
            so.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
            so.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapObject = new GameObject("BootstrapInitializer");
            bootstrapObject.AddComponent<BootstrapInitializer>();

            CreateInfoCanvas("Bootstrap", "초기화 중...");

            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        private static string CreateUiScene(
            string sceneName,
            string title,
            string buttonLabel,
            System.Type controllerType,
            string buttonPropertyName)
        {
            var scenePath = $"{ScenesRoot}/{sceneName}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var background = sceneName switch
            {
                SceneNames.MainMenu => new Color(0.95f, 0.94f, 0.90f),
                SceneNames.Game => new Color(0.90f, 0.93f, 0.96f),
                _ => new Color(0.93f, 0.91f, 0.95f)
            };
            ConfigureMainCamera(Camera.main, background);

            var canvasRoot = CreateInfoCanvas(sceneName, title);
            var safeArea = canvasRoot.transform.Find("SafeArea");
            if (safeArea == null)
            {
                Debug.LogError($"[ProjectFoundationSetup] SafeArea missing in {sceneName}.");
                EditorSceneManager.SaveScene(scene, scenePath);
                return scenePath;
            }

            var button = CreateButton(safeArea, buttonLabel, new Vector2(0f, -220f));
            var controller = canvasRoot.AddComponent(controllerType);
            var serialized = new SerializedObject(controller);
            var buttonProperty = serialized.FindProperty(buttonPropertyName);
            if (buttonProperty == null)
            {
                Debug.LogError(
                    $"[ProjectFoundationSetup] Property '{buttonPropertyName}' not found on {controllerType.Name}.");
            }
            else
            {
                buttonProperty.objectReferenceValue = button;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureEventSystem();
            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        private static GameObject CreateInfoCanvas(string sceneName, string title)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            CanvasSetupUtility.ApplyPortraitCanvasScaler(scaler);
            canvasObject.AddComponent<GraphicRaycaster>();

            var safeAreaObject = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            var safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
            StretchFull(safeAreaRect);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            CreateLabel(safeAreaObject.transform, "Title", title, 64, new Vector2(0f, 320f));
            CreateLabel(
                safeAreaObject.transform,
                "SceneLabel",
                $"Scene: {sceneName}",
                36,
                new Vector2(0f, 220f));

            return canvasObject;
        }

        private static void CreateLabel(
            Transform parent,
            string objectName,
            string text,
            int fontSize,
            Vector2 anchoredPosition)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = anchoredPosition;

            var label = labelObject.AddComponent<Text>();
            label.text = text;
            label.font = ResolveUiFont();
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.15f, 0.15f, 0.18f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject("ActionButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 120f);
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.42f, 0.55f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            StretchFull(textObject.GetComponent<RectTransform>());

            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = ResolveUiFont();
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return button;
        }

        private static Font ResolveUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                return font;
            }

            Debug.LogWarning("[ProjectFoundationSetup] Builtin UI font not found. Text may be invisible.");
            return null;
        }

        private static void ConfigureMainCamera(Camera camera, Color background)
        {
            if (camera == null)
            {
                Debug.LogError("[ProjectFoundationSetup] Main Camera is missing.");
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureBuildSettings(
            string bootstrapPath,
            string mainMenuPath,
            string gamePath,
            string resultPath)
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(bootstrapPath, true),
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(gamePath, true),
                new EditorBuildSettingsScene(resultPath, true)
            };

            EditorBuildSettings.scenes = scenes;
        }

        private static void RemoveSampleSceneIfPresent()
        {
            const string samplePath = "Assets/Scenes/SampleScene.unity";
            if (!File.Exists(samplePath))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "SampleScene 삭제",
                    "개발 단위 1 Scene으로 교체합니다. SampleScene.unity를 삭제할까요?",
                    "삭제",
                    "유지"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(samplePath);
        }
    }
}
