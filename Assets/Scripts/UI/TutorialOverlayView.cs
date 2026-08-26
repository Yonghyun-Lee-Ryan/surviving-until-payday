using System;
using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 첫 실행 튜토리얼 오버레이 (Unit 26). 스킵·다음으로 진행.
    /// </summary>
    public sealed class TutorialOverlayView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        private static readonly Color CardColor = new Color(0.14f, 0.16f, 0.2f, 1f);
        private static readonly Color Accent = new Color(0.28f, 0.48f, 0.62f, 1f);

        private static readonly string[] StepTitles = TutorialCopy.Titles;
        private static readonly string[] StepBodies = TutorialCopy.Bodies;

        [SerializeField] private GameObject root;
        private Text titleLabel;
        private Text bodyLabel;
        private Text progressLabel;
        private Button nextButton;
        private Button skipButton;
        private int stepIndex;
        private Action onFinished;
        private bool layoutReady;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            EnsureLayout();
            Hide();
        }

        public void Show(Action finished)
        {
            EnsureLayout();
            onFinished = finished;
            stepIndex = 0;
            RefreshStep();
            if (root != null)
            {
                root.SetActive(true);
                UiModalLayer.BringToFront(root.transform);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void RefreshStep()
        {
            var clamped = Mathf.Clamp(stepIndex, 0, StepTitles.Length - 1);
            if (titleLabel != null)
            {
                titleLabel.text = StepTitles[clamped];
                UiFont.Apply(titleLabel, bold: true);
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = StepBodies[clamped];
                UiFont.Apply(bodyLabel);
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"{clamped + 1} / {StepTitles.Length}";
                UiFont.Apply(progressLabel);
            }

            if (nextButton != null)
            {
                var label = nextButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = clamped >= StepTitles.Length - 1 ? "시작하기" : "다음";
                    UiFont.Apply(label, bold: true);
                }
            }
        }

        private void OnNextClicked()
        {
            if (stepIndex >= StepTitles.Length - 1)
            {
                Finish();
                return;
            }

            stepIndex++;
            RefreshStep();
        }

        private void OnSkipClicked()
        {
            Finish();
        }

        private void Finish()
        {
            Hide();
            var callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        private void EnsureLayout()
        {
            if (layoutReady && titleLabel != null && bodyLabel != null)
            {
                return;
            }

            layoutReady = false;
            if (root == null)
            {
                root = gameObject;
            }

            var old = root.transform.Find("TutorialCard");
            if (old != null)
            {
                old.name = "TutorialCard_PendingDestroy";
                UnityEngine.Object.Destroy(old.gameObject);
            }

            var rootRect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            rootImage.color = OverlayColor;
            rootImage.raycastTarget = true;

            var card = new GameObject("TutorialCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 900f);
            card.GetComponent<Image>().color = CardColor;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 20);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleLabel = CreateLabel(card.transform, "Title", "튜토리얼", 32, 48f, true);
            progressLabel = CreateLabel(card.transform, "Progress", "1 / 5", 20, 28f, false);
            bodyLabel = CreateBody(card.transform, "Body", "", 360f);

            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(card.transform, false);
            row.GetComponent<LayoutElement>().preferredHeight = 60f;
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            skipButton = CreateButton(row.transform, "Skip", "건너뛰기", new Color(0.35f, 0.37f, 0.4f, 1f));
            nextButton = CreateButton(row.transform, "Next", "다음", Accent);
            skipButton.onClick.RemoveAllListeners();
            nextButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
            nextButton.onClick.AddListener(OnNextClicked);
            layoutReady = true;
        }

        private static Text CreateLabel(Transform parent, string name, string text, int size, float height, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold);
            return label;
        }

        private static Text CreateBody(Transform parent, string name, string text, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.28f, 1f);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<LayoutElement>().flexibleHeight = 1f;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 14f);
            rect.offsetMax = new Vector2(-16f, -14f);
            var label = labelGo.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            UiFont.Apply(label);
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string caption, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            go.GetComponent<LayoutElement>().preferredHeight = 56f;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            button.colors = colors;
            return button;
        }
    }
}
