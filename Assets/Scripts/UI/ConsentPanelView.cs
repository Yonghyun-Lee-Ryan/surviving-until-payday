using System;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 첫 실행 광고/개인정보 동의 패널.
    /// </summary>
    public sealed class ConsentPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private PrivacyPolicyConfig privacyConfig;

        private Action onAccepted;
        private bool buttonsWired;

        private void Awake()
        {
            WireButtonsIfNeeded();
            // 씬에서 비활성으로 시작하는 경우 Awake는 Show()의 SetActive(true) 직후에 호출된다.
            // 여기서 Hide()를 호출하면 동의 패널이 바로 다시 꺼져 스플래시에 영구 정지한다.
        }

        private void OnDestroy()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(OnAcceptClicked);
            }

            if (privacyButton != null)
            {
                privacyButton.onClick.RemoveListener(OpenPrivacyPolicy);
            }

            buttonsWired = false;
        }

        public void Bind(
            GameObject panelRoot,
            Text summary,
            Button accept,
            Button privacy,
            PrivacyPolicyConfig config)
        {
            root = panelRoot;
            summaryLabel = summary;
            acceptButton = accept;
            privacyButton = privacy;
            privacyConfig = config;
            buttonsWired = false;
            WireButtonsIfNeeded();
        }

        public void Show(Action acceptedCallback)
        {
            onAccepted = acceptedCallback;
            WireButtonsIfNeeded();
            if (summaryLabel != null)
            {
                summaryLabel.raycastTarget = false;
                if (privacyConfig != null)
                {
                    summaryLabel.text = privacyConfig.SummaryText;
                }
            }

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }
        }

        private void WireButtonsIfNeeded()
        {
            if (buttonsWired)
            {
                return;
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(OnAcceptClicked);
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (privacyButton != null)
            {
                privacyButton.onClick.RemoveListener(OpenPrivacyPolicy);
                privacyButton.onClick.AddListener(OpenPrivacyPolicy);
            }

            buttonsWired = acceptButton != null || privacyButton != null;
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnAcceptClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.Settings?.CompleteConsent(privacyAccepted: true, adsConsentGranted: true);
            appRoot.Settings?.TryVibrate();
            Hide();
            onAccepted?.Invoke();
        }

        private void OpenPrivacyPolicy()
        {
            PrivacyPolicyOpener.Open(privacyConfig);
        }
    }
}
