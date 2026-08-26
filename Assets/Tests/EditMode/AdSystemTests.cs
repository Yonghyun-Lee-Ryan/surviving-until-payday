using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Tests
{
    public sealed class AdSystemTests
    {
        [Test]
        public void MockAdService_CompletesImmediately()
        {
            var mock = new MockAdService();
            AdShowResult? rewarded = null;
            AdShowResult? interstitial = null;

            mock.ShowRewardedAd(RewardedAdPlacement.ChoiceReroll, r => rewarded = r);
            mock.ShowInterstitial(r => interstitial = r);

            Assert.IsTrue(rewarded.HasValue && rewarded.Value.IsSuccess);
            Assert.IsTrue(interstitial.HasValue && interstitial.Value.IsSuccess);
            Assert.AreEqual(1, mock.RewardedShowCount);
            Assert.AreEqual(1, mock.InterstitialShowCount);
        }

        [Test]
        public void Gateway_Failure_DoesNotGrantRewardOrConsumeQuota()
        {
            var clock = new ManualAdClock { UtcSeconds = 100 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var mock = new MockAdService();
            mock.SetForceRewardedFailure(true, "load failed");
            var gateway = new RewardedAdGateway(mock, quota);

            AdRewardRequestResult? result = null;
            gateway.Request(RewardedAdPlacement.EmergencyLoan, r => result = r);

            Assert.IsTrue(result.HasValue);
            Assert.IsFalse(result.Value.RewardGranted);
            Assert.AreEqual(AdShowStatus.Failed, result.Value.ShowResult.Status);
            Assert.AreEqual(1, quota.GetRemaining(RewardedAdPlacement.EmergencyLoan));
        }

        [Test]
        public void Gateway_Success_GrantsRewardOnce_ThenQuotaBlocks()
        {
            var clock = new ManualAdClock { UtcSeconds = 10 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            AdRewardRequestResult? first = null;
            gateway.Request(RewardedAdPlacement.RetryOutcome, r => first = r);
            Assert.IsTrue(first.Value.RewardGranted);
            Assert.IsTrue(first.Value.Reward.Value.RetryOutcome);

            AdRewardRequestResult? second = null;
            gateway.Request(RewardedAdPlacement.RetryOutcome, r => second = r);
            Assert.IsFalse(second.Value.RewardGranted);
            Assert.AreEqual(AdShowStatus.QuotaExceeded, second.Value.ShowResult.Status);
        }

        [Test]
        public void ChoiceReroll_AllowsTwoPerRun()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            var granted = 0;
            for (var i = 0; i < 3; i++)
            {
                gateway.Request(RewardedAdPlacement.ChoiceReroll, r =>
                {
                    if (r.RewardGranted)
                    {
                        granted++;
                    }
                });
                clock.UtcSeconds += 1;
            }

            Assert.AreEqual(2, granted);
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.ChoiceReroll));
        }

        [Test]
        public void Cooldown_BlocksSamePlacementRapidRequests()
        {
            var clock = new ManualAdClock { UtcSeconds = 50 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 5);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            gateway.Request(RewardedAdPlacement.ChoiceReroll, _ => { });
            AdRewardRequestResult? blocked = null;
            gateway.Request(RewardedAdPlacement.ChoiceReroll, r => blocked = r);

            Assert.AreEqual(AdShowStatus.OnCooldown, blocked.Value.ShowResult.Status);
            Assert.IsFalse(blocked.Value.RewardGranted);

            clock.UtcSeconds += 5;
            AdRewardRequestResult? ok = null;
            gateway.Request(RewardedAdPlacement.ChoiceReroll, r => ok = r);
            Assert.IsTrue(ok.Value.RewardGranted);
        }

        [Test]
        public void Cooldown_DoesNotBlockDifferentPlacement()
        {
            var clock = new ManualAdClock { UtcSeconds = 50 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 5);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            gateway.Request(RewardedAdPlacement.DailySideJob, _ => { });
            AdRewardRequestResult? retry = null;
            gateway.Request(RewardedAdPlacement.RetryOutcome, r => retry = r);

            Assert.IsTrue(retry.Value.RewardGranted);
        }

        [Test]
        public void DailySideJob_ResetsOnNewGameDay()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota);

            gateway.Request(RewardedAdPlacement.DailySideJob, _ => { });
            Assert.AreEqual(0, quota.GetRemaining(RewardedAdPlacement.DailySideJob));

            quota.SetGameDay(2);
            Assert.AreEqual(1, quota.GetRemaining(RewardedAdPlacement.DailySideJob));
        }

        [Test]
        public void AdRewardApplicator_AddsCashOnlyOnGrant()
        {
            var state = new GameState { CurrentDay = 1 };
            state.Stats.Cash = 100_000L;
            var reward = AdRewardGrant.ForPlacement(RewardedAdPlacement.EmergencyLoan);

            AdRewardApplicator.ApplyCash(state, reward);
            Assert.AreEqual(200_000L, state.Stats.Cash);
        }

        [Test]
        public void Interstitial_SkipsFirstThreeRuns_ThenShows()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            var mock = new MockAdService();
            var interstitial = new InterstitialAdGateway(mock, quota, showEveryNRuns: 3);

            for (var i = 0; i < 3; i++)
            {
                interstitial.NotifyRunCompleted();
                Assert.IsFalse(interstitial.ShouldShowOnReturnToMenu(out _));
            }

            interstitial.NotifyRunCompleted(); // 4th completed run
            Assert.IsTrue(interstitial.ShouldShowOnReturnToMenu(out _));

            var results = new List<AdShowResult>();
            interstitial.TryShowOnReturnToMenu(r => results.Add(r));
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].IsSuccess);
            Assert.AreEqual(1, mock.InterstitialShowCount);
        }

        [Test]
        public void Interstitial_SetRemoveInterstitials_SkipsShow()
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
            Assert.IsTrue(reason.Contains("disabled"));
        }

        [Test]
        public void Interstitial_SkipsAfterRewardedAd()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var mock = new MockAdService();
            var rewarded = new RewardedAdGateway(mock, quota);
            var interstitial = new InterstitialAdGateway(mock, quota, showEveryNRuns: 1);

            for (var i = 0; i < 4; i++)
            {
                interstitial.NotifyRunCompleted();
            }

            rewarded.Request(RewardedAdPlacement.DoubleExperience, _ => { });
            Assert.IsFalse(interstitial.ShouldShowOnReturnToMenu(out var reason));
            Assert.IsTrue(reason.Contains("rewarded"));
        }

        [Test]
        public void NotReady_DoesNotBlockAndGivesNoReward()
        {
            var clock = new ManualAdClock { UtcSeconds = 1 };
            var quota = new AdQuotaTracker(clock, cooldownSeconds: 0);
            quota.BeginRun();
            var mock = new MockAdService();
            mock.SetRewardedReady(false);
            var gateway = new RewardedAdGateway(mock, quota);

            AdRewardRequestResult? result = null;
            gateway.Request(RewardedAdPlacement.ChoiceReroll, r => result = r);

            Assert.AreEqual(AdShowStatus.NotReady, result.Value.ShowResult.Status);
            Assert.IsFalse(result.Value.RewardGranted);
            Assert.AreEqual(2, quota.GetRemaining(RewardedAdPlacement.ChoiceReroll));
            Assert.AreEqual(0, mock.RewardedShowCount);
        }

        [Test]
        public void AdRewardApplicator_AppliesCashOnlyWhenGranted()
        {
            var state = new GameState { CurrentDay = 1, JobId = "job" };
            state.Stats.Cash = 50_000L;
            var grant = AdRewardGrant.ForPlacement(RewardedAdPlacement.EmergencyLoan);
            AdRewardApplicator.ApplyCash(state, grant);
            Assert.AreEqual(150_000L, state.Stats.Cash);
        }
    }
}
