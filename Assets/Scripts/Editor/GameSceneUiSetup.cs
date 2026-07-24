using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 7: Game Scene 플레이 UI를 생성하고 Presenter에 연결한다.
    /// </summary>
    public static class GameSceneUiSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MenuPath = "Tools/Surviving Until Payday/Setup Game Scene UI (Unit 7)";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError(
                    "[GameSceneUiSetup] Game.unity not found. Run Setup Project Foundation first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            CleanupTempControllers();
            EnsureEventSystem();

            var canvas = EnsureCanvas();
            var safeArea = EnsureSafeArea(canvas.transform);
            ClearChildren(safeArea);

            var hud = BuildHud(safeArea);
            var eventPanel = BuildEventPanel(safeArea);
            var choicePanel = BuildChoicePanel(safeArea);
            var resultPopup = BuildResultPopup(safeArea);

            var presenterObject = new GameObject("GamePlayPresenter");
            Undo.RegisterCreatedObjectUndo(presenterObject, "Create GamePlayPresenter");
            var presenter = presenterObject.AddComponent<GamePlayPresenter>();
            presenter.BindViews(hud, eventPanel, choicePanel, resultPopup);

            AssignSampleData(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = presenterObject;

            Debug.Log(
                "[GameSceneUiSetup] Game Scene UI ready.\n" +
                "1) GamePlayPresenter에 Job/Events가 비어 있으면 Inspector에서 할당하세요.\n" +
                "2) Bootstrap → MainMenu → 게임 시작으로 플레이하세요.\n" +
                "3) Game 뷰를 1080x1920 Portrait로 맞추세요.");
        }

        private static void CleanupTempControllers()
        {
            foreach (var old in Object.FindObjectsByType<GameSceneController>(FindObjectsInactive.Include))
            {
                // Canvas에 붙어 있을 수 있으므로 컴포넌트만 제거한다.
                Object.DestroyImmediate(old);
            }

            foreach (var old in Object.FindObjectsByType<GamePlayPresenter>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(old.gameObject);
            }
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

        private static Canvas EnsureCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    CanvasSetupUtility.ApplyPortraitCanvasScaler(scaler);
                }

                return canvas;
            }

            var canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var newScaler = canvasObject.AddComponent<CanvasScaler>();
            CanvasSetupUtility.ApplyPortraitCanvasScaler(newScaler);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Transform EnsureSafeArea(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("SafeArea");
            if (existing != null)
            {
                if (existing.GetComponent<SafeAreaFitter>() == null)
                {
                    existing.gameObject.AddComponent<SafeAreaFitter>();
                }

                return existing;
            }

            var safeAreaObject = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasTransform, false);
            Stretch(safeAreaObject.GetComponent<RectTransform>());
            safeAreaObject.AddComponent<SafeAreaFitter>();
            return safeAreaObject.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static GameHudView BuildHud(Transform parent)
        {
            var root = CreatePanel(parent, "HUD", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(1000f, 420f), new Color(0.96f, 0.96f, 0.94f, 0.95f));

            var day = CreateText(root.transform, "DayLabel", "1일 (월)", 40, TextAnchor.MiddleLeft,
                new Vector2(-220f, 160f), new Vector2(480f, 60f));
            var cash = CreateText(root.transform, "CashLabel", "0원", 40, TextAnchor.MiddleRight,
                new Vector2(220f, 160f), new Vector2(480f, 60f));

            var crisis = CreatePanel(root.transform, "CrisisBanner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 95f), new Vector2(920f, 48f), new Color(0.85f, 0.3f, 0.25f, 0.9f));
            crisis.SetActive(false);
            var crisisText = CreateText(crisis.transform, "CrisisLabel", "", 28, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(880f, 40f));
            crisisText.color = Color.white;

            var health = CreateGauge(root.transform, "HealthGauge", new Vector2(-345f, -20f));
            var stress = CreateGauge(root.transform, "StressGauge", new Vector2(-115f, -20f));
            var happiness = CreateGauge(root.transform, "HappinessGauge", new Vector2(115f, -20f));
            var company = CreateGauge(root.transform, "CompanyGauge", new Vector2(345f, -20f));

            var hud = root.AddComponent<GameHudView>();
            hud.BindLabels(day, cash, crisis, crisisText);
            hud.BindGauges(
                health.GetComponent<StatGaugeView>(),
                stress.GetComponent<StatGaugeView>(),
                happiness.GetComponent<StatGaugeView>(),
                company.GetComponent<StatGaugeView>());
            return hud;
        }

        private static GameObject CreateGauge(Transform parent, string name, Vector2 anchoredPos)
        {
            var root = CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                anchoredPos, new Vector2(210f, 110f), new Color(0.9f, 0.9f, 0.9f, 0.95f));

            var nameLabel = CreateText(root.transform, "Name", name, 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 32f), new Vector2(190f, 30f));
            var valueLabel = CreateText(root.transform, "Value", "0", 28, TextAnchor.MiddleCenter,
                new Vector2(0f, -30f), new Vector2(190f, 30f));

            var track = CreatePanel(root.transform, "Track", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(180f, 18f), new Color(0.82f, 0.82f, 0.82f, 1f));
            var fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(track.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.25f, 0.55f, 0.45f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.5f;

            var gauge = root.AddComponent<StatGaugeView>();
            var so = new SerializedObject(gauge);
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("valueLabel").objectReferenceValue = valueLabel;
            so.FindProperty("fillImage").objectReferenceValue = fill;
            so.FindProperty("backgroundImage").objectReferenceValue = track.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static EventPanelView BuildEventPanel(Transform parent)
        {
            var root = CreatePanel(parent, "EventPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f), new Vector2(960f, 620f), new Color(0.93f, 0.94f, 0.96f, 1f));

            var illustration = CreatePanel(root.transform, "Illustration", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(880f, 260f), new Color(0.78f, 0.82f, 0.86f, 1f));
            var illustrationImage = illustration.GetComponent<Image>();

            var placeholder = CreateText(illustration.transform, "Placeholder", "사건 이미지 (Placeholder)", 32,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(800f, 80f));

            var title = CreateText(root.transform, "Title", "사건 제목", 48, TextAnchor.MiddleCenter,
                new Vector2(0f, -320f), new Vector2(900f, 70f));
            var description = CreateText(root.transform, "Description", "사건 설명", 30, TextAnchor.UpperCenter,
                new Vector2(0f, -430f), new Vector2(900f, 160f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            var view = root.AddComponent<EventPanelView>();
            view.Bind(title, description, illustrationImage, placeholder);
            return view;
        }

        private static ChoicePanelView BuildChoicePanel(Transform parent)
        {
            var root = CreatePanel(parent, "ChoicePanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 220f), new Vector2(980f, 360f), new Color(1f, 1f, 1f, 0.01f));
            root.GetComponent<Image>().raycastTarget = false;

            var buttons = new Button[3];
            var labels = new Text[3];
            var offsets = new[] { 110f, 0f, -110f };
            for (var i = 0; i < 3; i++)
            {
                var buttonObject = CreatePanel(root.transform, $"Choice_{i}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, offsets[i]), new Vector2(900f, 96f),
                    new Color(0.18f, 0.42f, 0.55f, 1f));
                var button = buttonObject.AddComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
                var label = CreateText(buttonObject.transform, "Label", $"선택지 {i + 1}", 34,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(860f, 80f));
                label.color = Color.white;
                buttons[i] = button;
                labels[i] = label;
            }

            var view = root.AddComponent<ChoicePanelView>();
            view.Bind(buttons, labels);
            return view;
        }

        private static ResultPopupView BuildResultPopup(Transform parent)
        {
            var root = CreatePanel(parent, "ResultPopup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860f, 720f), new Color(0.12f, 0.14f, 0.18f, 0.72f));
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.55f);

            var card = CreatePanel(root.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 560f), new Color(0.98f, 0.98f, 0.96f, 1f));

            var title = CreateText(card.transform, "Title", "선택 결과", 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 200f), new Vector2(760f, 60f));
            var message = CreateText(card.transform, "Message", "메시지", 32, TextAnchor.UpperCenter,
                new Vector2(0f, 80f), new Vector2(740f, 140f));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            var changes = CreateText(card.transform, "Changes", "변화", 30, TextAnchor.UpperCenter,
                new Vector2(0f, -60f), new Vector2(740f, 160f));
            changes.horizontalOverflow = HorizontalWrapMode.Wrap;

            var nextButtonObject = CreatePanel(card.transform, "NextDayButton", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(420f, 100f),
                new Color(0.18f, 0.42f, 0.55f, 1f));
            var nextButton = nextButtonObject.AddComponent<Button>();
            nextButton.targetGraphic = nextButtonObject.GetComponent<Image>();
            var nextLabel = CreateText(nextButtonObject.transform, "Label", "다음 날", 36,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(400f, 80f));
            nextLabel.color = Color.white;

            var view = root.AddComponent<ResultPopupView>();
            view.Bind(root, title, message, changes, nextButton, nextLabel);
            root.SetActive(false);
            return view;
        }

        private static void AssignSampleData(GamePlayPresenter presenter)
        {
            var job = AssetDatabase.LoadAssetAtPath<JobData>("Assets/Data/Jobs/Job_JuniorOffice.asset");
            var trait = AssetDatabase.LoadAssetAtPath<TraitData>("Assets/Data/Traits/Trait_Thrifty.asset");
            var overtime = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Data/Events/Event_Overtime_001.asset");
            var phone = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Data/Events/Event_PhoneCrack_001.asset");
            var rest = EnsureRestFallbackEvent();

            if (job == null || overtime == null || rest == null)
            {
                Debug.LogWarning(
                    "[GameSceneUiSetup] Sample data missing. Run 'Create Sample Data (Unit 2)' then re-run this setup.");
                return;
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("startingJob").objectReferenceValue = job;
            so.FindProperty("startingTrait").objectReferenceValue = trait;
            so.FindProperty("fallbackEvent").objectReferenceValue = rest;
            so.FindProperty("randomSeed").intValue = 1;

            var catalogProp = so.FindProperty("eventCatalog");
            catalogProp.ClearArray();
            var events = new List<EventData>();
            if (overtime != null)
            {
                events.Add(overtime);
            }

            if (phone != null)
            {
                events.Add(phone);
            }

            events.Add(rest);
            for (var i = 0; i < events.Count; i++)
            {
                catalogProp.InsertArrayElementAtIndex(i);
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = events[i];
            }

            var traitGuids = AssetDatabase.FindAssets("t:TraitData", new[] { "Assets/Data/Traits" });
            var allTraitsProp = so.FindProperty("allTraits");
            allTraitsProp.ClearArray();
            for (var i = 0; i < traitGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(traitGuids[i]);
                var traitAsset = AssetDatabase.LoadAssetAtPath<TraitData>(path);
                allTraitsProp.InsertArrayElementAtIndex(i);
                allTraitsProp.GetArrayElementAtIndex(i).objectReferenceValue = traitAsset;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static EventData EnsureRestFallbackEvent()
        {
            const string path = "Assets/Data/Events/Event_Rest_Fallback.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EventData>(path);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Data/Events"))
            {
                return null;
            }

            var eventData = ScriptableObject.CreateInstance<EventData>();
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "rest_home",
                    "집에서 쉰다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, -5),
                        new StatEffect(StatType.Happiness, 3)
                    }),
                new EventChoiceData(
                    "rest_walk",
                    "산책한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, 3),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "rest_hobby",
                    "취미를 즐긴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, 6),
                        new StatEffect(StatType.Cash, -8_000L)
                    })
            };

            eventData.EditorSetCore(
                "event_rest_fallback",
                "여유로운 하루",
                "특별히 급한 일은 없다. 어떻게 보내볼까?",
                EventCategory.Rest,
                1,
                30,
                50,
                new EventCondition(),
                choices);

            AssetDatabase.CreateAsset(eventData, path);
            return eventData;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = ResolveUiFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.15f, 0.15f, 0.18f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font ResolveUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
