using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Save;
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

        public SceneLoader SceneLoader => sceneLoader;

        public GameSession Session { get; private set; }

        public SaveRepository SaveRepository { get; private set; }

        public IAdService AdService { get; private set; }

        public AdQuotaTracker AdQuota { get; private set; }

        public RewardedAdGateway RewardedAds { get; private set; }

        public InterstitialAdGateway InterstitialAds { get; private set; }

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

            if (AdService == null)
            {
                // 추후 AdMob 등으로 교체. 게임 로직은 IAdService만 참조한다.
                AdService = new MockAdService();
            }

            if (AdQuota == null)
            {
                AdQuota = new AdQuotaTracker();
            }

            if (RewardedAds == null)
            {
                RewardedAds = new RewardedAdGateway(AdService, AdQuota);
            }

            if (InterstitialAds == null)
            {
                InterstitialAds = new InterstitialAdGateway(AdService, AdQuota);
            }
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
            }
        }

        private void OnApplicationQuit()
        {
            PersistSession(includeActiveRun: Session != null && Session.HasActiveRun,
                runOverride: Session?.CachedSave?.run);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
