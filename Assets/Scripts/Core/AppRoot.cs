using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Analytics;
using SurviveUntilPayday.Save;
using SurviveUntilPayday.Services;
using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// DontDestroyOnLoad를 쓰는 유일한 루트.
    /// 필수 Manager는 이 오브젝트 하위에서만 유지한다.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private SdkIntegrationConfig sdkConfig;
        [SerializeField] private PrivacyPolicyConfig privacyPolicyConfig;

        public SceneLoader SceneLoader => sceneLoader;

        public GameSession Session { get; private set; }

        public SaveRepository SaveRepository { get; private set; }

        public IAdService AdService { get; private set; }

        public AdQuotaTracker AdQuota { get; private set; }

        public RewardedAdGateway RewardedAds { get; private set; }

        public InterstitialAdGateway InterstitialAds { get; private set; }

        public IAnalyticsService AnalyticsService { get; private set; }

        public GameAnalytics Analytics { get; private set; }

        public IRemoteConfigService RemoteConfig { get; private set; }

        public ICrashReporter CrashReporter { get; private set; }

        public AppSettingsService Settings { get; private set; }

        public IAdsConsentService AdsConsent { get; private set; }

        public PrivacyPolicyConfig PrivacyPolicy => privacyPolicyConfig;

        private bool sessionTrackingActive;

        public static AppRoot EnsureCreated()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindAnyObjectByType<AppRoot>();
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }

            var rootObject = new GameObject("AppRoot");
            var appRoot = rootObject.AddComponent<AppRoot>();
            appRoot.Initialize();
            return appRoot;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AppRoot] Duplicate AppRoot detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<PortraitOrientationLocker>() == null)
            {
                gameObject.AddComponent<PortraitOrientationLocker>();
            }

            EnsureManagers();
            LoadSaveIntoSession();
            BeginSessionTracking();
        }

        private void BeginSessionTracking()
        {
            if (sessionTrackingActive || Analytics == null)
            {
                return;
            }

            Analytics.SessionStarted();
            sessionTrackingActive = true;
        }

        private void EndSessionTracking()
        {
            if (!sessionTrackingActive || Analytics == null)
            {
                return;
            }

            Analytics.SessionEnded();
            sessionTrackingActive = false;
        }

        private void EnsureManagers()
        {
            if (sceneLoader == null)
            {
                sceneLoader = GetComponentInChildren<SceneLoader>(true);
            }

            if (sceneLoader == null)
            {
                var loaderObject = new GameObject("SceneLoader");
                loaderObject.transform.SetParent(transform, false);
                sceneLoader = loaderObject.AddComponent<SceneLoader>();
            }

            if (Session == null)
            {
                Session = new GameSession();
            }

            if (SaveRepository == null)
            {
                SaveRepository = new SaveRepository(new JsonFileSaveService());
            }

            if (Settings == null)
            {
                Settings = new AppSettingsService();
            }

            if (AdsConsent == null)
            {
                AdsConsent = new LocalAdsConsentService(Settings);
            }

            var composed = SdkComposition.Create(this, sdkConfig);
            RemoteConfig ??= composed.RemoteConfig;
            CrashReporter ??= composed.CrashReporter;
            CrashReporter.Initialize();

            if (AnalyticsService == null)
            {
                AnalyticsService = composed.Analytics;
            }

            if (Analytics == null)
            {
                Analytics = new GameAnalytics(AnalyticsService);
            }

            if (AdService == null)
            {
                AdService = composed.Ads;
            }

            if (AdQuota == null)
            {
                var cooldown = SdkComposition.ResolveRewardedCooldown(RemoteConfig, composed.Config);
                AdQuota = new AdQuotaTracker(cooldownSeconds: cooldown);
            }

            if (RewardedAds == null)
            {
                RewardedAds = new RewardedAdGateway(AdService, AdQuota, Analytics);
            }

            if (InterstitialAds == null)
            {
                var everyN = RemoteConfig.GetInt(
                    RemoteConfigKeys.InterstitialEveryNRuns,
                    composed.Config.InterstitialEveryNRuns);
                InterstitialAds = new InterstitialAdGateway(AdService, AdQuota, everyN);
            }

            RemoteConfig.FetchAndActivate(ok =>
            {
                SdkComposition.ApplyRemoteConfigToAds(RemoteConfig, InterstitialAds);
                CrashReporter?.SetCustomKey("remote_config_ok", ok.ToString());
            });
        }

        private void LoadSaveIntoSession()
        {
            var save = SaveRepository.LoadOrCreate();
            Session.ApplyLoadedSave(save);
        }

        public void PersistSession(bool includeActiveRun, RunSaveData runOverride = null)
        {
            if (Session == null || SaveRepository == null)
            {
                return;
            }

            var save = Session.CachedSave ?? SaveRepository.CreateDefault();
            save.meta = SaveMapper.CaptureMeta(Session.Meta);

            if (includeActiveRun && runOverride != null)
            {
                save.run = runOverride;
            }
            else if (!includeActiveRun)
            {
                save.run = new RunSaveData();
            }

            SaveRepository.Save(save);
            Session.CachedSave = save;
        }

        public void ClearActiveRunAndSave()
        {
            if (Session == null || SaveRepository == null)
            {
                return;
            }

            var save = Session.CachedSave ?? SaveRepository.CreateDefault();
            save.meta = SaveMapper.CaptureMeta(Session.Meta);
            SaveRepository.ClearRunAndSave(save);
            Session.CachedSave = save;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 활성 회차 스냅샷은 GamePlayPresenter가 최신 run을 넣도록 이벤트로 요청할 수 있으나,
                // 여기선 메타만이라도 보존한다. 상세 run은 Presenter OnDisable에서 저장한다.
                PersistSession(includeActiveRun: Session != null && Session.HasActiveRun,
                    runOverride: Session?.CachedSave?.run);
                EndSessionTracking();
            }
            else
            {
                BeginSessionTracking();
            }
        }

        private void OnApplicationQuit()
        {
            PersistSession(includeActiveRun: Session != null && Session.HasActiveRun,
                runOverride: Session?.CachedSave?.run);
            EndSessionTracking();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                EndSessionTracking();
                if (CrashReporter is DebugCrashReporter debugCrash)
                {
                    debugCrash.Dispose();
                }
                else if (CrashReporter is FirebaseCrashReporter)
                {
                    // Firebase wrapper owns Debug fallback; best-effort dispose not required.
                }

                Instance = null;
            }
        }

        public void BindSdkConfig(SdkIntegrationConfig config)
        {
            sdkConfig = config;
        }

        public void BindPrivacyPolicy(PrivacyPolicyConfig config)
        {
            privacyPolicyConfig = config;
        }

        /// <summary>
        /// 회차/메타 세이브 파일을 삭제하고 세션을 기본값으로 되돌린다.
        /// </summary>
        public void ResetAllSaveData()
        {
            if (SaveRepository == null)
            {
                SaveRepository = new SaveRepository(new JsonFileSaveService());
            }

            if (SaveRepository != null)
            {
                // JsonFileSaveService Delete via re-create empty
                var fileService = new JsonFileSaveService();
                fileService.Delete();
            }

            Session = new GameSession();
            Session.ApplyLoadedSave(SaveRepository.CreateDefault());
            Settings?.ResetToDefaultsKeepingConsent(keepConsent: true);
            AdQuota?.BeginRun();
            PersistSession(includeActiveRun: false);
            Debug.Log("[AppRoot] All save data reset.");
        }
    }
}
