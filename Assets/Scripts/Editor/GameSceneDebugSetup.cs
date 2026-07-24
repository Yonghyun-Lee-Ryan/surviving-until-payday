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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 11: Game Scene에 DebugPanel UI를 추가한다.
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
                    "[GameSceneDebugSetup] GamePlayPresenter missing. Run Setup Game Scene UI (Unit 7) first.");
                return;
            }

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[GameSceneDebugSetup] Canvas missing.");
                return;
            }

            RemoveExistingDebugPanels();

            var root = CreatePanel(
                canvas.transform,
                "DebugPanel",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero,
                new Color(0f, 0f, 0f, 0.72f));
            root.transform.SetAsLastSibling();
            root.SetActive(false);

            var content = CreatePanel(
                root.transform,
                "Content",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(920f, 1500f),
                new Color(0.12f, 0.14f, 0.16f, 0.96f));

            var title = CreateText(content.transform, "Title", "DEBUG (F1)", 36, TextAnchor.MiddleCenter,
                new Vector2(0f, 680f), new Vector2(860f, 50f));
            title.color = Color.white;

            var day = CreateLabeledInput(content.transform, "Day", "1", new Vector2(0f, 600f));
            var cash = CreateLabeledInput(content.transform, "Cash", "2800000", new Vector2(0f, 520f));
            var health = CreateLabeledInput(content.transform, "Health", "80", new Vector2(0f, 440f));
            var stress = CreateLabeledInput(content.transform, "Stress", "20", new Vector2(0f, 360f));
            var happiness = CreateLabeledInput(content.transform, "Happiness", "50", new Vector2(0f, 280f));
            var company = CreateLabeledInput(content.transform, "Company", "50", new Vector2(0f, 200f));
            var seed = CreateLabeledInput(content.transform, "Seed", "1", new Vector2(0f, 120f));

            var eventDd = CreateDropdown(content.transform, "EventDropdown", new Vector2(0f, 30f));
            var endingDd = CreateDropdown(content.transform, "EndingDropdown", new Vector2(0f, -50f));

            var status = CreateText(content.transform, "Status", "Ready", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, -120f), new Vector2(860f, 40f));
            status.color = new Color(0.85f, 0.9f, 0.7f);

            var panel = root.AddComponent<DebugPanel>();
            var events = LoadAll<EventData>("Assets/Data/Events");
            var endings = LoadAll<EndingData>("Assets/Data/Endings");
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
                eventDd,
                endingDd,
                status,
                events,
                endings);

            WireButton(content.transform, "ApplyDay", "날짜 적용", new Vector2(-220f, -200f), panel.ApplyDay);
            WireButton(content.transform, "ApplyStats", "능력치 적용", new Vector2(220f, -200f), panel.ApplyStats);
            WireButton(content.transform, "ApplySeed", "시드 적용", new Vector2(-220f, -280f), panel.ApplySeed);
            WireButton(content.transform, "ForceEvent", "사건 강제", new Vector2(220f, -280f), panel.ForceSelectedEvent);
            WireButton(content.transform, "ForceEnding", "엔딩 강제", new Vector2(-220f, -360f), panel.ForceSelectedEnding);
            WireButton(content.transform, "ForceWin", "즉시 성공", new Vector2(220f, -360f), panel.ForceSuccess);
            WireButton(content.transform, "ForceLose", "즉시 파산", new Vector2(0f, -440f), panel.ForceFailBankruptcy);
            WireButton(content.transform, "ToggleClose", "닫기", new Vector2(0f, -520f), panel.Toggle);

            var hint = CreateText(
                canvas.transform,
                "DebugHint",
                "F1 Debug",
                22,
                TextAnchor.LowerLeft,
                new Vector2(120f, 40f),
                new Vector2(200f, 40f));
            hint.color = new Color(1f, 1f, 1f, 0.55f);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(0f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(24f, 24f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;

            Debug.Log(
                "[GameSceneDebugSetup] DebugPanel ready.\n" +
                "Play Mode에서 F1로 열고 날짜/능력치/사건/엔딩을 조작하세요.\n" +
                "자동 시뮬레이터: Tools → Surviving Until Payday → Run Simulator Window");
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

                var canvas = root.GetComponent<Canvas>();
                if (canvas == null)
                {
                    continue;
                }

                var nested = canvas.transform.Find("DebugHint");
                if (nested != null)
                {
                    UnityEngine.Object.DestroyImmediate(nested.gameObject);
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
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        private static InputField CreateLabeledInput(Transform parent, string label, string value, Vector2 pos)
        {
            CreateText(parent, label + "Label", label, 24, TextAnchor.MiddleLeft,
                pos + new Vector2(-280f, 0f), new Vector2(200f, 40f)).color = Color.white;

            var fieldObject = new GameObject(label + "Input", typeof(RectTransform));
            fieldObject.transform.SetParent(parent, false);
            var rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos + new Vector2(120f, 0f);
            rect.sizeDelta = new Vector2(520f, 48f);
            var image = fieldObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.28f, 1f);

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(fieldObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            Stretch(textRect, 8f);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform));
            placeholderObject.transform.SetParent(fieldObject.transform, false);
            var phRect = placeholderObject.GetComponent<RectTransform>();
            Stretch(phRect, 8f);
            var placeholder = placeholderObject.AddComponent<Text>();
            placeholder.font = text.font;
            placeholder.fontSize = 26;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.text = label;

            var input = fieldObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            return input;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, Vector2 pos)
        {
            var root = CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(860f, 48f), new Color(0.22f, 0.24f, 0.28f, 1f));
            var label = CreateText(root.transform, "Label", "select", 24, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(820f, 40f));
            label.color = Color.white;

            var template = CreatePanel(root.transform, "Template", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, -8f), new Vector2(860f, 160f), new Color(0.18f, 0.2f, 0.22f, 1f));
            template.SetActive(false);
            var viewport = CreatePanel(template.transform, "Viewport", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860f, 160f), new Color(0.18f, 0.2f, 0.22f, 1f));
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 40f);

            var item = CreatePanel(content.transform, "Item", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(860f, 40f), new Color(0.25f, 0.28f, 0.32f, 1f));
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var itemLabel = CreateText(item.transform, "Item Label", "Option", 22, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(820f, 36f));
            itemLabel.color = Color.white;

            var scroll = template.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.captionText = label;
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = itemLabel;
            return dropdown;
        }

        private static void WireButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(400f, 56f), new Color(0.28f, 0.45f, 0.55f, 1f));
            CreateText(buttonObject.transform, "Label", label, 26, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(380f, 48f)).color = Color.white;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one && size == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = size;
            }

            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor anchor,
            Vector2 pos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.black;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
