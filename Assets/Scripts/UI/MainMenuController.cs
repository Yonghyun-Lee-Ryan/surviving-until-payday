using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// MainMenu: 새 게임 / 이어하기 / 도감 해금률.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private CodexPanelView codexPanel;
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
        }

        public void Bind(Button startButton, Button continueGameButton, CodexPanelView codex)
        {
            startGameButton = startButton;
            continueButton = continueGameButton;
            codexPanel = codex;
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

            appRoot.Session.StartMode = GameStartMode.ContinueRun;
            appRoot.SceneLoader.LoadGame();
        }
    }
}
