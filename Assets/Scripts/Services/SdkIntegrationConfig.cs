using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// SDK 연동 기본값. Firebase/AdMob 패키지 없이도 Inspector에서 빈도를 바꿀 수 있다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SdkIntegrationConfig",
        menuName = "Survive Until Payday/Config/SDK Integration",
        order = 100)]
    public sealed class SdkIntegrationConfig : ScriptableObject
    {
        public const string GoogleTestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string GoogleTestRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
        public const string GoogleTestInterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";

        [Header("Ads")]
        [SerializeField] private bool preferRealAds;
        [SerializeField] [Min(1)] private int interstitialEveryNRuns = 3;
        [SerializeField] [Min(0f)] private float rewardedCooldownSeconds = 2f;
        [SerializeField] private bool useTestAdsInEditor = true;
        [SerializeField] private bool allowRealAdsInEditor;
        [SerializeField] private bool useGoogleTestAdUnits = true;
        [SerializeField] private string androidAdMobAppId = GoogleTestAppId;
        [SerializeField] private string rewardedAdUnitId = GoogleTestRewardedUnitId;
        [SerializeField] private string interstitialAdUnitId = GoogleTestInterstitialUnitId;
        [SerializeField] private string[] testDeviceHashedIds;
        [SerializeField] private bool umpDebugForceEea;

        [Header("Analytics / Crash")]
        [SerializeField] private bool mirrorEventsToDebugConsole = true;
        [SerializeField] private bool enableCrashCapture = true;

        public bool PreferRealAds => preferRealAds;
        public int InterstitialEveryNRuns => Mathf.Max(1, interstitialEveryNRuns);
        public float RewardedCooldownSeconds => Mathf.Max(0f, rewardedCooldownSeconds);
        public bool UseTestAdsInEditor => useTestAdsInEditor;
        public bool AllowRealAdsInEditor => allowRealAdsInEditor;
        public bool UseGoogleTestAdUnits => useGoogleTestAdUnits;
        public string AndroidAdMobAppId => string.IsNullOrWhiteSpace(androidAdMobAppId)
            ? GoogleTestAppId
            : androidAdMobAppId.Trim();
        public string RewardedAdUnitId => ResolveUnitId(rewardedAdUnitId, GoogleTestRewardedUnitId);
        public string InterstitialAdUnitId => ResolveUnitId(interstitialAdUnitId, GoogleTestInterstitialUnitId);
        public string[] TestDeviceHashedIds => testDeviceHashedIds ?? System.Array.Empty<string>();
        public bool UmpDebugForceEea => umpDebugForceEea;
        public bool MirrorEventsToDebugConsole => mirrorEventsToDebugConsole;
        public bool EnableCrashCapture => enableCrashCapture;

#if UNITY_EDITOR
        public void EditorSet(
            bool realAds,
            int everyN,
            float cooldown,
            bool testInEditor,
            bool mirrorDebug,
            bool crashCapture)
        {
            preferRealAds = realAds;
            interstitialEveryNRuns = Mathf.Max(1, everyN);
            rewardedCooldownSeconds = Mathf.Max(0f, cooldown);
            useTestAdsInEditor = testInEditor;
            mirrorEventsToDebugConsole = mirrorDebug;
            enableCrashCapture = crashCapture;
        }

        public void EditorSetAdUnits(
            bool googleTestUnits,
            string appId,
            string rewardedUnit,
            string interstitialUnit,
            string[] testDevices,
            bool umpForceEea,
            bool allowRealInEditor)
        {
            useGoogleTestAdUnits = googleTestUnits;
            androidAdMobAppId = appId;
            rewardedAdUnitId = rewardedUnit;
            interstitialAdUnitId = interstitialUnit;
            testDeviceHashedIds = testDevices ?? System.Array.Empty<string>();
            umpDebugForceEea = umpForceEea;
            allowRealAdsInEditor = allowRealInEditor;
        }
#endif

        private string ResolveUnitId(string configured, string testFallback)
        {
            if (useGoogleTestAdUnits || string.IsNullOrWhiteSpace(configured))
            {
                return testFallback;
            }

            return configured.Trim();
        }
    }
}
