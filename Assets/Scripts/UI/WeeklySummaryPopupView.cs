using System;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 7/14/21 주간 결산 팝업.
    /// </summary>
    public sealed class WeeklySummaryPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text warningsLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueButtonLabel;

        public event Action ContinueClicked;

        private void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinue);
            }
        }

        private void OnDisable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinue);
            }
        }

        public void Bind(
            GameObject panelRoot,
            Text title,
            Text body,
            Text warnings,
            Button continueBtn,
            Text continueLabel)
        {
            root = panelRoot;
            titleLabel = title;
            bodyLabel = body;
            warningsLabel = warnings;
            continueButton = continueBtn;
            continueButtonLabel = continueLabel;
        }

        public Transform RootTransform => root != null ? root.transform : transform;

        public void Show(string title, string body, string warnings, string continueText = "계속")
        {
            if (root != null)
            {
                root.SetActive(true);
                UiModalLayer.BringToFront(root.transform);
            }
            else
            {
                gameObject.SetActive(true);
                UiModalLayer.BringToFront(transform);
            }

            if (titleLabel != null)
            {
                titleLabel.text = title ?? "주간 결산";
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = body ?? string.Empty;
            }

            if (warningsLabel != null)
            {
                warningsLabel.text = warnings ?? string.Empty;
            }

            if (continueButtonLabel != null)
            {
                continueButtonLabel.text = continueText ?? "계속";
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
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

        private void OnContinue()
        {
            ContinueClicked?.Invoke();
        }
    }
}
