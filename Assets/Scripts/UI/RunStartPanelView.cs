using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 새 회차 시작: 직업 안내 + 해금 특성 선택(또는 없음).
    /// 특성 목록은 ScrollRect Content에 배치한다. 「특성 없음」은 목록 맨 위 항목이다.
    /// </summary>
    public sealed class RunStartPanelView : MonoBehaviour
    {
        private const string NoneTraitButtonName = "Trait_None";
        private const float TraitRowHeight = 72f;

        [SerializeField] private GameObject root;
        [SerializeField] private Text jobTitleLabel;
        [SerializeField] private Text jobDescriptionLabel;
        [SerializeField] private Text traitHintLabel;
        [SerializeField] private Transform traitButtonRoot;
        [SerializeField] private ScrollRect traitScroll;
        [SerializeField] private Button noneTraitButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text selectedTraitLabel;

        private readonly List<Button> traitButtons = new List<Button>();
        private TraitData selectedTrait;
        private Action<TraitData> onConfirm;
        private Action onCancel;
        private bool isShowing;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void Start()
        {
            if (!isShowing)
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }

            ClearTraitButtons();
        }

        public void Bind(
            GameObject panelRoot,
            Text jobTitle,
            Text jobDescription,
            Text traitHint,
            Transform traitRoot,
            Button noneTrait,
            Button confirm,
            Button cancel,
            Text selectedTrait,
            ScrollRect scroll = null)
        {
            root = panelRoot;
            jobTitleLabel = jobTitle;
            jobDescriptionLabel = jobDescription;
            traitHintLabel = traitHint;
            traitButtonRoot = traitRoot;
            noneTraitButton = noneTrait;
            confirmButton = confirm;
            cancelButton = cancel;
            selectedTraitLabel = selectedTrait;
            traitScroll = scroll;
        }

        public bool IsVisible => root != null && root.activeSelf;

        public void Show(
            JobData job,
            IReadOnlyList<TraitData> unlockedTraits,
            Action<TraitData> confirm,
            Action cancel)
        {
            onConfirm = confirm;
            onCancel = cancel;
            selectedTrait = null;
            isShowing = true;

            if (jobTitleLabel != null)
            {
                jobTitleLabel.text = job != null ? job.DisplayName : "직업 미정";
                UiFont.Apply(jobTitleLabel, bold: true);
            }

            if (jobDescriptionLabel != null)
            {
                jobDescriptionLabel.text = job != null
                    ? $"{job.Description}\n월급 {job.Salary:N0}원 · 시작 현금 {job.StartingCash:N0}원"
                    : string.Empty;
                UiFont.Apply(jobDescriptionLabel);
            }

            var unlockedCount = unlockedTraits?.Count ?? 0;
            if (traitHintLabel != null)
            {
                traitHintLabel.text = unlockedCount > 0
                    ? $"해금된 특성 {unlockedCount}개 — 스크롤해서 고르세요."
                    : "해금된 특성이 없습니다. 「특성 없음」으로 시작할 수 있습니다.";
                UiFont.Apply(traitHintLabel);
            }

            if (root != null)
            {
                root.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            ApplyCenteredLayout();
            EnsureScrollInfrastructure();
            RebuildTraitButtons(unlockedTraits);
            RefreshSelectionLabel();
            HighlightSelection(null);
            if (traitScroll != null)
            {
                traitScroll.verticalNormalizedPosition = 1f;
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// 스크롤을 화면 중앙(높이 ≈ 이전의 2/3)에 두고,
        /// 상단 텍스트는 스크롤 위 여백 중앙, 시작/취소는 스크롤 아래 여백 중앙에 배치한다.
        /// </summary>
        private void ApplyCenteredLayout()
        {
            var panel = root != null ? root.transform as RectTransform : transform as RectTransform;
            if (panel == null)
            {
                return;
            }

            panel.SetAsLastSibling();
            StretchFull(panel);

            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);
                panelImage.raycastTarget = true;
            }

            // 하단 고정 「특성 없이 시작」은 목록 항목으로 대체
            if (noneTraitButton != null)
            {
                noneTraitButton.gameObject.SetActive(false);
            }

            SetButtonLabel(confirmButton, "시작");

            Canvas.ForceUpdateCanvases();
            var panelHeight = panel.rect.height;
            if (panelHeight < 200f)
            {
                panelHeight = 1920f;
            }

            // 이전 stretch 높이(대략 H-750)의 약 2/3, 화면 중앙
            var previousScrollHeight = Mathf.Max(280f, panelHeight - 750f);
            var scrollHeight = previousScrollHeight * (2f / 3f);
            var sideGap = (panelHeight - scrollHeight) * 0.5f;

            var scrollRectTransform = traitScroll != null
                ? traitScroll.GetComponent<RectTransform>()
                : panel.Find("TraitScroll") as RectTransform;
            if (scrollRectTransform != null)
            {
                scrollRectTransform.anchorMin = new Vector2(0.05f, 0.5f);
                scrollRectTransform.anchorMax = new Vector2(0.95f, 0.5f);
                scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
                scrollRectTransform.anchoredPosition = Vector2.zero;
                scrollRectTransform.sizeDelta = new Vector2(0f, scrollHeight);
            }

            // 상단 텍스트 블록을 (화면 상단 ~ 스크롤 상단) 구간의 중앙에
            const float titleH = 48f;
            const float descH = 96f;
            const float hintH = 36f;
            const float selectedH = 100f;
            const float stackGap = 8f;
            var headerBlock =
                titleH + stackGap + descH + stackGap + hintH + stackGap + selectedH;
            var headerTop = Mathf.Clamp(
                (sideGap - headerBlock) * 0.5f,
                8f,
                Mathf.Max(8f, sideGap - 8f));

            PlaceFromTop(jobTitleLabel?.rectTransform, headerTop, titleH);
            PlaceFromTop(jobDescriptionLabel?.rectTransform, headerTop + titleH + stackGap, descH);
            PlaceFromTop(
                traitHintLabel?.rectTransform,
                headerTop + titleH + stackGap + descH + stackGap,
                hintH);
            PlaceFromTop(
                selectedTraitLabel?.rectTransform,
                headerTop + titleH + stackGap + descH + stackGap + hintH + stackGap,
                selectedH);

            ConfigureHeaderTexts();

            // 시작/취소: 동일 크기, (스크롤 하단 ~ 화면 하단) 중앙
            const float buttonW = 520f;
            const float buttonH = 88f;
            const float btnGap = 16f;
            var buttonBlock = buttonH + btnGap + buttonH;
            var blockBottom = Mathf.Clamp(
                (sideGap - buttonBlock) * 0.5f,
                12f,
                Mathf.Max(12f, sideGap - 12f));

            PlaceBottom(cancelButton?.GetComponent<RectTransform>(), blockBottom, buttonW, buttonH);
            PlaceBottom(
                confirmButton?.GetComponent<RectTransform>(),
                blockBottom + buttonH + btnGap,
                buttonW,
                buttonH);
        }

        private void ConfigureHeaderTexts()
        {
            if (jobTitleLabel != null)
            {
                jobTitleLabel.alignment = TextAnchor.MiddleCenter;
                jobTitleLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (jobDescriptionLabel != null)
            {
                jobDescriptionLabel.alignment = TextAnchor.MiddleCenter;
                jobDescriptionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                jobDescriptionLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }

            if (traitHintLabel != null)
            {
                traitHintLabel.alignment = TextAnchor.MiddleCenter;
                traitHintLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (selectedTraitLabel != null)
            {
                selectedTraitLabel.alignment = TextAnchor.UpperCenter;
                selectedTraitLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                selectedTraitLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlaceFromTop(RectTransform rect, float topInset, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.05f, 1f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topInset);
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void PlaceBottom(RectTransform rect, float bottomOffset, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottomOffset);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                UiFont.Apply(label);
            }
        }

        public void Hide()
        {
            isShowing = false;
            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureScrollInfrastructure()
        {
            if (traitButtonRoot == null)
            {
                return;
            }

            if (traitScroll == null)
            {
                traitScroll = traitButtonRoot.GetComponentInParent<ScrollRect>();
            }

            if (traitScroll != null)
            {
                if (traitScroll.content == null)
                {
                    traitScroll.content = traitButtonRoot as RectTransform;
                }

                return;
            }

            var parent = traitButtonRoot.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var scrollGo = new GameObject("TraitScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            scrollGo.transform.SetSiblingIndex(traitButtonRoot.GetSiblingIndex());
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            var old = traitButtonRoot as RectTransform;
            scrollRectTransform.anchorMin = old.anchorMin;
            scrollRectTransform.anchorMax = old.anchorMax;
            scrollRectTransform.pivot = old.pivot;
            scrollRectTransform.anchoredPosition = old.anchoredPosition;
            scrollRectTransform.sizeDelta = old.sizeDelta;
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.09f, 0.11f, 0.75f);
            scrollImage.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            traitButtonRoot.SetParent(viewportGo.transform, false);
            old.anchorMin = new Vector2(0f, 1f);
            old.anchorMax = new Vector2(1f, 1f);
            old.pivot = new Vector2(0.5f, 1f);
            old.anchoredPosition = Vector2.zero;
            old.sizeDelta = new Vector2(0f, 0f);

            traitScroll = scrollGo.GetComponent<ScrollRect>();
            traitScroll.content = old;
            traitScroll.viewport = viewportRect;
            traitScroll.horizontal = false;
            traitScroll.vertical = true;
            traitScroll.movementType = ScrollRect.MovementType.Clamped;
            traitScroll.scrollSensitivity = 40f;
        }

        private void RebuildTraitButtons(IReadOnlyList<TraitData> unlockedTraits)
        {
            ClearTraitButtons();
            if (traitButtonRoot == null)
            {
                Debug.LogWarning("[RunStartPanelView] traitButtonRoot is not assigned.");
                return;
            }

            EnsureTraitRootLayout();

            var noneButton = CreateListButton(
                NoneTraitButtonName,
                "특성 없음",
                new Color(0.35f, 0.35f, 0.4f, 1f),
                OnNoneTraitClicked);
            if (noneButton != null)
            {
                traitButtons.Add(noneButton);
            }

            if (unlockedTraits == null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(traitButtonRoot as RectTransform);
                return;
            }

            for (var i = 0; i < unlockedTraits.Count; i++)
            {
                var trait = unlockedTraits[i];
                if (trait == null)
                {
                    continue;
                }

                var button = CreateTraitButton(trait);
                if (button != null)
                {
                    traitButtons.Add(button);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(traitButtonRoot as RectTransform);
        }

        private void EnsureTraitRootLayout()
        {
            var layout = traitButtonRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = traitButtonRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);

            var fitter = traitButtonRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = traitButtonRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentRect = traitButtonRoot as RectTransform;
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
            }
        }

        private Button CreateTraitButton(TraitData trait)
        {
            var captured = trait;
            return CreateListButton(
                $"Trait_{trait.Id}",
                FormatTraitButtonLabel(trait),
                new Color(0.28f, 0.42f, 0.55f, 1f),
                () =>
                {
                    selectedTrait = captured;
                    RefreshSelectionLabel();
                    HighlightSelection(captured);
                });
        }

        private Button CreateListButton(string objectName, string labelText, Color color, Action onClick)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(traitButtonRoot, false);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = TraitRowHeight;
            layoutElement.preferredHeight = TraitRowHeight;
            layoutElement.flexibleWidth = 1f;

            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 6f);
            labelRect.offsetMax = new Vector2(-16f, -6f);
            var label = labelGo.AddComponent<Text>();
            label.font = UiFont.Regular;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = labelText;
            UiFont.Apply(label);

            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        private static string FormatTraitButtonLabel(TraitData trait)
        {
            return trait.DisplayName ?? trait.Id ?? string.Empty;
        }

        private void HighlightSelection(TraitData selected)
        {
            for (var i = 0; i < traitButtons.Count; i++)
            {
                var button = traitButtons[i];
                if (button == null)
                {
                    continue;
                }

                var image = button.targetGraphic as Image;
                if (image == null)
                {
                    continue;
                }

                var isNone = button.gameObject.name == NoneTraitButtonName;
                var isSelected = selected == null
                    ? isNone
                    : button.gameObject.name == $"Trait_{selected.Id}";

                if (isSelected)
                {
                    image.color = new Color(0.2f, 0.55f, 0.4f, 1f);
                }
                else if (isNone)
                {
                    image.color = new Color(0.35f, 0.35f, 0.4f, 1f);
                }
                else
                {
                    image.color = new Color(0.28f, 0.42f, 0.55f, 1f);
                }
            }
        }

        private void ClearTraitButtons()
        {
            for (var i = 0; i < traitButtons.Count; i++)
            {
                if (traitButtons[i] != null)
                {
                    Destroy(traitButtons[i].gameObject);
                }
            }

            traitButtons.Clear();
        }

        private void OnNoneTraitClicked()
        {
            selectedTrait = null;
            RefreshSelectionLabel();
            HighlightSelection(null);
        }

        private void OnConfirmClicked()
        {
            onConfirm?.Invoke(selectedTrait);
        }

        private void OnCancelClicked()
        {
            onCancel?.Invoke();
            Hide();
        }

        private void RefreshSelectionLabel()
        {
            if (selectedTraitLabel == null)
            {
                return;
            }

            if (selectedTrait == null)
            {
                selectedTraitLabel.text = "선택: 특성 없음";
            }
            else
            {
                var desc = selectedTrait.Description ?? string.Empty;
                selectedTraitLabel.text = string.IsNullOrWhiteSpace(desc)
                    ? $"선택: {selectedTrait.DisplayName}"
                    : $"선택: {selectedTrait.DisplayName}\n{desc}";
            }

            selectedTraitLabel.alignment = TextAnchor.UpperCenter;
            selectedTraitLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            selectedTraitLabel.verticalOverflow = VerticalWrapMode.Overflow;
            UiFont.Apply(selectedTraitLabel);
        }
    }
}
