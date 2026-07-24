using System;
using System.Collections.Generic;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Analytics
{
    /// <summary>
    /// 게임 로직이 호출하는 분석 파사드. 파라미터 조립을 한곳에 모은다.
    /// </summary>
    public sealed class GameAnalytics : IAdTelemetry
    {
        private readonly IAnalyticsService service;
        private readonly IAdClock clock;
        private double sessionStartUtcSeconds;
        private bool sessionOpen;

        public GameAnalytics(IAnalyticsService service, IAdClock clock = null)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.clock = clock ?? new SystemAdClock();
        }

        public IAnalyticsService Service => service;

        public void SessionStarted()
        {
            sessionStartUtcSeconds = clock.UtcSeconds;
            sessionOpen = true;
            Log(AnalyticsEventNames.GameStart, null);
        }

        public void SessionEnded()
        {
            if (!sessionOpen)
            {
                return;
            }

            var duration = Math.Max(0, clock.UtcSeconds - sessionStartUtcSeconds);
            sessionOpen = false;
            Log(
                AnalyticsEventNames.SessionEnded,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.DurationSeconds] = Math.Round(duration, 1)
                });
        }

        public void RunStarted(string jobId, string traitId, int seed, int day, bool continued)
        {
            Log(
                AnalyticsEventNames.RunStarted,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.JobId] = SanitizeId(jobId),
                    [AnalyticsParams.TraitId] = SanitizeId(traitId),
                    [AnalyticsParams.Seed] = seed,
                    [AnalyticsParams.Day] = day,
                    [AnalyticsParams.Continued] = continued
                });
        }

        public void DayStarted(int day, long cash)
        {
            Log(
                AnalyticsEventNames.DayStarted,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.Day] = day,
                    [AnalyticsParams.Cash] = cash
                });
        }

        public void EventShown(string eventId, int day)
        {
            Log(
                AnalyticsEventNames.EventShown,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.EventId] = SanitizeId(eventId),
                    [AnalyticsParams.Day] = day
                });
        }

        public void ChoiceSelected(
            string eventId,
            int choiceIndex,
            int day,
            PlayerStats statsBefore,
            PlayerStats statsAfter)
        {
            var before = statsBefore ?? new PlayerStats();
            var after = statsAfter ?? before;
            Log(
                AnalyticsEventNames.ChoiceSelected,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.EventId] = SanitizeId(eventId),
                    [AnalyticsParams.ChoiceIndex] = choiceIndex,
                    [AnalyticsParams.Day] = day,
                    [AnalyticsParams.CashBefore] = before.Cash,
                    [AnalyticsParams.CashAfter] = after.Cash,
                    [AnalyticsParams.HealthBefore] = before.Health,
                    [AnalyticsParams.HealthAfter] = after.Health,
                    [AnalyticsParams.StressBefore] = before.Stress,
                    [AnalyticsParams.StressAfter] = after.Stress,
                    [AnalyticsParams.HappinessBefore] = before.Happiness,
                    [AnalyticsParams.HappinessAfter] = after.Happiness,
                    [AnalyticsParams.CompanyBefore] = before.CompanyScore,
                    [AnalyticsParams.CompanyAfter] = after.CompanyScore,
                    // 하위 호환: 단일 cash는 선택 후 잔액
                    [AnalyticsParams.Cash] = after.Cash
                });
        }

        public void RunFailed(FailureReason reason, int daysSurvived, long cash)
        {
            Log(
                AnalyticsEventNames.RunFailed,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.FailureReason] = reason.ToString(),
                    [AnalyticsParams.DaysSurvived] = daysSurvived,
                    [AnalyticsParams.Cash] = cash,
                    [AnalyticsParams.IsSuccess] = false
                });
        }

        public void RunCompleted(int daysSurvived, long cash, bool isSuccess)
        {
            Log(
                AnalyticsEventNames.RunCompleted,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.DaysSurvived] = daysSurvived,
                    [AnalyticsParams.Cash] = cash,
                    [AnalyticsParams.IsSuccess] = isSuccess
                });
        }

        public void OnRewardedOffered(RewardedAdPlacement placement)
        {
            LogAd(AnalyticsEventNames.RewardedAdOffered, placement);
        }

        public void OnRewardedStarted(RewardedAdPlacement placement)
        {
            LogAd(AnalyticsEventNames.RewardedAdStarted, placement);
        }

        public void OnRewardedCompleted(RewardedAdPlacement placement)
        {
            LogAd(AnalyticsEventNames.RewardedAdCompleted, placement);
        }

        private void LogAd(string eventName, RewardedAdPlacement placement)
        {
            Log(
                eventName,
                new Dictionary<string, object>
                {
                    [AnalyticsParams.Placement] = placement.ToString()
                });
        }

        private void Log(string eventName, Dictionary<string, object> parameters)
        {
            service.LogEvent(eventName, parameters);
        }

        private static string SanitizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
