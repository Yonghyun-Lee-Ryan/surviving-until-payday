using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 회차 종료 결과. Scene 간 전달용.
    /// </summary>
    public sealed class ResultData
    {
        public int DaysSurvived { get; }
        public bool IsSuccess { get; }
        public FailureReason FailureReason { get; }
        public PlayerStats FinalStats { get; }
        public EndingData Ending { get; }
        public int ExperienceGained { get; }
        public bool EndingNewlyUnlocked { get; }
        public MetaProgressResult MetaProgress { get; }

        public ResultData(
            int daysSurvived,
            bool isSuccess,
            FailureReason failureReason,
            PlayerStats finalStats,
            EndingData ending,
            int experienceGained,
            bool endingNewlyUnlocked,
            MetaProgressResult metaProgress = null)
        {
            DaysSurvived = daysSurvived;
            IsSuccess = isSuccess;
            FailureReason = failureReason;
            FinalStats = finalStats != null ? finalStats.Clone() : new PlayerStats();
            Ending = ending;
            ExperienceGained = experienceGained;
            EndingNewlyUnlocked = endingNewlyUnlocked;
            MetaProgress = metaProgress;
        }

        public static ResultData Create(
            GameState state,
            bool isSuccess,
            FailureReason failureReason,
            EndingData ending)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new ResultData(
                state.CurrentDay,
                isSuccess,
                failureReason,
                state.Stats,
                ending,
                experienceGained: 0,
                endingNewlyUnlocked: false);
        }

        public ResultData WithMeta(MetaProgressResult meta)
        {
            if (meta == null)
            {
                return this;
            }

            return new ResultData(
                DaysSurvived,
                IsSuccess,
                FailureReason,
                FinalStats,
                Ending,
                meta.ExperienceGained,
                meta.NewlyUnlockedEndings.Count > 0,
                meta);
        }
    }

    public static class ExperienceCalculator
    {
        public const int NewEndingBonus = 50;

        public static int Calculate(
            int daysSurvived,
            bool isSuccess,
            PlayerStats stats,
            bool newEndingUnlocked = false)
        {
            var xp = Math.Max(0, daysSurvived) * 10;
            if (isSuccess)
            {
                xp += 100;
            }

            if (stats != null)
            {
                if (stats.Cash > 0)
                {
                    xp += (int)Math.Min(stats.Cash / 100_000L, 50L);
                }

                if (stats.CompanyScore >= 80)
                {
                    xp += 20;
                }
            }

            if (newEndingUnlocked)
            {
                xp += NewEndingBonus;
            }

            return xp;
        }
    }
}
