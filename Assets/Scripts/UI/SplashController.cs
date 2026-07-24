using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Bootstrap 스플래시: 로고/버전 표시 후 동의 확인 → MainMenu.
    /// </summary>
    public sealed class SplashController : MonoBehaviour
    {
        [SerializeField] private float minimumSplashSeconds = 1.25f;
        [SerializeField] private Text versionLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private ConsentPanelView consentPanel;

        private float elapsed;
        private bool leaving;

        private void Start()
        {
            CanvasSetupUtility.EnsureEventSystem();
            AppRoot.EnsureCreated();
            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version}";
            }

            if (titleLabel != null && string.IsNullOrEmpty(titleLabel.text))
            {
                titleLabel.text = "월급날까지 살아남기";
            }
        }

        private void Update()
        {
            if (leaving)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < minimumSplashSeconds)
            {
                return;
            }

            TryProceed();
        }

        public void Bind(float minSeconds, Text version, Text title, ConsentPanelView consent)
        {
            minimumSplashSeconds = minSeconds;
            versionLabel = version;
            titleLabel = title;
            consentPanel = consent;
        }

        private void TryProceed()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot?.Settings == null)
            {
                GoMainMenu();
                return;
            }

            if (appRoot.Settings.ConsentFlowCompleted)
            {
                GoMainMenu();
                return;
            }

            leaving = true;
            if (consentPanel == null)
            {
                appRoot.Settings.CompleteConsent(true, true);
                GoMainMenu();
                return;
            }

            // 스플래시가 동의 UI를 가리지 않도록 뒤로 보내고 레이캐스트를 끈다.
            transform.SetAsFirstSibling();
            var splashImage = GetComponent<Image>();
            if (splashImage != null)
            {
                splashImage.raycastTarget = false;
            }

            consentPanel.Show(GoMainMenu);
        }

        private void GoMainMenu()
        {
            leaving = true;
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[SplashController] SceneLoader missing.");
                return;
            }

            appRoot.SceneLoader.LoadMainMenu();
        }
    }
}
