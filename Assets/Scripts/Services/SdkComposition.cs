using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Analytics;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// AppRoot가 쓰는 SDK 조합을 한곳에서 만든다.
    /// </summary>
    public static class SdkComposition
    {
        public readonly struct Result
        {
            public IAdService Ads { get; }
            public IAnalyticsService Analytics { get; }
            public IRemoteConfigService RemoteConfig { get; }
            public ICrashReporter CrashReporter { get; }
            public SdkIntegrationConfig Config { get; }

            public Result(
                IAdService ads,
                IAnalyticsService analytics,
                IRemoteConfigService remoteConfig,
                ICrashReporter crashReporter,
                SdkIntegrationConfig config)
            {
                Ads = ads;
                Analytics = analytics;
                RemoteConfig = remoteConfig;
                CrashReporter = crashReporter;
                Config = config;
            }
        }

        public static Result Create(MonoBehaviour host, SdkIntegrationConfig config)
        {
            config ??= ScriptableObject.CreateInstance<SdkIntegrationConfig>();
            var remote = new LocalRemoteConfigService(config);

            IAdService adsFallback =
                Application.isEditor && config.UseTestAdsInEditor && !config.AllowRealAdsInEditor
                    ? new TestDeviceAdService(host)
                    : (IAdService)new MockAdService();

            var preferReal = remote.GetBool(RemoteConfigKeys.UseRealAds, config.PreferRealAds);
            var useRealWrapper = Application.isEditor
                ? preferReal && config.AllowRealAdsInEditor
                : preferReal || SdkDefines.HasGoogleMobileAds;
            IAdService ads = useRealWrapper
                ? new AdMobAdService(adsFallback, config)
                : adsFallback;

            // Firebase 심볼이 없어도 Debug로 폴백되어 Console에서 이벤트를 확인한다.
            // MirrorEventsToDebugConsole이 켜져 있으면 Debug를 한 번만 붙인다
            // (Firebase 폴백 Debug + Mirror Debug가 이중 로그 나지 않게).
            var debugAnalytics = new DebugAnalyticsService();
            IAnalyticsService analytics;
            if (config.MirrorEventsToDebugConsole)
            {
                analytics = new CompositeAnalyticsService(
                    debugAnalytics,
                    new FirebaseAnalyticsService(fallback: null));
            }
            else
            {
                analytics = new FirebaseAnalyticsService(debugAnalytics);
            }

            ICrashReporter crash = config.EnableCrashCapture
                ? new FirebaseCrashReporter(new DebugCrashReporter())
                : new DebugCrashReporter();

            return new Result(ads, analytics, remote, crash, config);
        }

        public static void ApplyRemoteConfigToAds(
            IRemoteConfigService remote,
            InterstitialAdGateway interstitial)
        {
            if (remote == null || interstitial == null)
            {
                return;
            }

            var everyN = remote.GetInt(
                RemoteConfigKeys.InterstitialEveryNRuns,
                InterstitialAdGateway.DefaultShowEveryNRuns);
            interstitial.SetShowEveryNRuns(everyN);
            Debug.Log($"[SdkComposition] interstitial_every_n_runs={everyN}");
        }

        public static float ResolveRewardedCooldown(IRemoteConfigService remote, SdkIntegrationConfig config)
        {
            var fallback = config != null
                ? config.RewardedCooldownSeconds
                : (float)AdQuotaTracker.DefaultCooldownSeconds;
            return remote != null
                ? remote.GetFloat(RemoteConfigKeys.RewardedCooldownSeconds, fallback)
                : fallback;
        }
    }
}
