using System;
using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Game Scene DebugPanel UI 생성/갱신.
    /// </summary>
    public static class GameSceneDebugSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MenuPath = "Tools/Surviving Until Payday/Setup Debug Panel (Unit 11)";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError("[GameSceneDebugSetup] Game.unity not found.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = UnityEngine.Object.FindAnyObjectByType<GamePlayPresenter>();
            if (presenter == null)
            {
                Debug.LogError(
                    "[GameSceneDebugSetup] GamePlayPresenter missing. Run Setup Game Scene UI first.");
                return;
            }

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[GameSceneDebugSetup] Canvas missing.");
                return;
            }

            RemoveExistingDebugPanels();

            var root = CreateStretchPanel(canvas.transform, "DebugPanel", new Color(0f, 0f, 0f, 0.72f));
            root.transform.SetAsLastSibling();
            root.SetActive(false);

            var frame = CreateFixedPanel(
                root.transform,
                "Frame",
                Vector2.zero,
                new Vector2(960f, 1680f),
                new Color(0.12f, 0.14f, 0.16f, 0.98f));

            var title = CreateCenteredText(frame.transform, "Title", "DEBUG (F1 / Hint)", 34,
                new Vector2(0f, 780f), new Vector2(900f, 48f));
            title.color = Color.white;

            var scrollRoot = CreateFixedPanel(
                frame.transform,
                "Scroll",
                new Vector2(0f, 90f),
                new Vector2(920f, 1180f),
                new Color(0.1f, 0.11f, 0.13f, 1f));
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateStretchPanel(scrollRoot.transform, "Viewport", new Color(0.1f, 0.11f, 0.13f, 1f));
            viewport.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(900f, 2400f);

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;

            var content = contentGo.transform;
            var y = -16f;

            Section(content, "날짜", ref y);
            var day = LabeledInput(content, "Day", "1", ref y);
            var dayButtons1 = TripleButtons(content, ref y, "D1", "D7", "D14");
            var dayButtons2 = TripleButtons(content, ref y, "D15", "D21", "D30");

            Section(content, "현금 / 능력치", ref y);
            var cash = LabeledInput(content, "Cash", "2800000", ref y);
            var cashButtons1 = TripleButtons(content, ref y, "+100k", "-100k", "+500k");
            var cashButtons2 = TripleButtons(content, ref y, "-500k", "0원", "부자500만");
            var health = LabeledInput(content, "Health", "80", ref y);
            var stress = LabeledInput(content, "Stress", "20", ref y);
            var happiness = LabeledInput(content, "Happiness", "50", ref y);
            var company = LabeledInput(content, "Company", "50", ref y);
            var presetButtons = TripleButtons(content, ref y, "위기", "안정", "해고위기");
            var seed = LabeledInput(content, "Seed", "1", ref y);

            Section(content, "사건 / 엔딩 / 실패", ref y);
            var eventFilter = LabeledInput(content, "EventFilter", string.Empty, ref y, "filter id/title");
            var eventDd = Dropdown(content, "EventDropdown", ref y);
            var endingFilter = LabeledInput(content, "EndingFilter", string.Empty, ref y, "filter id/title");
            var endingDd = Dropdown(content, "EndingDropdown", ref y);
            var failureDd = Dropdown(content, "FailureDropdown", ref y);

            Section(content, "런 플래그", ref y);
            var flagIds = new List<string>
            {
                RunFlags.HasBoughtStock,
                RunFlags.StockBigWin,
                RunFlags.PhoneStillCracked,
                RunFlags.OwesDebt,
                RunFlags.OrderedDelivery
            };
            var flagToggles = new List<Toggle>();
            for (var i = 0; i < flagIds.Count; i++)
            {
                flagToggles.Add(FlagToggle(content, flagIds[i], ref y));
            }

            var clearFlagsBtn = SingleButton(content, "ClearFlags", "플래그 전체 클리어", ref y, 360f);
            var flagsSummary = TopText(content, "FlagsSummary", "Flags: (none)", 22, ref y, 44f);
            flagsSummary.color = new Color(0.85f, 0.9f, 0.75f);
            var status = TopText(content, "Status", "Ready", 22, ref y, 48f);
            status.color = new Color(0.85f, 0.9f, 0.7f);
            status.alignment = TextAnchor.MiddleCenter;

            contentRect.sizeDelta = new Vector2(900f, Mathf.Max(1200f, -y + 40f));

            var panel = root.AddComponent<DebugPanel>();
            panel.Bind(
                presenter,
                root,
                day,
                cash,
                health,
                stress,
                happiness,
                company,
                seed,
                eventFilter,
                endingFilter,
                eventDd,
                endingDd,
                failureDd,
                status,
                flagsSummary,
                flagToggles,
                flagIds,
                LoadAll<EventData>("Assets/Data/Events"),
                LoadAll<EndingData>("Assets/Data/Endings"));

            Listen(dayButtons1[0], panel.JumpDay1);
            Listen(dayButtons1[1], panel.JumpDay7);
            Listen(dayButtons1[2], panel.JumpDay14);
            Listen(dayButtons2[0], panel.JumpDay15);
            Listen(dayButtons2[1], panel.JumpDay21);
            Listen(dayButtons2[2], panel.JumpDay30);
            Listen(cashButtons1[0], panel.CashPlus100k);
            Listen(cashButtons1[1], panel.CashMinus100k);
            Listen(cashButtons1[2], panel.CashPlus500k);
            Listen(cashButtons2[0], panel.CashMinus500k);
            Listen(cashButtons2[1], panel.SetCashZero);
            Listen(cashButtons2[2], panel.SetCashRich);
            Listen(presetButtons[0], panel.ApplyPresetCrisis);
            Listen(presetButtons[1], panel.ApplyPresetStable);
            Listen(presetButtons[2], panel.ApplyPresetFiredRisk);
            Listen(clearFlagsBtn, panel.ClearAllFlags);

            var actions = CreateFixedPanel(
                frame.transform,
                "Actions",
                new Vector2(0f, 16f),
                new Vector2(920f, 300f),
                new Color(0.14f, 0.16f, 0.18f, 1f));
            var actionsRt = actions.GetComponent<RectTransform>();
            actionsRt.anchorMin = actionsRt.anchorMax = new Vector2(0.5f, 0f);
            actionsRt.pivot = new Vector2(0.5f, 0f);

            ActionButton(actions.transform, "ApplyDay", "날짜 적용", new Vector2(-230f, 230f), panel.ApplyDay);
            ActionButton(actions.transform, "ApplyStats", "능력치 적용", new Vector2(230f, 230f), panel.ApplyStats);
            ActionButton(actions.transform, "ApplySeed", "시드 적용", new Vector2(-230f, 160f), panel.ApplySeed);
            ActionButton(actions.transform, "ForceEvent", "사건 강제", new Vector2(230f, 160f), panel.ForceSelectedEvent);
            ActionButton(actions.transform, "ForceEnding", "엔딩 강제", new Vector2(-230f, 90f), panel.ForceSelectedEnding);
            ActionButton(actions.transform, "ForceWin", "즉시 성공", new Vector2(230f, 90f), panel.ForceSuccess);
            ActionButton(actions.transform, "ForceFail", "선택 실패", new Vector2(-230f, 20f), panel.ForceSelectedFailure);
            ActionButton(actions.transform, "LogState", "상태 로그", new Vector2(230f, 20f), panel.LogStateDump);
            ActionButton(actions.transform, "ToggleClose", "닫기", new Vector2(0f, 20f), panel.Toggle, 200f);

            CreateHint(canvas.transform, panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("[GameSceneDebugSetup] DebugPanel ready. Play Mode: F1 또는 좌하단 Hint.");
        }

        private static void CreateHint(Transform canvas, DebugPanel panel)
        {
            var go = new GameObject("DebugHint", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(200f, 48f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.18f, 0.22f, 0.7f);
            var text = CreateCenteredText(go.transform, "Label", "F1 Debug", 22, Vector2.zero, new Vector2(190f, 40f));
            text.color = new Color(1f, 1f, 1f, 0.85f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            Listen(button, panel.Toggle);
        }

        private static void RemoveExistingDebugPanels()
        {
            foreach (var panel in UnityEngine.Object.FindObjectsByType<DebugPanel>(FindObjectsInactive.Include))
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hint = root.transform.Find("DebugHint");
                if (hint != null)
                {
                    UnityEngine.Object.DestroyImmediate(hint.gameObject);
                }

                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    var nested = canvas.transform.Find("DebugHint");
                    if (nested != null)
                    {
                        UnityEngine.Object.DestroyImmediate(nested.gameObject);
                    }
                }
            }
        }

        private static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return list;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        private static void Section(Transform parent, string label, ref float y)
        {
            var text = TopText(parent, "Section_" + label, "— " + label + " —", 26, ref y, 36f);
            text.color = new Color(0.7f, 0.85f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static InputField LabeledInput(
            Transform parent,
            string label,
            string value,
            ref float y,
            string placeholder = null)
        {
            var labelText = TopText(parent, label + "Label", label, 22, ref y, 0f);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(-280f, y);
            labelRect.sizeDelta = new Vector2(200f, 40f);

            var fieldObject = new GameObject(label + "Input", typeof(RectTransform));
            fieldObject.transform.SetParent(parent, false);
            var rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(120f, y);
            rect.sizeDelta = new Vector2(520f, 44f);
            fieldObject.AddComponent<Image>().color = new Color(0.22f, 0.24f, 0.28f, 1f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(fieldObject.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), 8f);
            var text = textGo.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(fieldObject.transform, false);
            Stretch(phGo.GetComponent<RectTransform>(), 8f);
            var ph = phGo.AddComponent<Text>();
            ph.font = text.font;
            ph.fontSize = 24;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = placeholder ?? label;

            var input = fieldObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value;
            y -= 56f;
            return input;
        }

        private static Button[] TripleButtons(Transform parent, ref float y, string a, string b, string c)
        {
            var buttons = new[]
            {
                MiniButton(parent, "Btn_" + a, a, new Vector2(-280f, y)),
                MiniButton(parent, "Btn_" + b, b, new Vector2(0f, y)),
                MiniButton(parent, "Btn_" + c, c, new Vector2(280f, y))
            };
            y -= 60f;
            return buttons;
        }

        private static Button MiniButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = CreateTopPanel(parent, name, pos, new Vector2(250f, 48f), new Color(0.3f, 0.42f, 0.5f, 1f));
            var text = CreateCenteredText(go.transform, "Label", label, 22, Vector2.zero, new Vector2(230f, 40f));
            text.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static Button SingleButton(Transform parent, string name, string label, ref float y, float width)
        {
            var go = CreateTopPanel(parent, name, new Vector2(0f, y), new Vector2(width, 48f),
                new Color(0.45f, 0.32f, 0.28f, 1f));
            var text = CreateCenteredText(go.transform, "Label", label, 22, Vector2.zero, new Vector2(width - 20f, 40f));
            text.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            y -= 60f;
            return button;
        }

        private static Toggle FlagToggle(Transform parent, string flagId, ref float y)
        {
            var root = CreateTopPanel(parent, "Flag_" + flagId, new Vector2(0f, y), new Vector2(860f, 44f),
                new Color(0.18f, 0.2f, 0.24f, 1f));
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            var check = CreateTopPanel(root.transform, "Checkmark", new Vector2(-390f, -22f), new Vector2(28f, 28f),
                new Color(0.35f, 0.75f, 0.45f, 1f));
            // check uses top-leftish; simplify:
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = new Vector2(28f, 0f);
            toggle.graphic = check.GetComponent<Image>();
            check.SetActive(false);
            var label = CreateCenteredText(root.transform, "Label", flagId, 22, new Vector2(40f, 0f),
                new Vector2(760f, 36f));
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            y -= 52f;
            return toggle;
        }

        private static Dropdown Dropdown(Transform parent, string name, ref float y)
        {
            var root = CreateTopPanel(parent, name, new Vector2(0f, y), new Vector2(860f, 48f),
                new Color(0.22f, 0.24f, 0.28f, 1f));
            var label = CreateCenteredText(root.transform, "Label", "select", 22, Vector2.zero, new Vector2(820f, 40f));
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;

            var template = CreateTopPanel(root.transform, "Template", new Vector2(0f, -8f), new Vector2(860f, 180f),
                new Color(0.18f, 0.2f, 0.22f, 1f));
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = templateRt.anchorMax = new Vector2(0.5f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            template.SetActive(false);

            var viewport = CreateFixedPanel(template.transform, "Viewport", Vector2.zero, new Vector2(860f, 180f),
                new Color(0.18f, 0.2f, 0.22f, 1f));
            var itemContent = new GameObject("Content", typeof(RectTransform));
            itemContent.transform.SetParent(viewport.transform, false);
            var itemContentRt = itemContent.GetComponent<RectTransform>();
            itemContentRt.anchorMin = new Vector2(0f, 1f);
            itemContentRt.anchorMax = new Vector2(1f, 1f);
            itemContentRt.pivot = new Vector2(0.5f, 1f);
            itemContentRt.sizeDelta = new Vector2(0f, 40f);

            var item = CreateFixedPanel(itemContent.transform, "Item", Vector2.zero, new Vector2(860f, 40f),
                new Color(0.25f, 0.28f, 0.32f, 1f));
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = itemRt.anchorMax = new Vector2(0.5f, 1f);
            itemRt.pivot = new Vector2(0.5f, 1f);
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var itemLabel = CreateCenteredText(item.transform, "Item Label", "Option", 20, Vector2.zero,
                new Vector2(820f, 36f));
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAnchor.MiddleLeft;

            var scroll = template.AddComponent<ScrollRect>();
            scroll.content = itemContentRt;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.captionText = label;
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = itemLabel;
            y -= 60f;
            return dropdown;
        }

        private static void ActionButton(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            UnityAction action,
            float width = 400f)
        {
            var go = CreateFixedPanel(parent, name, pos, new Vector2(width, 52f), new Color(0.28f, 0.45f, 0.55f, 1f));
            var text = CreateCenteredText(go.transform, "Label", label, 24, Vector2.zero, new Vector2(width - 20f, 44f));
            text.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Listen(button, action);
        }

        private static void Listen(Button button, UnityAction action)
        {
            if (button != null && action != null)
            {
                UnityEventTools.AddVoidPersistentListener(button.onClick, action);
            }
        }

        private static GameObject CreateStretchPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateFixedPanel(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateTopPanel(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static Text TopText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            ref float y,
            float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(860f, height > 0f ? height : 40f);
            var text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            if (height > 0f)
            {
                y -= height + 8f;
            }

            return text;
        }

        private static Text CreateCenteredText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 pos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.text = value;
            return text;
        }

        private static void Stretch(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        private static Font UiFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
