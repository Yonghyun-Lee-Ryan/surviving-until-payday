using System;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 선택 결과 팝업 + 다음 날 버튼.
    /// </summary>
    public sealed class ResultPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Text changesLabel;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Text nextDayButtonLabel;

        public event Action NextDayClicked;

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

        private void WireButton()
        {
            if (nextDayButton == null)
            {
                return;
            }

            nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
            nextDayButton.onClick.AddListener(HandleNextDayClicked);
        }

        private void UnwireButton()
        {
            if (nextDayButton == null)
            {
                return;
            }

            nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
        }

        private void HandleNextDayClicked()
        {
            NextDayClicked?.Invoke();
        }
    }
}
