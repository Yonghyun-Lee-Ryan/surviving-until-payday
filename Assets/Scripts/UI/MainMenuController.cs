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
        [SerializeField] private Button dailyButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private CodexPanelView codexPanel;
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private RunStartPanelView runStartPanel;
        [SerializeField] private DailyPanelView dailyPanel;
        [SerializeField] private ShopPanelView shopPanel;
        [SerializeField] private TutorialOverlayView tutorialOverlay;
        [SerializeField] private JobData defaultJob;
        [SerializeField] private List<JobData> jobCatalog = new List<JobData>();
        [SerializeField] private List<TraitData> traitCatalog = new List<TraitData>();
        [SerializeField] private List<EndingData> endingCatalog = new List<EndingData>();
        [SerializeField] private List<EventData> eventCatalog = new List<EventData>();
        [SerializeField] private List<DailyMissionData> dailyMissionPool = new List<DailyMissionData>();
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

            EnsureDailyEntryPoints();
            if (dailyButton != null)
            {
                dailyButton.onClick.AddListener(OnDailyClicked);
            }

            EnsureShopEntryPoints();
            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopClicked);
            }

            ApplyMainMenuChromeLayout();
        }

        private void Start()
        {
            EnsureDefaultCatalog();
            ApplyMainMenuChromeLayout();
            RefreshContinueButton();
            RefreshCodex();
            RefreshDailyContent();
            SubscribeUnlockNotifications();
            ShowLastRunUnlockToast();
            TryShowFirstRunTutorial();
            AppRoot.EnsureCreated().Audio?.SetBgm(BgmId.Main);
            AppRoot.EnsureCreated().ApplyMonetizationFromMeta(
                AppRoot.Instance?.Session?.CachedSave?.meta);
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

            if (dailyButton != null)
            {
                dailyButton.onClick.RemoveListener(OnDailyClicked);
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OnShopClicked);
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

        public void BindDaily(Button daily, DailyPanelView panel, List<DailyMissionData> missions)
        {
            dailyButton = daily;
            dailyPanel = panel;
            dailyMissionPool = missions ?? new List<DailyMissionData>();
        }

        public void BindShop(Button shop, ShopPanelView panel)
        {
            shopButton = shop;
            shopPanel = panel;
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

            // Play Mode에서 카탈로그가 비어 있으면 로드된 TraitData를 모은다.
            if (traitCatalog.Count == 0)
            {
                var loaded = Resources.FindObjectsOfTypeAll<TraitData>();
                for (var i = 0; i < loaded.Length; i++)
                {
                    var trait = loaded[i];
                    if (trait == null || string.IsNullOrWhiteSpace(trait.Id))
                    {
                        continue;
                    }

                    EnsureTraitInCatalog(trait);
                }
            }
        }

        private void EnsureTraitInCatalog(TraitData trait)
        {
            if (trait == null || traitCatalog == null)
            {
                return;
            }

            for (var i = 0; i < traitCatalog.Count; i++)
            {
                if (traitCatalog[i] == trait ||
                    (traitCatalog[i] != null && traitCatalog[i].Id == trait.Id))
                {
                    return;
                }
            }

            traitCatalog.Add(trait);
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
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = hasRun;
                var label = continueButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = hasRun ? "이어하기" : "이어할 회차 없음";
                    UiFont.Apply(label, bold: true);
                }

                var colors = continueButton.colors;
                colors.disabledColor = new Color(0.55f, 0.58f, 0.6f, 0.85f);
                continueButton.colors = colors;
            }

            RelayoutPrimaryMenuButtons();
        }

        private void TryShowFirstRunTutorial()
        {
            var appRoot = AppRoot.EnsureCreated();
            var meta = appRoot.Session?.Meta;
            if (meta == null || meta.FirstRunTutorialCompleted)
            {
                return;
            }

            // 이미 진행한 세이브는 튜토리얼을 생략한다.
            if (meta.TotalExperience > 0)
            {
                meta.MarkFirstRunTutorialCompleted();
                appRoot.PersistSession(includeActiveRun: appRoot.Session.HasActiveRun);
                return;
            }

            EnsureTutorialOverlay();
            tutorialOverlay?.Show(() =>
            {
                meta.MarkFirstRunTutorialCompleted();
                appRoot.PersistSession(includeActiveRun: appRoot.Session != null && appRoot.Session.HasActiveRun);
            });
        }

        private void EnsureTutorialOverlay()
        {
            if (tutorialOverlay != null)
            {
                return;
            }

            tutorialOverlay = FindAnyObjectByType<TutorialOverlayView>(FindObjectsInactive.Include);
            if (tutorialOverlay != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;
            var go = new GameObject(
                "TutorialOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(TutorialOverlayView));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            tutorialOverlay = go.GetComponent<TutorialOverlayView>();
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

        private void OnDailyClicked()
        {
            AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);
            EnsureDailyEntryPoints();
            RefreshDailyContent();
            var appRoot = AppRoot.EnsureCreated();
            var daily = appRoot.Session?.Meta?.Daily;
            if (dailyPanel == null || daily == null)
            {
                Debug.LogWarning("[MainMenuController] Daily panel unavailable.");
                return;
            }

            dailyPanel.Toggle(daily, StartDailyChallenge);
        }

        private void OnShopClicked()
        {
            AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);
            EnsureShopEntryPoints();
            if (shopPanel == null)
            {
                Debug.LogWarning("[MainMenuController] Shop panel unavailable.");
                return;
            }

            EnsureDefaultCatalog();
            shopPanel.SetTraitCatalog(traitCatalog);
            shopPanel.Toggle();
        }

        private void RefreshDailyContent()
        {
            var appRoot = AppRoot.EnsureCreated();
            var session = appRoot.Session;
            if (session == null)
            {
                return;
            }

            EnsureDailyMissionPool();
            session.SetDailyMissionPool(dailyMissionPool);
            session.Meta.Daily.EnsureForLocalDate(dailyMissionPool);
            session.Meta.Daily.BindMissionDefinitions(dailyMissionPool);
            appRoot.PersistSession(includeActiveRun: session.HasActiveRun);
        }

        private void EnsureDailyMissionPool()
        {
            if (dailyMissionPool == null)
            {
                dailyMissionPool = new List<DailyMissionData>();
            }

            if (dailyMissionPool.Count > 0)
            {
                return;
            }

            var loaded = Resources.LoadAll<DailyMissionData>("Missions");
            if (loaded != null)
            {
                for (var i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                    {
                        dailyMissionPool.Add(loaded[i]);
                    }
                }
            }

            if (dailyMissionPool.Count == 0)
            {
                dailyMissionPool.AddRange(DailyMissionDefaults.CreateRuntimePool());
            }
        }

        private void StartDailyChallenge()
        {
            var appRoot = AppRoot.EnsureCreated();
            if (appRoot.SceneLoader == null)
            {
                Debug.LogError("[MainMenuController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            EnsureDefaultCatalog();
            RefreshDailyContent();
            var job = defaultJob;
            var seed = DailyChallenge.SeedForLocalToday();
            appRoot.Session.SetPendingDailyRun(job, trait: null, seed);
            dailyPanel?.Hide();
            appRoot.Settings?.TryVibrate();
            appRoot.SceneLoader.LoadGame();
        }

        private void EnsureDailyEntryPoints()
        {
            if (dailyButton == null)
            {
                dailyButton = transform.Find("DailyButton")?.GetComponent<Button>();
            }

            if (dailyButton == null && startGameButton != null)
            {
                var parent = startGameButton.transform.parent;
                dailyButton = parent != null
                    ? parent.Find("DailyButton")?.GetComponent<Button>()
                    : null;
            }

            if (dailyButton == null && startGameButton != null)
            {
                dailyButton = CreateMenuButton(
                    "DailyButton",
                    "오늘의 직장인",
                    startGameButton.transform.parent,
                    startGameButton.transform as RectTransform,
                    yOffset: 0f);
            }

            RelayoutPrimaryMenuButtons();

            if (dailyPanel == null)
            {
                dailyPanel = FindAnyObjectByType<DailyPanelView>(FindObjectsInactive.Include);
            }

            if (dailyPanel == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                var parent = canvas != null ? canvas.transform : transform;
                var go = new GameObject(
                    "DailyPanel",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image),
                    typeof(DailyPanelView));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                dailyPanel = go.GetComponent<DailyPanelView>();
            }
        }

        private void EnsureShopEntryPoints()
        {
            if (shopButton == null)
            {
                shopButton = transform.Find("ShopButton")?.GetComponent<Button>();
            }

            if (shopButton == null && startGameButton != null)
            {
                var parent = startGameButton.transform.parent;
                shopButton = parent != null
                    ? parent.Find("ShopButton")?.GetComponent<Button>()
                    : null;
            }

            if (shopButton == null && startGameButton != null)
            {
                shopButton = CreateMenuButton(
                    "ShopButton",
                    "상점",
                    startGameButton.transform.parent,
                    startGameButton.transform as RectTransform,
                    yOffset: 0f);
            }

            RelayoutPrimaryMenuButtons();

            if (shopPanel == null)
            {
                shopPanel = FindAnyObjectByType<ShopPanelView>(FindObjectsInactive.Include);
            }

            if (shopPanel == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                var parent = canvas != null ? canvas.transform : transform;
                var go = new GameObject(
                    "ShopPanel",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(ShopPanelView));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                shopPanel = go.GetComponent<ShopPanelView>();
            }
        }

        /// <summary>
        /// Scene 라벨 제거 + 제목/메인 버튼을 레벨(도감) 패널 위로 올린다.
        /// </summary>
        private void ApplyMainMenuChromeLayout()
        {
            var safe = ResolveSafeArea();
            if (safe == null)
            {
                return;
            }

            var sceneLabel = safe.Find("SceneLabel");
            if (sceneLabel != null)
            {
                sceneLabel.gameObject.SetActive(false);
            }

            var title = safe.Find("Title") as RectTransform;
            if (title != null)
            {
                title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
                title.pivot = new Vector2(0.5f, 0.5f);
                title.sizeDelta = new Vector2(920f, 90f);
                title.anchoredPosition = new Vector2(0f, 760f);
                title.SetAsLastSibling();
            }

            RelayoutPrimaryMenuButtons();
        }

        private Transform ResolveSafeArea()
        {
            if (startGameButton != null && startGameButton.transform.parent != null)
            {
                return startGameButton.transform.parent;
            }

            var safe = transform.Find("SafeArea");
            if (safe != null)
            {
                return safe;
            }

            return GameObject.Find("SafeArea")?.transform;
        }

        /// <summary>
        /// 새 게임 → 오늘의 직장인 → 상점 → 이어하기를 도감 패널 위 여백에 세로 배치한다.
        /// </summary>
        private void RelayoutPrimaryMenuButtons()
        {
            var startRect = startGameButton != null ? startGameButton.transform as RectTransform : null;
            var dailyRect = dailyButton != null ? dailyButton.transform as RectTransform : null;
            var shopRect = shopButton != null ? shopButton.transform as RectTransform : null;
            var continueRect = continueButton != null ? continueButton.transform as RectTransform : null;
            if (startRect == null)
            {
                return;
            }

            const float gap = 14f;
            const float height = 78f;
            const float startY = 560f;
            const float width = 520f;

            void Place(RectTransform rect, float y)
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(0f, y);
            }

            var row = 0;
            Place(startRect, startY - row * (height + gap));
            row++;
            Place(dailyRect, startY - row * (height + gap));
            row++;
            Place(shopRect, startY - row * (height + gap));
            row++;
            Place(continueRect, startY - row * (height + gap));

            startRect.SetAsLastSibling();
            dailyRect?.SetAsLastSibling();
            shopRect?.SetAsLastSibling();
            continueRect?.SetAsLastSibling();
        }

        private static Button CreateMenuButton(
            string name,
            string caption,
            Transform parent,
            RectTransform reference,
            float yOffset)
        {
            if (parent == null)
            {
                return null;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (reference != null)
            {
                rect.anchorMin = reference.anchorMin;
                rect.anchorMax = reference.anchorMax;
                rect.pivot = reference.pivot;
                rect.sizeDelta = reference.sizeDelta;
                rect.anchoredPosition = reference.anchoredPosition + new Vector2(0f, yOffset);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(520f, 86f);
                rect.anchoredPosition = new Vector2(0f, 560f);
            }

            var image = go.GetComponent<Image>();
            image.color = new Color(0.28f, 0.48f, 0.62f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);
            return button;
        }
    }
}
