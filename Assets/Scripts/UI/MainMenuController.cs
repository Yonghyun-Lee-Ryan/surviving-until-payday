using System.Collections.Generic;
using SurviveUntilPayday.Audio;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// MainMenu: 새 게임 / 이어하기 / 도감 해금률 / 설정 / 회차 시작(직업·특성).
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private CodexPanelView codexPanel;
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private RunStartPanelView runStartPanel;
        [SerializeField] private JobData defaultJob;
        [SerializeField] private List<TraitData> traitCatalog = new List<TraitData>();
        [SerializeField] private int totalEndingCount = 9;
        [SerializeField] private int totalEventCount = 20;
        [SerializeField] private int totalTraitCount = 4;
        [SerializeField] private int totalAchievementCount = 5;

        private MetaProgressionManager subscribedMeta;

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
            EnsureDefaultCatalog();
            RefreshContinueButton();
            RefreshCodex();
            SubscribeUnlockNotifications();
            ShowLastRunUnlockToast();
            AppRoot.EnsureCreated().Audio?.SetBgm(BgmId.Main);
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

            if (subscribedMeta != null)
            {
                subscribedMeta.UnlockNotified -= OnUnlockNotified;
                subscribedMeta = null;
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

        public void BindRunStart(RunStartPanelView panel, JobData job, List<TraitData> traits)
        {
            runStartPanel = panel;
            defaultJob = job;
            traitCatalog = traits ?? new List<TraitData>();
        }

        private void EnsureDefaultCatalog()
        {
            if (defaultJob == null)
            {
                defaultJob = Resources.Load<JobData>("Jobs/Job_JuniorOffice");
            }

            if (traitCatalog == null)
            {
                traitCatalog = new List<TraitData>();
            }
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

        private void SubscribeUnlockNotifications()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            subscribedMeta = appRoot.Session?.Meta;
            if (subscribedMeta != null)
            {
                subscribedMeta.UnlockNotified -= OnUnlockNotified;
                subscribedMeta.UnlockNotified += OnUnlockNotified;
            }
        }

        private void ShowLastRunUnlockToast()
        {
            if (codexPanel == null)
            {
                return;
            }

            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var metaProgress = appRoot.Session?.LastResult?.MetaProgress;
            if (metaProgress == null)
            {
                return;
            }

            var message = BuildUnlockToast(metaProgress);
            if (!string.IsNullOrEmpty(message))
            {
                codexPanel.ShowUnlockToast(message);
            }
        }

        private void OnUnlockNotified(string category, string id, string displayName)
        {
            if (codexPanel == null)
            {
                return;
            }

            var label = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            codexPanel.ShowUnlockToast($"해금: {CategoryLabel(category)} {label}");
            RefreshCodex();
        }

        private static string BuildUnlockToast(MetaProgressResult progress)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (progress.NewlyUnlockedEndings.Count > 0)
            {
                parts.Add($"엔딩 {progress.NewlyUnlockedEndings.Count}");
            }

            if (progress.NewlyUnlockedEvents.Count > 0)
            {
                parts.Add($"사건 {progress.NewlyUnlockedEvents.Count}");
            }

            if (progress.NewlyUnlockedTraits.Count > 0)
            {
                parts.Add($"특성 {progress.NewlyUnlockedTraits.Count}");
            }

            if (progress.NewlyUnlockedAchievements.Count > 0)
            {
                parts.Add($"업적 {progress.NewlyUnlockedAchievements.Count}");
            }

            return parts.Count == 0 ? string.Empty : $"새 해금: {string.Join(", ", parts)}";
        }

        private static string CategoryLabel(string category)
        {
            switch (category)
            {
                case "ending":
                    return "엔딩";
                case "event":
                    return "사건";
                case "trait":
                    return "특성";
                case "achievement":
                    return "업적";
                default:
                    return category ?? string.Empty;
            }
        }

        private void OnStartGameClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.Audio?.PlaySfx(SfxId.Click);
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            appRoot.Settings?.TryVibrate();
            OpenRunStartPanel();
        }

        private void OpenRunStartPanel()
        {
            EnsureDefaultCatalog();
            if (defaultJob == null)
            {
                Debug.LogError("[MainMenuController] defaultJob is not assigned. Starting without selection UI.");
                StartNewRun(null);
                return;
            }

            if (runStartPanel == null)
            {
                Debug.LogWarning("[MainMenuController] runStartPanel missing. Starting with no trait.");
                StartNewRun(null);
                return;
            }

            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var unlocked = CollectUnlockedTraits(appRoot.Session?.Meta);
            runStartPanel.Show(defaultJob, unlocked, StartNewRun, () => { });
        }

        private List<TraitData> CollectUnlockedTraits(MetaProgressionManager meta)
        {
            var unlocked = new List<TraitData>();
            if (traitCatalog == null)
            {
                return unlocked;
            }

            for (var i = 0; i < traitCatalog.Count; i++)
            {
                var trait = traitCatalog[i];
                if (trait == null)
                {
                    continue;
                }

                if (meta == null || meta.IsTraitUnlocked(trait))
                {
                    unlocked.Add(trait);
                }
            }

            return unlocked;
        }

        private void StartNewRun(TraitData selectedTrait)
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            EnsureDefaultCatalog();
            appRoot.Session.SetPendingNewRun(defaultJob, selectedTrait);

            var analytics = appRoot.Analytics;
            if (analytics != null)
            {
                if (defaultJob != null)
                {
                    analytics.JobSelected(defaultJob.Id);
                }

                analytics.TraitSelected(selectedTrait != null ? selectedTrait.Id : string.Empty);
            }

            if (runStartPanel != null)
            {
                runStartPanel.Hide();
            }

            appRoot.SceneLoader.LoadGame();
        }

        private void OnContinueClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.Audio?.PlaySfx(SfxId.Click);
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
            appRoot.Session.ClearPendingRunSelection();
            appRoot.Session.StartMode = GameStartMode.ContinueRun;
            appRoot.SceneLoader.LoadGame();
        }

        private void OnSettingsClicked()
        {
            AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);
            if (settingsPanel != null)
            {
                settingsPanel.Toggle();
            }
        }
    }
}
