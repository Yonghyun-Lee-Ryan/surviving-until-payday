using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Analytics;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Tests
{
    public sealed class AnalyticsTests
    {
        [Test]
        public void DebugAnalytics_LogsSnakeCaseEventsToHistory()
        {
            var debug = new DebugAnalyticsService();
            debug.LogEvent(
                AnalyticsEventNames.EventShown,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.EventId] = "event_ot",
                    [AnalyticsParams.Day] = 3
                });

            Assert.AreEqual(1, debug.EventCount);
            Assert.AreEqual(AnalyticsEventNames.EventShown, debug.History[0].Name);
            Assert.AreEqual("event_ot", debug.History[0].Parameters[AnalyticsParams.EventId]);
            Assert.AreEqual(3, debug.History[0].Parameters[AnalyticsParams.Day]);
        }

        [Test]
        public void GameAnalytics_SessionStarted_EmitsGameStart()
        {
            var debug = new DebugAnalyticsService();
            var analytics = new GameAnalytics(debug, new ManualAdClock { UtcSeconds = 1 });
            analytics.SessionStarted();
            Assert.AreEqual(AnalyticsEventNames.GameStart, debug.History[0].Name);
            Assert.AreEqual("game_start", debug.History[0].Name);
        }

        [Test]
        public void GameAnalytics_RunAndChoice_HaveExpectedParams()
        {
            var debug = new DebugAnalyticsService();
            var analytics = new GameAnalytics(debug, new ManualAdClock { UtcSeconds = 10 });

            analytics.RunStarted("job_a", "trait_b", seed: 7, day: 1, continued: false);
            analytics.DayStarted(1, cash: 900_000L);
            analytics.EventShown("event_ot", day: 1);
            analytics.ChoiceSelected(
                "event_ot",
                choiceIndex: 2,
                day: 1,
                statsBefore: new PlayerStats(1_000_000L, 80, 20, 50, 50),
                statsAfter: new PlayerStats(990_000L, 75, 32, 50, 60));
            analytics.RunFailed(FailureReason.Burnout, daysSurvived: 12, cash: 500L);
            analytics.RunCompleted(12, 500L, isSuccess: false);

            Assert.AreEqual(6, debug.EventCount);
            Assert.AreEqual(AnalyticsEventNames.RunStarted, debug.History[0].Name);
            Assert.AreEqual(AnalyticsEventNames.DayStarted, debug.History[1].Name);
            Assert.AreEqual(2, debug.History[3].Parameters[AnalyticsParams.ChoiceIndex]);
            Assert.AreEqual(1_000_000L, debug.History[3].Parameters[AnalyticsParams.CashBefore]);
            Assert.AreEqual(990_000L, debug.History[3].Parameters[AnalyticsParams.CashAfter]);
            Assert.AreEqual(80, debug.History[3].Parameters[AnalyticsParams.HealthBefore]);
            Assert.AreEqual(75, debug.History[3].Parameters[AnalyticsParams.HealthAfter]);
            Assert.AreEqual(
                FailureReason.Burnout.ToString(),
                debug.History[4].Parameters[AnalyticsParams.FailureReason]);
            Assert.AreEqual(12, debug.History[4].Parameters[AnalyticsParams.DaysSurvived]);
        }

        [Test]
        public void GameAnalytics_Session_TracksDuration()
        {
            var debug = new DebugAnalyticsService();
            var clock = new ManualAdClock { UtcSeconds = 100 };
            var analytics = new GameAnalytics(debug, clock);

            analytics.SessionStarted();
            clock.UtcSeconds = 130.4;
            analytics.SessionEnded();

            Assert.AreEqual(AnalyticsEventNames.GameStart, debug.History[0].Name);
            Assert.AreEqual(AnalyticsEventNames.SessionEnded, debug.History[1].Name);
            Assert.AreEqual(30.4, (double)debug.History[1].Parameters[AnalyticsParams.DurationSeconds], 0.01);
        }

        [Test]
        public void RewardedGateway_EmitsOfferedStartedCompleted_OnlyOnSuccess()
        {
            var debug = new DebugAnalyticsService();
            var analytics = new GameAnalytics(debug, new ManualAdClock { UtcSeconds = 1 });
            var quota = new AdQuotaTracker(new ManualAdClock { UtcSeconds = 1 }, cooldownSeconds: 0);
            quota.BeginRun();
            var gateway = new RewardedAdGateway(new MockAdService(), quota, analytics);

            gateway.Request(RewardedAdPlacement.DoubleExperience, _ => { });

            CollectionAssert.AreEqual(
                new[]
                {
                    AnalyticsEventNames.RewardedAdOffered,
                    AnalyticsEventNames.RewardedAdStarted,
                    AnalyticsEventNames.RewardedAdCompleted
                },
                new[]
                {
                    debug.History[0].Name,
                    debug.History[1].Name,
                    debug.History[2].Name
                });
        }

        [Test]
        public void RewardedGateway_Failure_DoesNotEmitCompleted()
        {
            var debug = new DebugAnalyticsService();
            var analytics = new GameAnalytics(debug, new ManualAdClock { UtcSeconds = 1 });
            var quota = new AdQuotaTracker(new ManualAdClock { UtcSeconds = 1 }, cooldownSeconds: 0);
            quota.BeginRun();
            var mock = new MockAdService();
            mock.SetForceRewardedFailure(true);
            var gateway = new RewardedAdGateway(mock, quota, analytics);

            gateway.Request(RewardedAdPlacement.ChoiceReroll, _ => { });

            Assert.AreEqual(2, debug.EventCount);
            Assert.AreEqual(AnalyticsEventNames.RewardedAdOffered, debug.History[0].Name);
            Assert.AreEqual(AnalyticsEventNames.RewardedAdStarted, debug.History[1].Name);
        }

        [Test]
        public void NoPersonalIdentifiers_InParameterKeys()
        {
            var keys = new[]
            {
                AnalyticsParams.Day,
                AnalyticsParams.EventId,
                AnalyticsParams.ChoiceIndex,
                AnalyticsParams.JobId,
                AnalyticsParams.TraitId,
                AnalyticsParams.Seed,
                AnalyticsParams.FailureReason,
                AnalyticsParams.DaysSurvived,
                AnalyticsParams.Cash,
                AnalyticsParams.CashBefore,
                AnalyticsParams.CashAfter,
                AnalyticsParams.Placement,
                AnalyticsParams.DurationSeconds
            };

            foreach (var key in keys)
            {
                Assert.IsFalse(key.Contains("email"));
                Assert.IsFalse(key.Contains("user_id"));
                Assert.IsFalse(key.Contains("device"));
                Assert.IsFalse(key.Contains("name"));
            }
        }

        [Test]
        public void MirrorPath_DoesNotDoubleLog_WhenFirebaseFallbackIsNull()
        {
            var debug = new DebugAnalyticsService();
            var composite = new SurviveUntilPayday.Services.CompositeAnalyticsService(
                debug,
                new SurviveUntilPayday.Services.FirebaseAnalyticsService(fallback: null));
            composite.LogEvent(AnalyticsEventNames.GameStart, null);
            Assert.AreEqual(1, debug.EventCount);
        }
    }
}
