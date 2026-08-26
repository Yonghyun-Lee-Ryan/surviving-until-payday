using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Art;
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
            var weeklyPopup = BuildWeeklySummaryPopup(safeArea);

            var presenterObject = new GameObject("GamePlayPresenter");
            Undo.RegisterCreatedObjectUndo(presenterObject, "Create GamePlayPresenter");
            var presenter = presenterObject.AddComponent<GamePlayPresenter>();
            presenter.BindViews(hud, eventPanel, choicePanel, resultPopup, weeklyPopup);

            AssignSampleData(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = presenterObject;

            Debug.Log(
                "[GameSceneUiSetup] Game Scene UI ready.\n" +
                "1) GamePlayPresenter에 Job/Events가 비어 있으면 Inspector에서 할당하세요.\n" +
                "2) Bootstrap → MainMenu → 새 게임으로 플레이하세요.\n" +
                "3) Game 뷰를 1080x1920 Portrait로 맞추세요.");
        }

        [MenuItem("Tools/Surviving Until Payday/Fix Game Scene UI Layout & Gauges")]
        public static void FixExistingLayoutAndGauges()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError("[GameSceneUiSetup] Game.unity not found.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[GameSceneUiSetup] Canvas not found.");
                return;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                CanvasSetupUtility.ApplyPortraitCanvasScaler(scaler);
            }

            var safeArea = EnsureSafeArea(canvas.transform);
            var hud = safeArea.Find("HUD")?.GetComponent<RectTransform>();
            if (hud != null)
            {
                ApplyTopBar(hud, -12f, -48f, 300f);
                RelayoutHudChildren(hud);
            }

            var eventPanel = safeArea.Find("EventPanel")?.GetComponent<RectTransform>();
            if (eventPanel != null)
            {
                eventPanel.anchorMin = new Vector2(0f, 0.5f);
                eventPanel.anchorMax = new Vector2(1f, 0.5f);
                eventPanel.pivot = new Vector2(0.5f, 0.5f);
                eventPanel.anchoredPosition = new Vector2(0f, 40f);
                eventPanel.sizeDelta = new Vector2(-48f, 560f);
            }

            var choicePanel = safeArea.Find("ChoicePanel")?.GetComponent<RectTransform>();
            if (choicePanel != null)
            {
                ApplyBottomBar(choicePanel, 36f, -48f, 340f);
                RelayoutChoiceButtons(choicePanel);
            }

            foreach (var gauge in Object.FindObjectsByType<StatGaugeView>(FindObjectsInactive.Include))
            {
                EditorUtility.SetDirty(gauge);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[GameSceneUiSetup] Layout/gauge fix saved.\n" +
                "Play Mode에서 선택 후 게이지 가로 비율이 줄어드는지 확인하세요.");
        }

        private static void ApplyTopBar(RectTransform rect, float topInset, float horizontalSizeDelta, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topInset);
            rect.sizeDelta = new Vector2(horizontalSizeDelta, height);
        }

        private static void ApplyBottomBar(RectTransform rect, float bottomInset, float horizontalSizeDelta, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottomInset);
            rect.sizeDelta = new Vector2(horizontalSizeDelta, height);
        }

        private static void RelayoutHudChildren(RectTransform hud)
        {
            var day = hud.Find("DayLabel") as RectTransform;
            if (day != null)
            {
                day.anchorMin = new Vector2(0f, 1f);
                day.anchorMax = new Vector2(0.5f, 1f);
                day.pivot = new Vector2(0f, 1f);
                day.anchoredPosition = new Vector2(24f, -16f);
                day.sizeDelta = new Vector2(-12f, 48f);
            }

            var cash = hud.Find("CashLabel") as RectTransform;
            if (cash != null)
            {
                cash.anchorMin = new Vector2(0.5f, 1f);
                cash.anchorMax = new Vector2(1f, 1f);
                cash.pivot = new Vector2(1f, 1f);
                cash.anchoredPosition = new Vector2(-24f, -16f);
                cash.sizeDelta = new Vector2(-12f, 48f);
            }

            var crisis = hud.Find("CrisisBanner") as RectTransform;
            if (crisis != null)
            {
                crisis.anchorMin = new Vector2(0f, 1f);
                crisis.anchorMax = new Vector2(1f, 1f);
                crisis.pivot = new Vector2(0.5f, 1f);
                crisis.anchoredPosition = new Vector2(0f, -72f);
                crisis.sizeDelta = new Vector2(-40f, 40f);
            }

            PlaceGaugeInHud(hud.Find("HealthGauge") as RectTransform, 0);
            PlaceGaugeInHud(hud.Find("StressGauge") as RectTransform, 1);
            PlaceGaugeInHud(hud.Find("HappinessGauge") as RectTransform, 2);
            PlaceGaugeInHud(hud.Find("CompanyGauge") as RectTransform, 3);
        }

        private static void RelayoutChoiceButtons(RectTransform choicePanel)
        {
            if (choicePanel == null)
            {
                return;
            }

            choicePanel.anchorMin = new Vector2(0f, 0f);
            choicePanel.anchorMax = new Vector2(1f, 0f);
            choicePanel.pivot = new Vector2(0.5f, 0f);
            choicePanel.anchoredPosition = new Vector2(0f, 24f);
            choicePanel.sizeDelta = new Vector2(-48f, 430f);

            var offsets = new[] { 250f, 148f, 46f };
            for (var i = 0; i < 3; i++)
            {
                var button = choicePanel.Find($"Choice_{i}") as RectTransform;
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

            var choiceView = choicePanel.GetComponent<ChoicePanelView>();
            choiceView?.EnsureRerollButton();
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
            // 상단 피벗 + 좌우 stretch로 SafeArea 밖으로 나가지 않게 한다.
            var root = CreatePanel(parent, "HUD", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -12f), new Vector2(-48f, 300f), new Color(0.96f, 0.96f, 0.94f, 0.95f));
            root.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

            var day = CreateText(root.transform, "DayLabel", "1일 (월)", 36, TextAnchor.MiddleLeft,
                new Vector2(-220f, -40f), new Vector2(440f, 48f));
            var dayRect = day.rectTransform;
            dayRect.anchorMin = new Vector2(0f, 1f);
            dayRect.anchorMax = new Vector2(0.5f, 1f);
            dayRect.pivot = new Vector2(0f, 1f);
            dayRect.anchoredPosition = new Vector2(24f, -16f);
            dayRect.sizeDelta = new Vector2(-12f, 48f);

            var cash = CreateText(root.transform, "CashLabel", "0원", 36, TextAnchor.MiddleRight,
                new Vector2(220f, -40f), new Vector2(440f, 48f));
            var cashRect = cash.rectTransform;
            cashRect.anchorMin = new Vector2(0.5f, 1f);
            cashRect.anchorMax = new Vector2(1f, 1f);
            cashRect.pivot = new Vector2(1f, 1f);
            cashRect.anchoredPosition = new Vector2(-24f, -16f);
            cashRect.sizeDelta = new Vector2(-12f, 48f);

            var crisis = CreatePanel(root.transform, "CrisisBanner", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -72f), new Vector2(-40f, 40f), new Color(0.85f, 0.3f, 0.25f, 0.9f));
            crisis.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            crisis.SetActive(false);
            var crisisText = CreateText(crisis.transform, "CrisisLabel", "", 26, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(0f, 36f));
            Stretch(crisisText.rectTransform);
            crisisText.color = Color.white;

            var health = CreateGauge(root.transform, "HealthGauge", "건강");
            var stress = CreateGauge(root.transform, "StressGauge", "스트레스");
            var happiness = CreateGauge(root.transform, "HappinessGauge", "행복도");
            var company = CreateGauge(root.transform, "CompanyGauge", "회사 평가");
            PlaceGaugeInHud(health.GetComponent<RectTransform>(), 0);
            PlaceGaugeInHud(stress.GetComponent<RectTransform>(), 1);
            PlaceGaugeInHud(happiness.GetComponent<RectTransform>(), 2);
            PlaceGaugeInHud(company.GetComponent<RectTransform>(), 3);

            var hud = root.AddComponent<GameHudView>();
            hud.BindLabels(day, cash, crisis, crisisText);
            hud.BindGauges(
                health.GetComponent<StatGaugeView>(),
                stress.GetComponent<StatGaugeView>(),
                happiness.GetComponent<StatGaugeView>(),
                company.GetComponent<StatGaugeView>());
            return hud;
        }

        private static void PlaceGaugeInHud(RectTransform gauge, int index)
        {
            if (gauge == null)
            {
                return;
            }

            const int count = 4;
            const float pad = 0.02f;
            var slot = (1f - pad * 2f) / count;
            var minX = pad + slot * index;
            var maxX = pad + slot * (index + 1);
            gauge.anchorMin = new Vector2(minX, 0f);
            gauge.anchorMax = new Vector2(maxX, 0f);
            gauge.pivot = new Vector2(0.5f, 0f);
            gauge.anchoredPosition = new Vector2(0f, 12f);
            gauge.sizeDelta = new Vector2(-8f, 140f);
        }

        private static GameObject CreateGauge(Transform parent, string name, string displayName)
        {
            var root = CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(210f, 130f), new Color(0.9f, 0.9f, 0.9f, 0.95f));

            var nameLabel = CreateText(root.transform, "Name", displayName, 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 42f), new Vector2(0f, 32f));
            StretchHorizontal(nameLabel.rectTransform, 42f, 32f);
            nameLabel.font = Resources.Load<Font>("Fonts/NotoSansKR-Bold")
                             ?? Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                             ?? nameLabel.font;
            nameLabel.color = new Color(0.15f, 0.16f, 0.2f, 1f);

            var valueLabel = CreateText(root.transform, "Value", "0", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, -42f), new Vector2(0f, 28f));
            StretchHorizontal(valueLabel.rectTransform, -42f, 28f);
            valueLabel.font = Resources.Load<Font>("Fonts/NotoSansKR-Regular") ?? valueLabel.font;
            valueLabel.color = new Color(0.15f, 0.16f, 0.2f, 1f);

            var track = CreatePanel(root.transform, "Track", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(-24f, 18f), new Color(0.82f, 0.82f, 0.82f, 1f));
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.sizeDelta = new Vector2(-24f, 18f);

            var fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(track.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            var fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.25f, 0.55f, 0.45f);
            fill.type = Image.Type.Simple;
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

        private static void StretchHorizontal(RectTransform rect, float anchoredY, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, anchoredY);
            rect.sizeDelta = new Vector2(-12f, height);
        }

        private static EventPanelView BuildEventPanel(Transform parent)
        {
            var root = CreatePanel(parent, "EventPanel", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 70f), new Vector2(-32f, 620f), new Color(0.93f, 0.94f, 0.96f, 1f));

            // 상황 이미지: 넓고 크게
            var background = CreatePanel(root.transform, "Background", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(-16f, 360f), new Color(0.78f, 0.82f, 0.86f, 1f));
            background.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = false;

            // 표정 슬롯은 생성하되 비활성(초상화 미사용)
            var expression = CreatePanel(root.transform, "Expression", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -200f), new Vector2(150f, 150f), new Color(1f, 1f, 1f, 0f));
            expression.SetActive(false);
            var expressionImage = expression.GetComponent<Image>();
            expressionImage.enabled = false;

            var title = CreateText(root.transform, "Title", "사건 제목", 38, TextAnchor.MiddleCenter,
                new Vector2(0f, -385f), new Vector2(-32f, 48f));
            StretchHorizontal(title.rectTransform, -385f, 48f);
            var description = CreateText(root.transform, "Description", "사건 설명", 32, TextAnchor.UpperCenter,
                new Vector2(0f, -450f), new Vector2(-32f, 140f));
            StretchHorizontal(description.rectTransform, -450f, 140f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            var view = root.AddComponent<EventPanelView>();
            view.Bind(title, description, backgroundImage, null, expressionImage, null);
            return view;
        }

        private static ChoicePanelView BuildChoicePanel(Transform parent)
        {
            var root = CreatePanel(parent, "ChoicePanel", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 24f), new Vector2(-48f, 500f), new Color(1f, 1f, 1f, 0.01f));
            root.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            root.GetComponent<Image>().raycastTarget = false;

            var buttons = new Button[3];
            var labels = new Text[3];
            // 하단부터: 선택지 3개(미리보기 두 줄), 위쪽에 광고 버튼 공간 확보
            var offsets = new[] { 292f, 176f, 60f };
            for (var i = 0; i < 3; i++)
            {
                var buttonObject = CreatePanel(root.transform, $"Choice_{i}", new Vector2(0f, 0f),
                    new Vector2(1f, 0f), new Vector2(0f, offsets[i]), new Vector2(-40f, 108f),
                    new Color(0.18f, 0.42f, 0.55f, 1f));
                buttonObject.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
                var button = buttonObject.AddComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
                var label = CreateText(buttonObject.transform, "Label", $"선택지 {i + 1}", 28,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0f, 80f));
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(16f, 10f);
                label.rectTransform.offsetMax = new Vector2(-16f, -10f);
                label.lineSpacing = 1.3f;
                label.color = Color.white;
                buttons[i] = button;
                labels[i] = label;
            }

            var view = root.AddComponent<ChoicePanelView>();
            view.Bind(buttons, labels);
            view.EnsureRerollButton();
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

            var nextButtonObject = CreatePanel(card.transform, "NextDayButton", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(420f, 100f),
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

        private static WeeklySummaryPopupView BuildWeeklySummaryPopup(Transform parent)
        {
            var root = CreatePanel(parent, "WeeklySummaryPopup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860f, 720f), new Color(0.12f, 0.14f, 0.18f, 0.72f));
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.55f);

            var card = CreatePanel(root.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 620f), new Color(0.97f, 0.96f, 0.92f, 1f));

            var title = CreateText(card.transform, "Title", "주간 결산", 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 230f), new Vector2(760f, 60f));
            var body = CreateText(card.transform, "Body", "요약", 30, TextAnchor.UpperCenter,
                new Vector2(0f, 80f), new Vector2(740f, 220f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            var warnings = CreateText(card.transform, "Warnings", "경고", 28, TextAnchor.UpperCenter,
                new Vector2(0f, -150f), new Vector2(740f, 120f));
            warnings.horizontalOverflow = HorizontalWrapMode.Wrap;
            warnings.verticalOverflow = VerticalWrapMode.Truncate;
            warnings.color = new Color(0.55f, 0.2f, 0.15f, 1f);

            var continueObject = CreatePanel(card.transform, "ContinueButton", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(420f, 100f),
                new Color(0.25f, 0.45f, 0.35f, 1f));
            var continueButton = continueObject.AddComponent<Button>();
            continueButton.targetGraphic = continueObject.GetComponent<Image>();
            var continueLabel = CreateText(continueObject.transform, "Label", "다음 주로", 36,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(400f, 80f));
            continueLabel.color = Color.white;

            var view = root.AddComponent<WeeklySummaryPopupView>();
            view.Bind(root, title, body, warnings, continueButton, continueLabel);
            root.SetActive(false);
            return view;
        }

        [MenuItem("Tools/Surviving Until Payday/Setup Weekly Summary Popup (Unit 19)")]
        public static void SetupWeeklySummaryPopupOnly()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError("[GameSceneUiSetup] Game.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<GamePlayPresenter>();
            var safeArea = GameObject.Find("SafeArea");
            if (presenter == null || safeArea == null)
            {
                Debug.LogError("[GameSceneUiSetup] GamePlayPresenter/SafeArea missing. Run Unit 7 setup first.");
                return;
            }

            var existing = safeArea.transform.Find("WeeklySummaryPopup");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var weekly = BuildWeeklySummaryPopup(safeArea.transform);
            var so = new SerializedObject(presenter);
            so.FindProperty("weeklySummaryPopupView").objectReferenceValue = weekly;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameSceneUiSetup] WeeklySummaryPopup wired (Unit 19).");
        }

        private static void AssignSampleData(GamePlayPresenter presenter)
        {
            var job = AssetDatabase.LoadAssetAtPath<JobData>("Assets/Data/Jobs/Job_JuniorOffice.asset");
            var trait = AssetDatabase.LoadAssetAtPath<TraitData>("Assets/Data/Traits/Trait_Thrifty.asset");
            var rest = EnsureRestFallbackEvent();

            if (job == null || rest == null)
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

            var artCatalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/Data/Art/ArtCatalog.asset");
            if (artCatalog != null && so.FindProperty("artCatalog") != null)
            {
                so.FindProperty("artCatalog").objectReferenceValue = artCatalog;
            }

            // 개발 단위 16: Assets/Data/Events 아래 모든 EventData를 카탈로그에 등록한다.
            var events = new List<EventData>();
            var eventGuids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/Data/Events" });
            foreach (var guid in eventGuids)
            {
                var eventPath = AssetDatabase.GUIDToAssetPath(guid);
                var eventAsset = AssetDatabase.LoadAssetAtPath<EventData>(eventPath);
                if (eventAsset != null)
                {
                    events.Add(eventAsset);
                }
            }

            events.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var catalogProp = so.FindProperty("eventCatalog");
            catalogProp.ClearArray();
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

            WireEndingCatalog(presenter, so);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void WireEndingCatalog(GamePlayPresenter presenter, SerializedObject so)
        {
            var guids = AssetDatabase.FindAssets("t:EndingData", new[] { "Assets/Data/Endings" });
            var endings = new List<EndingData>();
            EndingData fallback = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ending = AssetDatabase.LoadAssetAtPath<EndingData>(path);
                if (ending == null)
                {
                    continue;
                }

                endings.Add(ending);
                if (ending.Id == "ending_barely_survived")
                {
                    fallback = ending;
                }
            }

            if (fallback == null && endings.Count > 0)
            {
                fallback = endings[0];
            }

            var catalogProp = so.FindProperty("endingCatalog");
            catalogProp.ClearArray();
            for (var i = 0; i < endings.Count; i++)
            {
                catalogProp.InsertArrayElementAtIndex(i);
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = endings[i];
            }

            so.FindProperty("fallbackSuccessEnding").objectReferenceValue = fallback;
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
            var font = Resources.Load<Font>("Fonts/NotoSansKR-Regular");
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
