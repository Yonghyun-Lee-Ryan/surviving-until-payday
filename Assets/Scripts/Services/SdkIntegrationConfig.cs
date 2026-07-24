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
        [Header("Ads")]
        [SerializeField] private bool preferRealAds;
        [SerializeField] [Min(1)] private int interstitialEveryNRuns = 3;
        [SerializeField] [Min(0f)] private float rewardedCooldownSeconds = 2f;
        [SerializeField] private bool useTestAdsInEditor = true;

        [Header("Analytics / Crash")]
        [SerializeField] private bool mirrorEventsToDebugConsole = true;
        [SerializeField] private bool enableCrashCapture = true;

        public bool PreferRealAds => preferRealAds;
        public int InterstitialEveryNRuns => Mathf.Max(1, interstitialEveryNRuns);
        public float RewardedCooldownSeconds => Mathf.Max(0f, rewardedCooldownSeconds);
        public bool UseTestAdsInEditor => useTestAdsInEditor;
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
#endif
    }
}
