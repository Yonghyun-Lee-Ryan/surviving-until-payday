using System;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Events
{
    /// <summary>
    /// EventCondition과 GameState 매칭.
    /// </summary>
    public static class EventConditionEvaluator
    {
        public static bool Matches(EventCondition condition, GameState state, bool isWeekend)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (condition == null)
            {
                return true;
            }

            var stats = state.Stats;

            if (stats.Health < condition.MinHealth || stats.Health > condition.MaxHealth)
            {
                return false;
            }

            if (stats.Stress < condition.MinStress || stats.Stress > condition.MaxStress)
            {
                return false;
            }

            if (stats.Happiness < condition.MinHappiness || stats.Happiness > condition.MaxHappiness)
            {
                return false;
            }

            if (stats.CompanyScore < condition.MinCompanyScore || stats.CompanyScore > condition.MaxCompanyScore)
            {
                return false;
            }

            if (condition.UseMinCash && stats.Cash < condition.MinCash)
            {
                return false;
            }

            if (condition.UseMaxCash && stats.Cash > condition.MaxCash)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(condition.RequiredJobId)
                && !string.Equals(condition.RequiredJobId, state.JobId, StringComparison.Ordinal))
            {
                return false;
            }

            switch (condition.DayOfWeekConstraint)
            {
                case DayOfWeekConstraint.WeekdayOnly when isWeekend:
                    return false;
                case DayOfWeekConstraint.WeekendOnly when !isWeekend:
                    return false;
            }

            if (condition.RequiredFlags != null)
            {
                for (var i = 0; i < condition.RequiredFlags.Count; i++)
                {
                    var flag = condition.RequiredFlags[i];
                    if (string.IsNullOrWhiteSpace(flag))
                    {
                        continue;
                    }

                    if (!state.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            if (condition.ForbiddenFlags != null)
            {
                for (var i = 0; i < condition.ForbiddenFlags.Count; i++)
                {
                    var flag = condition.ForbiddenFlags[i];
                    if (string.IsNullOrWhiteSpace(flag))
                    {
                        continue;
                    }

                    if (state.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool MatchesDayRange(EventData eventData, int day)
        {
            if (eventData == null)
            {
                return false;
            }

            return day >= eventData.MinDay && day <= eventData.MaxDay;
        }
    }
}
