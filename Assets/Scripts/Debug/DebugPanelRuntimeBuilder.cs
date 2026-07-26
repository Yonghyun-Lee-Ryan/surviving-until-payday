#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurviveUntilPayday.DebugTools
{
    /// <summary>
    /// Play Mode에서 누락된 DebugPanel UI를 런타임 생성한다.
    /// </summary>
    public static class DebugPanelRuntimeBuilder
    {
        public static bool NeedsRebuild(DebugPanel panel)
        {
            return panel == null
                   || !panel.HasRequiredBindings();
        }

        public static void Rebuild(DebugPanel panel, GamePlayPresenter presenter)
        {
            if (panel == null)
            {
                return;
            }

            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("[DebugPanelRuntimeBuilder] Canvas missing.");
                return;
            }

            var existingRoot = panel.gameObject;
            var stale = new List<GameObject>();
            foreach (Transform child in existingRoot.transform)
            {
                stale.Add(child.gameObject);
            }

            for (var i = 0; i < stale.Count; i++)
            {
                Object.DestroyImmediate(stale[i]);
            }

            var image = existingRoot.GetComponent<Image>();
            if (image == null)
            {
                image = existingRoot.AddComponent<Image>();
            }

            image.color = new Color(0f, 0f, 0f, 0.72f);
            var rootRt = existingRoot.GetComponent<RectTransform>();
            if (rootRt == null)
            {
                rootRt = existingRoot.AddComponent<RectTransform>();
            }

            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var frame = Panel(existingRoot.transform, "Frame", new Vector2(960f, 1680f),
                new Color(0.12f, 0.14f, 0.16f, 0.98f));
            Label(frame.transform, "Title", "DEBUG (F1 / Hint)", 34, new Vector2(0f, 780f),
                new Vector2(900f, 48f), Color.white);

            var scrollRoot = Panel(frame.transform, "Scroll", new Vector2(920f, 1180f),
                new Color(0.1f, 0.11f, 0.13f, 1f));
            scrollRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = StretchPanel(scrollRoot.transform, "Viewport", new Color(0.1f, 0.11f, 0.13f, 1f));
            viewport.AddComponent<RectMask2D>();
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(900f, 2400f);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            var content = contentGo.transform;
            var y = -16f;
            Section(content, "날짜", ref y);
            var day = Input(content, "Day", "1", ref y);
            var d1 = Triple(content, ref y, "D1", "D7", "D14");
            var d2 = Triple(content, ref y, "D15", "D21", "D30");

            Section(content, "현금 / 능력치", ref y);
            var cash = Input(content, "Cash", "2800000", ref y);
            var c1 = Triple(content, ref y, "+100k", "-100k", "+500k");
            var c2 = Triple(content, ref y, "-500k", "0원", "부자500만");
            var health = Input(content, "Health", "80", ref y);
            var stress = Input(content, "Stress", "20", ref y);
            var happiness = Input(content, "Happiness", "50", ref y);
            var company = Input(content, "Company", "50", ref y);
            var presets = Triple(content, ref y, "위기", "안정", "해고위기");
            var seed = Input(content, "Seed", "1", ref y);

            Section(content, "사건 / 엔딩 / 실패", ref y);
            var eventFilter = Input(content, "EventFilter", string.Empty, ref y, "filter");
            var eventDd = MakeDropdown(content, "EventDropdown", ref y);
            var endingFilter = Input(content, "EndingFilter", string.Empty, ref y, "filter");
            var endingDd = MakeDropdown(content, "EndingDropdown", ref y);
            var failureDd = MakeDropdown(content, "FailureDropdown", ref y);

            Section(content, "런 플래그", ref y);
            var flagIds = new List<string>
            {
                RunFlags.HasBoughtStock,
                RunFlags.StockBigWin,
                RunFlags.PhoneStillCracked,
                RunFlags.OwesDebt,
                RunFlags.OrderedDelivery
            };
            var toggles = new List<Toggle>();
            for (var i = 0; i < flagIds.Count; i++)
            {
                toggles.Add(Flag(content, flagIds[i], ref y));
            }

            var clearFlags = WideButton(content, "ClearFlags", "플래그 전체 클리어", ref y);
            var flagsSummary = BodyText(content, "FlagsSummary", "Flags: (none)", ref y);
            flagsSummary.color = new Color(0.85f, 0.9f, 0.75f);
            var status = BodyText(content, "Status", "Ready", ref y);
            status.alignment = TextAnchor.MiddleCenter;
            contentRt.sizeDelta = new Vector2(900f, Mathf.Max(1200f, -y + 40f));

            var actions = Panel(frame.transform, "Actions", new Vector2(920f, 300f),
                new Color(0.14f, 0.16f, 0.18f, 1f));
            var actionsRt = actions.GetComponent<RectTransform>();
            actionsRt.anchorMin = actionsRt.anchorMax = new Vector2(0.5f, 0f);
            actionsRt.pivot = new Vector2(0.5f, 0f);
            actionsRt.anchoredPosition = new Vector2(0f, 16f);

            var events = panel.GetEventCatalogCopy();
            var endings = panel.GetEndingCatalogCopy();
            if (events.Count == 0)
            {
                events.AddRange(Resources.FindObjectsOfTypeAll<EventData>());
                events.RemoveAll(e => e == null || string.IsNullOrEmpty(e.Id));
            }

            if (endings.Count == 0)
            {
                endings.AddRange(Resources.FindObjectsOfTypeAll<EndingData>());
                endings.RemoveAll(e => e == null || string.IsNullOrEmpty(e.Id));
            }

            panel.Bind(
                presenter,
                existingRoot,
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
                toggles,
                flagIds,
                events,
                endings);

            Wire(d1[0], panel.JumpDay1);
            Wire(d1[1], panel.JumpDay7);
            Wire(d1[2], panel.JumpDay14);
            Wire(d2[0], panel.JumpDay15);
            Wire(d2[1], panel.JumpDay21);
            Wire(d2[2], panel.JumpDay30);
            Wire(c1[0], panel.CashPlus100k);
            Wire(c1[1], panel.CashMinus100k);
            Wire(c1[2], panel.CashPlus500k);
            Wire(c2[0], panel.CashMinus500k);
            Wire(c2[1], panel.SetCashZero);
            Wire(c2[2], panel.SetCashRich);
            Wire(presets[0], panel.ApplyPresetCrisis);
            Wire(presets[1], panel.ApplyPresetStable);
            Wire(presets[2], panel.ApplyPresetFiredRisk);
            Wire(clearFlags, panel.ClearAllFlags);

            Action(actions.transform, "ApplyDay", "날짜 적용", new Vector2(-230f, 230f), panel.ApplyDay);
            Action(actions.transform, "ApplyStats", "능력치 적용", new Vector2(230f, 230f), panel.ApplyStats);
            Action(actions.transform, "ApplySeed", "시드 적용", new Vector2(-230f, 160f), panel.ApplySeed);
            Action(actions.transform, "ForceEvent", "사건 강제", new Vector2(230f, 160f), panel.ForceSelectedEvent);
            Action(actions.transform, "ForceEnding", "엔딩 강제", new Vector2(-230f, 90f), panel.ForceSelectedEnding);
            Action(actions.transform, "ForceWin", "즉시 성공", new Vector2(230f, 90f), panel.ForceSuccess);
            Action(actions.transform, "ForceFail", "선택 실패", new Vector2(-230f, 20f), panel.ForceSelectedFailure);
            Action(actions.transform, "LogState", "상태 로그", new Vector2(230f, 20f), panel.LogStateDump);
            Action(actions.transform, "Close", "닫기", new Vector2(0f, 20f), panel.Toggle, 200f);

            EnsureHint(canvas.transform, panel);
            existingRoot.SetActive(false);
        }

        private static void EnsureHint(Transform canvas, DebugPanel panel)
        {
            var existing = canvas.Find("DebugHint");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject("DebugHint", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 24f);
            rt.sizeDelta = new Vector2(200f, 48f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.18f, 0.22f, 0.7f);
            Label(go.transform, "Label", "F1 Debug", 22, Vector2.zero, new Vector2(190f, 40f), Color.white);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            Wire(button, panel.Toggle);
        }

        private static void Wire(Button button, UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.AddListener(action);
        }

        private static void Action(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            UnityAction action,
            float width = 400f)
        {
            var go = Panel(parent, name, new Vector2(width, 52f), new Color(0.28f, 0.45f, 0.55f, 1f));
            go.GetComponent<RectTransform>().anchoredPosition = pos;
            Label(go.transform, "Label", label, 24, Vector2.zero, new Vector2(width - 20f, 44f), Color.white);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Wire(button, action);
        }

        private static void Section(Transform parent, string title, ref float y)
        {
            var t = BodyText(parent, "Section_" + title, "— " + title + " —", ref y, advance: false);
            t.color = new Color(0.7f, 0.85f, 1f);
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
            y -= 44f;
        }

        private static Text BodyText(Transform parent, string name, string value, ref float y, bool advance = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(860f, 40f);
            var text = go.AddComponent<Text>();
            text.font = Font();
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = value;
            if (advance)
            {
                y -= 48f;
            }

            return text;
        }

        private static InputField Input(
            Transform parent,
            string label,
            string value,
            ref float y,
            string placeholder = null)
        {
            var labelText = BodyText(parent, label + "Label", label, ref y, advance: false);
            labelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280f, y);
            labelText.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);

            var field = new GameObject(label + "Input", typeof(RectTransform));
            field.transform.SetParent(parent, false);
            var rt = field.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(120f, y);
            rt.sizeDelta = new Vector2(520f, 44f);
            field.AddComponent<Image>().color = new Color(0.22f, 0.24f, 0.28f, 1f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(field.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            var text = textGo.AddComponent<Text>();
            text.font = Font();
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(field.transform, false);
            Stretch(phGo.GetComponent<RectTransform>());
            var ph = phGo.AddComponent<Text>();
            ph.font = Font();
            ph.fontSize = 24;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = placeholder ?? label;

            var input = field.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value;
            y -= 56f;
            return input;
        }

        private static Button[] Triple(Transform parent, ref float y, string a, string b, string c)
        {
            var buttons = new[]
            {
                Mini(parent, a, new Vector2(-280f, y)),
                Mini(parent, b, new Vector2(0f, y)),
                Mini(parent, c, new Vector2(280f, y))
            };
            y -= 60f;
            return buttons;
        }

        private static Button Mini(Transform parent, string label, Vector2 pos)
        {
            var go = TopPanel(parent, "Btn_" + label, pos, new Vector2(250f, 48f), new Color(0.3f, 0.42f, 0.5f, 1f));
            Label(go.transform, "Label", label, 22, Vector2.zero, new Vector2(230f, 40f), Color.white);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static Button WideButton(Transform parent, string name, string label, ref float y)
        {
            var go = TopPanel(parent, name, new Vector2(0f, y), new Vector2(360f, 48f),
                new Color(0.45f, 0.32f, 0.28f, 1f));
            Label(go.transform, "Label", label, 22, Vector2.zero, new Vector2(340f, 40f), Color.white);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            y -= 60f;
            return button;
        }

        private static Toggle Flag(Transform parent, string flagId, ref float y)
        {
            var root = TopPanel(parent, "Flag_" + flagId, new Vector2(0f, y), new Vector2(860f, 44f),
                new Color(0.18f, 0.2f, 0.24f, 1f));
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            var check = TopPanel(root.transform, "Check", new Vector2(0f, 0f), new Vector2(28f, 28f),
                new Color(0.35f, 0.75f, 0.45f, 1f));
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = new Vector2(28f, 0f);
            toggle.graphic = check.GetComponent<Image>();
            check.SetActive(false);
            var label = Label(root.transform, "Label", flagId, 22, new Vector2(40f, 0f), new Vector2(760f, 36f),
                Color.white);
            label.alignment = TextAnchor.MiddleLeft;
            y -= 52f;
            return toggle;
        }

        private static Dropdown MakeDropdown(Transform parent, string name, ref float y)
        {
            var root = TopPanel(parent, name, new Vector2(0f, y), new Vector2(860f, 48f),
                new Color(0.22f, 0.24f, 0.28f, 1f));
            var caption = Label(root.transform, "Label", "select", 22, Vector2.zero, new Vector2(820f, 40f),
                Color.white);
            caption.alignment = TextAnchor.MiddleLeft;

            var template = TopPanel(root.transform, "Template", new Vector2(0f, -8f), new Vector2(860f, 180f),
                new Color(0.18f, 0.2f, 0.22f, 1f));
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = templateRt.anchorMax = new Vector2(0.5f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            template.SetActive(false);

            var viewport = Panel(template.transform, "Viewport", new Vector2(860f, 180f),
                new Color(0.18f, 0.2f, 0.22f, 1f));
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 40f);

            var item = Panel(content.transform, "Item", new Vector2(860f, 40f), new Color(0.25f, 0.28f, 0.32f, 1f));
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = itemRt.anchorMax = new Vector2(0.5f, 1f);
            itemRt.pivot = new Vector2(0.5f, 1f);
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var itemLabel = Label(item.transform, "Item Label", "Option", 20, Vector2.zero, new Vector2(820f, 36f),
                Color.white);
            itemLabel.alignment = TextAnchor.MiddleLeft;

            var scroll = template.AddComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.captionText = caption;
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = itemLabel;
            y -= 60f;
            return dropdown;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject TopPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject StretchPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static Text Label(
            Transform parent,
            string name,
            string value,
            int size,
            Vector2 pos,
            Vector2 rectSize,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;
            var text = go.AddComponent<Text>();
            text.font = Font();
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
        }

        private static Font Font()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
#endif
