using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Analytics;
using SurviveUntilPayday.Services;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class SdkIntegrationTests
    {
        [Test]
        public void LocalRemoteConfig_OverridesInterstitialFrequency()
        {
            var config = ScriptableObject.CreateInstance<SdkIntegrationConfig>();
            config.EditorSet(false, everyN: 5, cooldown: 1f, true, true, true);
            var remote = new LocalRemoteConfigService(config);

            Assert.AreEqual(5, remote.GetInt(RemoteConfigKeys.InterstitialEveryNRuns, 3));
            Assert.AreEqual(1f, remote.GetFloat(RemoteConfigKeys.RewardedCooldownSeconds, 2f), 0.001f);
        }

        [Test]
        public void InterstitialGateway_RespectsRemoteEveryN()
        {
            var mock = new MockAdService();
            var quota = new AdQuotaTracker(new ManualAdClock { UtcSeconds = 1 }, 0);
            var interstitial = new InterstitialAdGateway(mock, quota, showEveryNRuns: 3);
            interstitial.SetShowEveryNRuns(2);

            for (var i = 0; i < 3; i++)
            {
                interstitial.NotifyRunCompleted();
            }

            // completed=4 is first eligible after skip 3; with everyN=2: indexAfterSkip=1 -> show
            interstitial.NotifyRunCompleted();
            Assert.IsTrue(interstitial.ShouldShowOnReturnToMenu(out _));

            interstitial.NotifyRunCompleted(); // 5 -> index 2 -> (2-1)%2 != 0 skip
            Assert.IsFalse(interstitial.ShouldShowOnReturnToMenu(out _));

            interstitial.NotifyRunCompleted(); // 6 -> index 3 -> show
            Assert.IsTrue(interstitial.ShouldShowOnReturnToMenu(out _));
        }

        [Test]
        public void AdMobFallback_DoesNotThrow_AndGrantsViaFallback()
        {
            var mock = new MockAdService();
            var admob = new AdMobAdService(mock);
            AdShowResult? result = null;
            admob.ShowRewardedAd(RewardedAdPlacement.ChoiceReroll, r => result = r);
            Assert.IsTrue(result.HasValue && result.Value.IsSuccess);
            Assert.AreEqual(1, mock.RewardedShowCount);
        }

        [Test]
        public void FirebaseAnalytics_FallsBackToDebug()
        {
            var debug = new DebugAnalyticsService();
            var firebase = new FirebaseAnalyticsService(debug);
            firebase.LogEvent(AnalyticsEventNames.RunStarted, null);
            Assert.AreEqual(1, debug.EventCount);
        }

        [Test]
        public void DebugCrashReporter_CountsExceptions()
        {
            var crash = new DebugCrashReporter();
            crash.Initialize();
            crash.RecordException(new System.InvalidOperationException("sample"));
            Assert.AreEqual(1, crash.ExceptionCount);
            crash.Dispose();
        }

        [Test]
        public void SdkComposition_ApplyRemoteConfig_UpdatesGateway()
        {
            var remote = new LocalRemoteConfigService();
            remote.SetOverride(RemoteConfigKeys.InterstitialEveryNRuns, "4");
            var gateway = new InterstitialAdGateway(
                new MockAdService(),
                new AdQuotaTracker(new ManualAdClock { UtcSeconds = 1 }, 0),
                3);

            SdkComposition.ApplyRemoteConfigToAds(remote, gateway);
            Assert.AreEqual(4, gateway.ShowEveryNRuns);
        }
    }
}
