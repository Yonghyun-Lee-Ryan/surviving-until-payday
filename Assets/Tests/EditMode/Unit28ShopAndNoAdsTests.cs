using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Purchasing;
using SurviveUntilPayday.Save;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit28ShopAndNoAdsTests
    {
        [Test]
        public void MockPurchase_OwnsRemoveInterstitial()
        {
            var purchases = new MockPurchaseService();
            PurchaseResult? result = null;
            purchases.Purchase(PurchaseProductIds.RemoveInterstitial, r => result = r);

            Assert.IsTrue(result.HasValue && result.Value.IsSuccess);
            Assert.IsTrue(purchases.IsOwned(PurchaseProductIds.RemoveInterstitial));
        }

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
        public void TraitFragment_CalendarQuota_ThreePerDay_PersistsAcrossBeginRun()
        {
            var clock = new ManualAdClock { UtcSeconds = 10 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.SyncTraitFragmentCalendar("2026-07-26", "2026-07-26", 0);
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            var granted = 0;
            for (var i = 0; i < 4; i++)
            {
                gateway.Request(RewardedAdPlacement.TraitFragment, r =>
                {
                    if (r.RewardGranted)
                    {
                        granted++;
                    }
                });
                clock.UtcSeconds += 1;
            }

            Assert.AreEqual(3, granted);
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.TraitFragment));

            quota.BeginRun();
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.TraitFragment),
                "회차 시작으로 캘린더 쿼터가 리셋되면 안 된다.");
        }

        [Test]
        public void TraitFragment_ResetsOnNewCalendarDay()
        {
            var quota = new AdQuotaTracker(new ManualAdClock { UtcSeconds = 1 }, cooldownSeconds: 0);
            quota.SyncTraitFragmentCalendar("2026-07-26", "2026-07-26", 3);
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.TraitFragment));

            quota.SyncTraitFragmentCalendar("2026-07-27", "2026-07-26", 3);
            Assert.AreEqual(3, quota.GetRemaining(RewardedAdPlacement.TraitFragment));
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
        public void TraitFragment_IgnoresGlobalCooldown_AllowsConsecutiveWatches()
        {
            var clock = new ManualAdClock { UtcSeconds = 10 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 5);
            quota.SyncTraitFragmentCalendar("2026-07-26", "2026-07-26", 0);
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            var granted = 0;
            for (var i = 0; i < 3; i++)
            {
                gateway.Request(RewardedAdPlacement.TraitFragment, r =>
                {
                    if (r.RewardGranted)
                    {
                        granted++;
                    }
                });
                // 쿨다운을 기다리지 않고 바로 다음 요청
            }

            Assert.AreEqual(3, granted);
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.TraitFragment));
        }

        [Test]
        public void NonTraitPlacement_CooldownAppliesOnlyToSamePlacement()
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
