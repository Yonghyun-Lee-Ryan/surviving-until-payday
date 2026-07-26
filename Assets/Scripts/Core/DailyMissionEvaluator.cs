using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 회차 결과로 일일 미션 달성 여부를 판정한다.
    /// </summary>
    public static class DailyMissionEvaluator
    {
        public static bool IsCompleted(DailyMissionData mission, GameState state, bool isSuccess)
        {
            if (mission == null || state == null)
            {
                return false;
            }

            var stats = state.Stats;
            switch (mission.GoalType)
            {
                case DailyMissionGoalType.SurviveMinDays:
                    return state.CurrentDay >= Math.Max(1, mission.IntThreshold);

                case DailyMissionGoalType.SurviveSuccess:
                    return isSuccess;

                case DailyMissionGoalType.MinCashOnEnd:
                    return stats != null && stats.Cash >= mission.LongThreshold;

                case DailyMissionGoalType.MinCompanyScore:
                    return stats != null && stats.CompanyScore >= mission.IntThreshold;

                case DailyMissionGoalType.MaxStressOnEnd:
                    return stats != null && stats.Stress <= mission.IntThreshold;

                case DailyMissionGoalType.MinHealthOnSuccess:
                    return isSuccess && stats != null && stats.Health >= mission.IntThreshold;

                case DailyMissionGoalType.ForbiddenFlagThroughDays:
                    return state.CurrentDay >= Math.Max(1, mission.IntThreshold)
                           && !HasFlag(state, mission.FlagId);

                case DailyMissionGoalType.MinSideJobCount:
                    return state.SideJobCount >= Math.Max(1, mission.IntThreshold);

                case DailyMissionGoalType.MinHappinessOnEnd:
                    return stats != null && stats.Happiness >= mission.IntThreshold;

                default:
                    return false;
            }
        }

        private static bool HasFlag(GameState state, string flagId)
        {
            if (string.IsNullOrEmpty(flagId) || state.RunFlags == null)
            {
                return false;
            }

            for (var i = 0; i < state.RunFlags.Count; i++)
            {
                if (string.Equals(state.RunFlags[i], flagId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
