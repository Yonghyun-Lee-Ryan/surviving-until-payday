using System.IO;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 전 씬 UI 겹침·간격·사건 패널 네모/바인딩을 일괄 수정한다.
    /// </summary>
    public static class UiLayoutFixSetup
    {
        private const string CatalogPath = "Assets/Data/Art/ArtCatalog.asset";
        private const string ResourcesCatalogPath = "Assets/Resources/Art/ArtCatalog.asset";

        [MenuItem("Tools/Surviving Until Payday/Fix UI Layout (All Scenes)")]
        public static void Setup()
        {
            MediaPackSetup.Setup();
            FixGameScene();
            FixMainMenuScene();
            FixResultScene();
            Debug.Log(
                "[UiLayoutFixSetup] Game/MainMenu/Result 레이아웃 + EventPanel 네모 제거 + ArtCatalog 재연결 완료.");
        }

        private static void FixGameScene()
        {
            const string path = "Assets/Scenes/Game.unity";
            if (!File.Exists(path))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var eventPanel = Object.FindAnyObjectByType<EventPanelView>();
            var presenter = Object.FindAnyObjectByType<GamePlayPresenter>();
            if (eventPanel == null)
            {
                Debug.LogWarning("[UiLayoutFixSetup] EventPanelView missing.");
                return;
            }

            var root = eventPanel.transform as RectTransform;
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(0f, 70f);
            root.sizeDelta = new Vector2(-32f, 620f);

            DestroyNamed(root, "BgPlaceholder");
            DestroyNamed(root, "FacePlaceholder");
            DestroyNamed(root, "Placeholder");

            // Illustration → Background 통일
            var illustration = root.Find("Illustration");
            var backgroundTf = root.Find("Background");
            if (backgroundTf == null && illustration != null)
            {
                illustration.name = "Background";
                backgroundTf = illustration;
            }

            if (backgroundTf == null)
            {
                var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                backgroundTf = go.transform;
            }

            var bgRect = backgroundTf as RectTransform;
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.anchoredPosition = new Vector2(0f, -8f);
            bgRect.sizeDelta = new Vector2(-16f, 360f);
            var bgImage = backgroundTf.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.preserveAspect = true;
                bgImage.raycastTarget = false;
                bgImage.color = Color.white;
            }

            DestroyNamed(backgroundTf, "BgPlaceholder");
            DestroyNamed(backgroundTf, "Placeholder");

            // 남자 초상화 제거
            var expressionTf = root.Find("Expression");
            if (expressionTf != null)
            {
                expressionTf.gameObject.SetActive(false);
                var exImageHidden = expressionTf.GetComponent<Image>();
                if (exImageHidden != null)
                {
                    exImageHidden.enabled = false;
                    exImageHidden.sprite = null;
                }
            }

            var title = EnsureText(root, "Title", "사건 제목", 38);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, -50f);
            titleRect.sizeDelta = new Vector2(-32f, 48f);

            var description = EnsureText(root, "Description", "사건 설명", 32);
            var descRect = description.rectTransform;
            descRect.anchorMin = new Vector2(0f, 0.5f);
            descRect.anchorMax = new Vector2(1f, 0.5f);
            descRect.pivot = new Vector2(0.5f, 0.5f);
            descRect.anchoredPosition = new Vector2(0f, -160f);
            descRect.sizeDelta = new Vector2(-32f, 150f);
            description.fontSize = 32;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.alignment = TextAnchor.UpperCenter;

            var exImage = expressionTf != null ? expressionTf.GetComponent<Image>() : null;
            eventPanel.Bind(title, description, bgImage, null, exImage, null);
            EditorUtility.SetDirty(eventPanel);

            // HUD: 게이지 라벨(건강/스트레스) 복구
            var hud = Object.FindAnyObjectByType<GameHudView>()?.transform as RectTransform;
            if (hud != null)
            {
                hud.anchorMin = new Vector2(0f, 1f);
                hud.anchorMax = new Vector2(1f, 1f);
                hud.pivot = new Vector2(0.5f, 1f);
                hud.anchoredPosition = new Vector2(0f, -12f);
                hud.sizeDelta = new Vector2(-48f, 300f);
                FixGaugeLabels(hud);
            }

            var choice = Object.FindAnyObjectByType<ChoicePanelView>();
            if (choice != null)
            {
                var choiceRect = choice.transform as RectTransform;
                choiceRect.anchorMin = new Vector2(0f, 0f);
                choiceRect.anchorMax = new Vector2(1f, 0f);
                choiceRect.pivot = new Vector2(0.5f, 0f);
                choiceRect.anchoredPosition = new Vector2(0f, 24f);
                choiceRect.sizeDelta = new Vector2(-48f, 430f);
                var offsets = new[] { 250f, 148f, 46f };
                for (var i = 0; i < 3; i++)
                {
                    var button = choiceRect.Find($"Choice_{i}") as RectTransform;
                    if (button == null)
                    {
                        continue;
                    }

                    button.anchorMin = new Vector2(0f, 0f);
                    button.anchorMax = new Vector2(1f, 0f);
                    button.pivot = new Vector2(0.5f, 0f);
                    button.anchoredPosition = new Vector2(0f, offsets[i]);
                    button.sizeDelta = new Vector2(-40f, 88f);
                }

                choice.EnsureRerollButton();
            }

            if (presenter != null)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogPath)
                              ?? AssetDatabase.LoadAssetAtPath<ArtCatalog>(ResourcesCatalogPath);
                var so = new SerializedObject(presenter);
                so.FindProperty("artCatalog").objectReferenceValue = catalog;
                so.FindProperty("eventPanelView").objectReferenceValue = eventPanel;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }

            EnsureFontBootstrap();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void FixMainMenuScene()
        {
            // 특성 스크롤 포함 RunStart 패널을 다시 깐다.
            MainMenuRunStartSetup.Setup();

            const string path = "Assets/Scenes/MainMenu.unity";
            if (!File.Exists(path))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var safe = GameObject.Find("SafeArea")?.transform;
            if (safe == null)
            {
                return;
            }

            SetAnchored(safe.Find("Title") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 760f),
                new Vector2(920f, 90f));
            var sceneLabel = safe.Find("SceneLabel");
            if (sceneLabel != null)
            {
                sceneLabel.gameObject.SetActive(false);
            }

            SetAnchored(safe.Find("ActionButton") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 560f),
                new Vector2(520f, 86f));
            SetAnchored(safe.Find("ContinueButton") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 356f),
                new Vector2(520f, 86f));
            var settings = safe.Find("SettingsButton") as RectTransform;
            if (settings != null)
            {
                settings.anchorMin = settings.anchorMax = new Vector2(1f, 1f);
                settings.pivot = new Vector2(1f, 1f);
                settings.anchoredPosition = new Vector2(-24f, -24f);
                settings.sizeDelta = new Vector2(140f, 72f);
            }

            SetAnchored(safe.Find("CodexPanel") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -620f),
                new Vector2(920f, 260f));

            EnsureFontBootstrap();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void FixGaugeLabels(Transform hud)
        {
            ApplyGaugeName(hud.Find("HealthGauge"), "건강");
            ApplyGaugeName(hud.Find("StressGauge"), "스트레스");
            ApplyGaugeName(hud.Find("HappinessGauge"), "행복도");
            ApplyGaugeName(hud.Find("CompanyGauge"), "회사 평가");

            for (var i = 0; i < 4; i++)
            {
                var names = new[] { "HealthGauge", "StressGauge", "HappinessGauge", "CompanyGauge" };
                var gauge = hud.Find(names[i]) as RectTransform;
                if (gauge == null)
                {
                    continue;
                }

                const int count = 4;
                const float pad = 0.02f;
                var slot = (1f - pad * 2f) / count;
                gauge.anchorMin = new Vector2(pad + slot * i, 0f);
                gauge.anchorMax = new Vector2(pad + slot * (i + 1), 0f);
                gauge.pivot = new Vector2(0.5f, 0f);
                gauge.anchoredPosition = new Vector2(0f, 12f);
                gauge.sizeDelta = new Vector2(-8f, 140f);
            }
        }

        private static void ApplyGaugeName(Transform gaugeRoot, string displayName)
        {
            if (gaugeRoot == null)
            {
                return;
            }

            var nameLabel = gaugeRoot.Find("Name")?.GetComponent<Text>();
            if (nameLabel == null)
            {
                var go = new GameObject("Name", typeof(RectTransform));
                go.transform.SetParent(gaugeRoot, false);
                nameLabel = go.AddComponent<Text>();
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 42f);
                rect.sizeDelta = new Vector2(-12f, 32f);
            }

            nameLabel.text = displayName;
            nameLabel.fontSize = 24;
            nameLabel.alignment = TextAnchor.MiddleCenter;
            nameLabel.color = new Color(0.15f, 0.16f, 0.2f, 1f);
            nameLabel.font = Resources.Load<Font>("Fonts/NotoSansKR-Bold")
                             ?? Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                             ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameLabel.gameObject.SetActive(true);

            var gauge = gaugeRoot.GetComponent<StatGaugeView>();
            if (gauge != null)
            {
                gauge.SetName(displayName);
                var so = new SerializedObject(gauge);
                so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gauge);
            }
        }

        private static void FixResultScene()
        {
            const string path = "Assets/Scenes/Result.unity";
            if (!File.Exists(path))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var safe = GameObject.Find("SafeArea")?.transform;
            if (safe == null)
            {
                return;
            }

            SetAnchored(safe.Find("Title") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 720f),
                new Vector2(900f, 70f));
            SetAnchored(safe.Find("EndingTitle") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 580f),
                new Vector2(900f, 60f));
            SetAnchored(safe.Find("EndingDesc") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 430f),
                new Vector2(900f, 140f));
            SetAnchored(safe.Find("Days") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f),
                new Vector2(900f, 48f));
            SetAnchored(safe.Find("Cash") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f),
                new Vector2(900f, 48f));
            SetAnchored(safe.Find("Stats") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f),
                new Vector2(900f, 90f));
            SetAnchored(safe.Find("XP") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f),
                new Vector2(900f, 48f));
            SetAnchored(safe.Find("Unlock") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f),
                new Vector2(900f, 48f));
            SetAnchored(safe.Find("DoubleXpAdButton") as RectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -200f), new Vector2(480f, 90f));
            SetAnchored(safe.Find("BackButton") as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -330f),
                new Vector2(480f, 110f));

            EnsureFontBootstrap();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFontBootstrap()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponent<UiFontBootstrap>() == null)
            {
                canvas.gameObject.AddComponent<UiFontBootstrap>();
                EditorUtility.SetDirty(canvas.gameObject);
            }
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void DestroyNamed(Transform root, string name)
        {
            if (root == null)
            {
                return;
            }

            var child = root.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
                return;
            }

            // 깊은 자식도 제거
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != root && all[i].name == name)
                {
                    Object.DestroyImmediate(all[i].gameObject);
                }
            }
        }

        private static Text EnsureText(Transform parent, string name, string defaultText, int fontSize)
        {
            var existing = parent.Find(name)?.GetComponent<Text>();
            if (existing != null)
            {
                existing.fontSize = fontSize;
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.16f, 0.2f, 1f);
            return text;
        }
    }
}
