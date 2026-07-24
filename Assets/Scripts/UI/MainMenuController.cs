using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// MainMenu: 새 게임 / 이어하기 / 도감 해금률 / 설정.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private CodexPanelView codexPanel;
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private int totalEndingCount = 9;
        [SerializeField] private int totalEventCount = 3;
        [SerializeField] private int totalTraitCount = 4;
        [SerializeField] private int totalAchievementCount = 5;

        private void Awake()
        {
            if (startGameButton == null)
            {
                Debug.LogError("[MainMenuController] startGameButton is not assigned.", this);
            }
            else
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void Start()
        {
            RefreshContinueButton();
            RefreshCodex();
        }

        private void OnDestroy()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }
        }

        public void Bind(Button startButton, Button continueGameButton, CodexPanelView codex)
        {
            startGameButton = startButton;
            continueButton = continueGameButton;
            codexPanel = codex;
        }

        public void BindSettings(Button settings, SettingsPanelView panel)
        {
            settingsButton = settings;
            settingsPanel = panel;
        }

        private void RefreshContinueButton()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var hasRun = appRoot.Session != null && appRoot.Session.HasActiveRun;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(hasRun);
            }
        }

        private void RefreshCodex()
        {
            if (codexPanel == null)
            {
                return;
            }

            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            codexPanel.Refresh(
                appRoot.Session.Meta,
                totalEndingCount,
                totalEventCount,
                totalTraitCount,
                totalAchievementCount);
        }

        private void OnStartGameClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            appRoot.Settings?.TryVibrate();
            appRoot.Session.StartMode = GameStartMode.NewRun;
            appRoot.SceneLoader.LoadGame();
        }

        private void OnContinueClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            if (appRoot.Session == null || !appRoot.Session.HasActiveRun)
            {
                Debug.LogWarning("[MainMenuController] No active run to continue.");
                RefreshContinueButton();
                return;
            }

            appRoot.Settings?.TryVibrate();
            appRoot.Session.StartMode = GameStartMode.ContinueRun;
            appRoot.SceneLoader.LoadGame();
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.Toggle();
            }
        }
    }
}
