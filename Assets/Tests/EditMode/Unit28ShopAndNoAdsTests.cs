using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Save;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit28ShopAndNoAdsTests
    {
        [Test]
        public void Interstitial_SkippedWhenNoAdsOwned()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            var interstitial = new InterstitialAdGateway(new MockAdService(), quota, showEveryNRuns: 1);

            for (var i = 0; i < 4; i++)
            {
                interstitial.NotifyRunCompleted();
            }

            Assert.IsTrue(interstitial.ShouldShowOnReturnToMenu(out _));

            interstitial.SetRemoveInterstitials(true);
            Assert.IsFalse(interstitial.ShouldShowOnReturnToMenu(out var reason));
            Assert.IsTrue(reason.Contains("removed"));
        }

        [Test]
        public void MetaSave_PersistsHasNoAds()
        {
            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null, null, 0, false, hasNoAds: true);
            Assert.IsTrue(meta.HasNoAds);

            var captured = SaveMapper.CaptureMeta(meta);
            Assert.IsTrue(captured.hasNoAds);

            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(captured, loaded);
            Assert.IsTrue(loaded.HasNoAds);
        }

        [Test]
        public void Cooldown_AppliesOnlyToSamePlacement()
        {
            var clock = new ManualAdClock { UtcSeconds = 50 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 5);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            gateway.Request(RewardedAdPlacement.ChoiceReroll, _ => { });
            AdRewardRequestResult? sideJob = null;
            gateway.Request(RewardedAdPlacement.DailySideJob, r => sideJob = r);

            Assert.IsTrue(sideJob.Value.RewardGranted);

            AdRewardRequestResult? blocked = null;
            gateway.Request(RewardedAdPlacement.ChoiceReroll, r => blocked = r);
            Assert.AreEqual(AdShowStatus.OnCooldown, blocked.Value.ShowResult.Status);
            Assert.IsFalse(blocked.Value.RewardGranted);
        }
    }
}
