using System.IO;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Settings;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 15: 스플래시/동의, 설정, Release/AAB 준비.
    /// </summary>
    public static class ReleasePrepSetup
    {
        private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string ConfigFolder = "Assets/Data/Config";
        private const string PrivacyPath = ConfigFolder + "/PrivacyPolicyConfig.asset";
        private const string AndroidApplicationId = "com.surviveuntilpayday.game";

        [MenuItem("Tools/Surviving Until Payday/Setup Release Prep (Unit 15)")]
        public static void SetupAll()
        {
            var privacy = EnsurePrivacyConfig();
            SetupBootstrapSplash(privacy);
            SetupMainMenuSettings(privacy);
            ApplyAndroidReleasePlayerSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[ReleasePrepSetup] Unit 15 ready.\n" +
                "1) PrivacyPolicyConfig는 GitHub Pages Canonical URL입니다. Pages(Docs)를 켜세요.\n" +
                "2) Tools → Setup Android Release Signing 으로 Release Keystore를 연결하세요.\n" +
                "3) Tools → Assign Android Adaptive Icons (R-QA-08) 로 Adaptive Icon을 지정하세요.\n" +
                "4) Build Settings: Development Build OFF → Build App Bundle\n" +
                "5) Play Console 내부 테스트 트랙에 업로드하세요.");
        }

        [MenuItem("Tools/Surviving Until Payday/Apply Android AAB PlayerSettings (Unit 15)")]
        public static void ApplyAndroidReleasePlayerSettings()
        {
            ApplyAndroidReleasePlayerSettings(bumpVersionCode: true);
        }

        public static void ApplyAndroidReleasePlayerSettings(bool bumpVersionCode)
        {
            PlayerSettings.companyName = "SurviveUntilPayday";
            PlayerSettings.productName = "월급날까지 살아남기";
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
                || PlayerSettings.bundleVersion == "1.0")
            {
                PlayerSettings.bundleVersion = "0.1.0";
            }

            var previousCode = Mathf.Max(0, PlayerSettings.Android.bundleVersionCode);
            var nextCode = bumpVersionCode ? previousCode + 1 : previousCode;
            if (bumpVersionCode)
            {
                PlayerSettings.Android.bundleVersionCode = nextCode;
            }
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidApplicationId);
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

            // Unity 6 최소 지원 API 26 (Android 8.0)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.appCategory = "game";
            PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.muteOtherAudioSources = true;

            var appId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            Debug.Log(
                $"[ReleasePrepSetup] Android AAB settings applied. " +
                $"version={PlayerSettings.bundleVersion}, code={previousCode}→{nextCode}, " +
                $"id={appId}, minSdk=26, backend=IL2CPP, arch=ARM64, aab={EditorUserBuildSettings.buildAppBundle}");
        }

        /// <summary>
        /// Play Console 업로드용으로 Android versionCode를 1 올린다.
        /// </summary>
        public static int BumpAndroidBundleVersionCode()
        {
            var previous = Mathf.Max(0, PlayerSettings.Android.bundleVersionCode);
            var next = previous + 1;
            PlayerSettings.Android.bundleVersionCode = next;
            return next;
        }

        private static PrivacyPolicyConfig EnsurePrivacyConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Config");
            }

            var config = AssetDatabase.LoadAssetAtPath<PrivacyPolicyConfig>(PrivacyPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PrivacyPolicyConfig>();
                config.EditorSet(
                    PrivacyPolicyUrls.Canonical,
                    "본 게임은 광고(AdMob)·분석·크래시 수집을 위해 기기의 비식별 정보를 사용할 수 있습니다. " +
                    "EEA 등 일부 지역에서는 광고 동의(UMP) 화면이 이어서 표시됩니다. " +
                    "자세한 내용은 개인정보처리방침을 확인해 주세요.");
                AssetDatabase.CreateAsset(config, PrivacyPath);
            }
            else if (config.HasPlaceholderUrl)
            {
                config.EditorSet(PrivacyPolicyUrls.Canonical, config.SummaryText);
                EditorUtility.SetDirty(config);
            }

            var appRoot = Object.FindAnyObjectByType<AppRoot>();
            if (appRoot != null)
            {
                appRoot.BindPrivacyPolicy(config);
                EditorUtility.SetDirty(appRoot);
            }

            return config;
        }

        private static void SetupBootstrapSplash(PrivacyPolicyConfig privacy)
        {
            if (!File.Exists(BootstrapPath))
            {
                Debug.LogWarning("[ReleasePrepSetup] Bootstrap.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            EnsureBootstrapEventSystem();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }
            else if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            var splashRoot = canvas.transform.Find("SplashRoot");
            if (splashRoot == null)
            {
                var go = new GameObject("SplashRoot", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                var rect = go.GetComponent<RectTransform>();
                Stretch(rect);
                var image = go.AddComponent<Image>();
                image.color = new Color(0.08f, 0.1f, 0.14f, 1f);
                splashRoot = go.transform;
            }

            var title = EnsureText(splashRoot, "Title", "월급날까지 살아남기", 48, new Vector2(0f, 80f));
            var version = EnsureText(splashRoot, "Version", "v0.1.0", 28, new Vector2(0f, -40f));
            version.color = new Color(1f, 1f, 1f, 0.7f);
            title.color = Color.white;

            var consent = EnsureConsentPanel(canvas.transform, privacy);
            var splash = splashRoot.GetComponent<SplashController>()
                         ?? splashRoot.gameObject.AddComponent<SplashController>();
            splash.Bind(1.25f, version, title, consent);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureBootstrapEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static ConsentPanelView EnsureConsentPanel(Transform canvas, PrivacyPolicyConfig privacy)
        {
            var existing = Object.FindAnyObjectByType<ConsentPanelView>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("ConsentPanel", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            Stretch(root.GetComponent<RectTransform>());
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.82f);

            var card = CreatePanel(root.transform, "Card", new Vector2(0f, 0f), new Vector2(900f, 700f),
                new Color(0.15f, 0.17f, 0.2f, 1f));
            var summary = EnsureText(card.transform, "Summary", privacy.SummaryText, 28, new Vector2(0f, 80f));
            summary.rectTransform.sizeDelta = new Vector2(820f, 280f);
            summary.color = Color.white;
            summary.alignment = TextAnchor.UpperLeft;

            var privacyBtn = CreateButton(card.transform, "PrivacyButton", "개인정보처리방침", new Vector2(0f, -140f));
            var acceptBtn = CreateButton(card.transform, "AcceptButton", "동의하고 시작", new Vector2(0f, -240f));

            var view = root.AddComponent<ConsentPanelView>();
            view.Bind(root, summary, acceptBtn, privacyBtn, privacy);
            root.SetActive(false);
            return view;
        }

        private static void SetupMainMenuSettings(PrivacyPolicyConfig privacy)
        {
            if (!File.Exists(MainMenuPath))
            {
                Debug.LogWarning("[ReleasePrepSetup] MainMenu.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            var safe = GameObject.Find("SafeArea");
            if (controller == null || safe == null)
            {
                Debug.LogWarning("[ReleasePrepSetup] MainMenuController/SafeArea missing.");
                return;
            }

            var settingsBtnGo = safe.transform.Find("SettingsButton");
            Button settingsButton;
            if (settingsBtnGo == null)
            {
                settingsButton = CreateButton(safe.transform, "SettingsButton", "설정", new Vector2(400f, 800f));
                var rect = settingsButton.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, 80f);
            }
            else
            {
                settingsButton = settingsBtnGo.GetComponent<Button>();
            }

            var panel = Object.FindAnyObjectByType<SettingsPanelView>();
            if (panel == null)
            {
                panel = BuildSettingsPanel(safe.transform, privacy);
            }

            controller.BindSettings(settingsButton, panel);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static SettingsPanelView BuildSettingsPanel(Transform parent, PrivacyPolicyConfig privacy)
        {
            var root = CreatePanel(parent, "SettingsPanel", Vector2.zero, new Vector2(920f, 1200f),
                new Color(0.1f, 0.12f, 0.15f, 0.96f));
            var title = EnsureText(root.transform, "Title", "설정", 40, new Vector2(0f, 500f));
            title.color = Color.white;

            var soundToggle = CreateToggle(root.transform, "SoundToggle", "사운드", new Vector2(0f, 360f));
            var vibrationToggle = CreateToggle(root.transform, "VibrationToggle", "진동", new Vector2(0f, 260f));
            var bgm = CreateSlider(root.transform, "BgmSlider", new Vector2(0f, 180f));
            var sfx = CreateSlider(root.transform, "SfxSlider", new Vector2(0f, 100f));
            var privacyBtn = CreateButton(root.transform, "PrivacyButton", "개인정보처리방침", new Vector2(0f, 20f));
            var resetBtn = CreateButton(root.transform, "ResetSaveButton", "저장 데이터 초기화", new Vector2(0f, -100f));
            var mainMenuBtn = CreateButton(root.transform, "MainMenuButton", "메인 메뉴로", new Vector2(0f, -200f));
            var closeBtn = CreateButton(root.transform, "CloseButton", "닫기", new Vector2(0f, -300f));
            var version = EnsureText(root.transform, "Version", "v0.1.0", 24, new Vector2(0f, -400f));
            version.color = new Color(1f, 1f, 1f, 0.65f);

            var view = root.AddComponent<SettingsPanelView>();
            view.Bind(root, soundToggle, vibrationToggle, bgm, sfx, privacyBtn, resetBtn, mainMenuBtn, closeBtn, version, privacy);
            root.SetActive(false);
            return view;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos)
        {
            var go = CreatePanel(parent, name, pos, new Vector2(800f, 70f), new Color(0.2f, 0.22f, 0.26f, 1f));
            var labelText = EnsureText(go.transform, "Label", label, 30, new Vector2(-200f, 0f));
            labelText.color = Color.white;
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = go.GetComponent<Image>();
            toggle.isOn = true;
            return toggle;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 pos)
        {
            var go = CreatePanel(parent, name, pos, new Vector2(800f, 40f), new Color(0.25f, 0.27f, 0.3f, 1f));
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = CreatePanel(parent, name, pos, new Vector2(520f, 90f), new Color(0.25f, 0.45f, 0.55f, 1f));
            var text = EnsureText(go.transform, "Label", label, 30, Vector2.zero);
            text.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text EnsureText(Transform parent, string name, string value, int size, Vector2 pos)
        {
            var existing = parent.Find(name);
            Text text;
            if (existing != null)
            {
                text = existing.GetComponent<Text>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                text = go.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(860f, 80f);
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
