using System;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 선택 결과 팝업 + 다음 날 / 보상형 광고 버튼.
    /// </summary>
    public sealed class ResultPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Text changesLabel;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Text nextDayButtonLabel;
        [SerializeField] private Button retryAdButton;
        [SerializeField] private Text retryAdLabel;
        [SerializeField] private Button sideJobAdButton;
        [SerializeField] private Text sideJobAdLabel;
        [SerializeField] private Button loanAdButton;
        [SerializeField] private Text loanAdLabel;

        public event Action NextDayClicked;
        public event Action RetryAdClicked;
        public event Action SideJobAdClicked;
        public event Action LoanAdClicked;

        private void OnEnable()
        {
            WireButton();
        }

        private void OnDisable()
        {
            UnwireButton();
        }

        public void Show(string title, string message, string changes, string nextButtonText)
        {
            if (root != null)
            {
                root.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (titleLabel != null)
            {
                titleLabel.text = title ?? "결과";
            }

            if (messageLabel != null)
            {
                messageLabel.text = message ?? string.Empty;
            }

            if (changesLabel != null)
            {
                changesLabel.text = changes ?? string.Empty;
            }

            if (nextDayButtonLabel != null)
            {
                nextDayButtonLabel.text = nextButtonText ?? "다음 날";
            }

            if (nextDayButton != null)
            {
                nextDayButton.interactable = true;
            }

            LayoutNextDayInLowerGap();
        }

        /// <summary>
        /// 「다음 날」버튼을 변경사항 텍스트 하단과 카드 하단 사이 중앙에 둔다.
        /// </summary>
        private void LayoutNextDayInLowerGap()
        {
            if (nextDayButton == null || changesLabel == null)
            {
                return;
            }

            var buttonRect = nextDayButton.GetComponent<RectTransform>();
            var changesRect = changesLabel.rectTransform;
            var card = changesRect != null ? changesRect.parent as RectTransform : null;
            if (buttonRect == null || changesRect == null || card == null)
            {
                return;
            }

            changesLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            changesLabel.verticalOverflow = VerticalWrapMode.Overflow;
            Canvas.ForceUpdateCanvases();

            var textWidth = changesRect.sizeDelta.x > 10f ? changesRect.sizeDelta.x : 740f;
            var textHeight = Mathf.Max(changesLabel.preferredHeight, 48f);
            changesRect.sizeDelta = new Vector2(textWidth, textHeight);

            Canvas.ForceUpdateCanvases();

            var changesCorners = new Vector3[4];
            var cardCorners = new Vector3[4];
            changesRect.GetWorldCorners(changesCorners);
            card.GetWorldCorners(cardCorners);

            // corners[0] = bottom-left
            var midWorldY = (changesCorners[0].y + cardCorners[0].y) * 0.5f;
            var midLocal = card.InverseTransformPoint(new Vector3(card.position.x, midWorldY, card.position.z));

            buttonRect.SetParent(card, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(420f, 100f);
            buttonRect.anchoredPosition = new Vector2(0f, midLocal.y);
            buttonRect.SetAsLastSibling();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetNextDayInteractable(bool interactable)
        {
            if (nextDayButton != null)
            {
                nextDayButton.interactable = interactable;
            }
        }

        public void SetAdButtons(
            bool retryVisible,
            bool retryInteractable,
            bool sideJobVisible,
            bool sideJobInteractable,
            bool loanVisible,
            bool loanInteractable)
        {
            EnsureAdButtons();
            SetButton(retryAdButton, retryAdLabel, retryVisible, retryInteractable, "광고: 결과 재시도");
            SetButton(sideJobAdButton, sideJobAdLabel, sideJobVisible, sideJobInteractable, "광고: 부업(+30,000원)");
            SetButton(loanAdButton, loanAdLabel, loanVisible, loanInteractable, "광고: 긴급 대출(+100,000원)");
        }

        public void Bind(
            GameObject popupRoot,
            Text title,
            Text message,
            Text changes,
            Button nextButton,
            Text nextLabel)
        {
            UnwireButton();
            root = popupRoot;
            titleLabel = title;
            messageLabel = message;
            changesLabel = changes;
            nextDayButton = nextButton;
            nextDayButtonLabel = nextLabel;
            WireButton();
        }

        public void EnsureAdButtons()
        {
            var parent = root != null ? root.transform : transform;
            if (retryAdButton == null)
            {
                CreateAdButton(parent, "RetryAdButton", "광고: 결과 재시도", new Vector2(0f, 160f),
                    out retryAdButton, out retryAdLabel);
            }

            if (sideJobAdButton == null)
            {
                CreateAdButton(parent, "SideJobAdButton", "광고: 부업(+30,000원)", new Vector2(0f, 100f),
                    out sideJobAdButton, out sideJobAdLabel);
            }

            if (loanAdButton == null)
            {
                CreateAdButton(parent, "LoanAdButton", "광고: 긴급 대출(+100,000원)", new Vector2(0f, 40f),
                    out loanAdButton, out loanAdLabel);
            }

            WireAdButtons();
        }

        private void WireButton()
        {
            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
                nextDayButton.onClick.AddListener(HandleNextDayClicked);
            }

            WireAdButtons();
        }

        private void UnwireButton()
        {
            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
            }

            UnwireAdButtons();
        }

        private void WireAdButtons()
        {
            if (retryAdButton != null)
            {
                retryAdButton.onClick.RemoveListener(HandleRetryClicked);
                retryAdButton.onClick.AddListener(HandleRetryClicked);
            }

            if (sideJobAdButton != null)
            {
                sideJobAdButton.onClick.RemoveListener(HandleSideJobClicked);
                sideJobAdButton.onClick.AddListener(HandleSideJobClicked);
            }

            if (loanAdButton != null)
            {
                loanAdButton.onClick.RemoveListener(HandleLoanClicked);
                loanAdButton.onClick.AddListener(HandleLoanClicked);
            }
        }

        private void UnwireAdButtons()
        {
            if (retryAdButton != null)
            {
                retryAdButton.onClick.RemoveListener(HandleRetryClicked);
            }

            if (sideJobAdButton != null)
            {
                sideJobAdButton.onClick.RemoveListener(HandleSideJobClicked);
            }

            if (loanAdButton != null)
            {
                loanAdButton.onClick.RemoveListener(HandleLoanClicked);
            }
        }

        private void HandleNextDayClicked()
        {
            NextDayClicked?.Invoke();
        }

        private void HandleRetryClicked()
        {
            RetryAdClicked?.Invoke();
        }

        private void HandleSideJobClicked()
        {
            SideJobAdClicked?.Invoke();
        }

        private void HandleLoanClicked()
        {
            LoanAdClicked?.Invoke();
        }

        private static void SetButton(Button button, Text label, bool visible, bool interactable, string text)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.interactable = interactable;
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void CreateAdButton(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPos,
            out Button button,
            out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(520f, 52f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.35f, 0.28f, 0.45f, 1f);
            button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label = labelGo.AddComponent<Text>();
            label.font = UiFont.Regular;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            go.SetActive(false);
        }
    }
}
