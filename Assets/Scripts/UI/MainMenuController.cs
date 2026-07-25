using System.Collections.Generic;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Audio;
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
        [SerializeField] private List<JobData> jobCatalog = new List<JobData>();
        [SerializeField] private List<TraitData> traitCatalog = new List<TraitData>();
        [SerializeField] private List<EndingData> endingCatalog = new List<EndingData>();
        [SerializeField] private List<EventData> eventCatalog = new List<EventData>();
        [SerializeField] private int totalEndingCount = 12;
        [SerializeField] private int totalEventCount = 55;
        [SerializeField] private int totalTraitCount = 4;
        [SerializeField] private int totalAchievementCount = AchievementIds.CatalogCount;

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
            if (job != null)
            {
                EnsureJobInCatalog(job);
            }
        }

        private void EnsureDefaultCatalog()
        {
            if (jobCatalog == null)
            {
                jobCatalog = new List<JobData>();
            }

            if (defaultJob == null)
            {
                defaultJob = Resources.Load<JobData>("Jobs/Job_JuniorOffice");
            }

            if (defaultJob != null)
            {
                EnsureJobInCatalog(defaultJob);
            }

            TryAddJobFromResources("Jobs/Job_CivilPrep");
            TryAddJobFromResources("Jobs/Job_Freelancer");

            // Resources 폴더가 없으면 Data 경로 에셋은 Editor Setup에서 주입한다.
            if (traitCatalog == null)
            {
                traitCatalog = new List<TraitData>();
            }
        }

        private void TryAddJobFromResources(string resourcesPath)
        {
            var job = Resources.Load<JobData>(resourcesPath);
            if (job != null)
            {
                EnsureJobInCatalog(job);
            }
        }

        private void EnsureJobInCatalog(JobData job)
        {
            if (job == null || jobCatalog == null)
            {
                return;
            }

            for (var i = 0; i < jobCatalog.Count; i++)
            {
                if (jobCatalog[i] == job)
                {
                    return;
                }
            }

            jobCatalog.Add(job);
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
            var endings = endingCatalog != null && endingCatalog.Count > 0
                ? endingCatalog
                : appRoot.Session?.EndingCatalog;
            var events = eventCatalog != null && eventCatalog.Count > 0
                ? eventCatalog
                : null;
            codexPanel.Refresh(
                appRoot.Session.Meta,
                totalEndingCount,
                totalEventCount,
                totalTraitCount,
                totalAchievementCount > 0 ? totalAchievementCount : AchievementIds.CatalogCount,
                endings,
                events);
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
            var parts = new List<string>();
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

            if (progress.NewlyUnlockedJobs.Count > 0)
            {
                parts.Add($"직업 {progress.NewlyUnlockedJobs.Count}");
            }

            if (progress.NewlyUnlockedAchievements.Count > 0)
            {
                parts.Add($"업적 {progress.NewlyUnlockedAchievements.Count}");
            }

            if (progress.TraitFragmentsGained > 0)
            {
                parts.Add($"조각 +{progress.TraitFragmentsGained}");
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
                case "job":
                    return "직업";
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
            var unlockedJobs = CollectUnlockedJobs(AppRoot.EnsureCreated().Session?.Meta);
            if (unlockedJobs.Count == 0 && defaultJob != null)
            {
                unlockedJobs.Add(defaultJob);
            }

            if (unlockedJobs.Count == 0)
            {
                Debug.LogError("[MainMenuController] No jobs available.");
                return;
            }

            if (runStartPanel == null)
            {
                Debug.LogWarning("[MainMenuController] runStartPanel missing. Starting with defaults.");
                StartNewRun(unlockedJobs[0], null);
                return;
            }

            var unlockedTraits = CollectUnlockedTraits(AppRoot.EnsureCreated().Session?.Meta);
            runStartPanel.Show(unlockedJobs, defaultJob, unlockedTraits, StartNewRun, () => { });
        }

        private List<JobData> CollectUnlockedJobs(MetaProgressionManager meta)
        {
            var unlocked = new List<JobData>();
            if (jobCatalog == null)
            {
                return unlocked;
            }

            for (var i = 0; i < jobCatalog.Count; i++)
            {
                var job = jobCatalog[i];
                if (job == null)
                {
                    continue;
                }

                if (meta == null || meta.IsJobUnlocked(job))
                {
                    unlocked.Add(job);
                }
            }

            return unlocked;
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

        private void StartNewRun(JobData selectedJob, TraitData selectedTrait)
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            EnsureDefaultCatalog();
            var job = selectedJob != null ? selectedJob : defaultJob;
            appRoot.Session.SetPendingNewRun(job, selectedTrait);

            var analytics = appRoot.Analytics;
            if (analytics != null)
            {
                if (job != null)
                {
                    analytics.JobSelected(job.Id);
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
            AppRoot.EnsureCreated().ToggleSettings();
        }
    }
}
