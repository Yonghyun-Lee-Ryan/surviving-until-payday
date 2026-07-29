using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 실패 조건 판정. 우선순위: 파산 → 입원 → 번아웃 → 해고.
    /// </summary>
    public static class FailureEvaluator
    {
        public static FailureReason Evaluate(GameState state)
        {
            if (state == null)
            {
                return FailureReason.None;
            }

            return Evaluate(state.Stats);
        }

        public static FailureReason Evaluate(PlayerStats stats)
        {
            if (stats == null)
            {
                return FailureReason.None;
            }

            if (stats.Cash < 0)
            {
                return FailureReason.Bankruptcy;
            }

            if (stats.Health <= StatLimits.MinGauge)
            {
                return FailureReason.Hospitalization;
            }

            if (stats.Stress >= StatLimits.MaxGauge)
            {
                return FailureReason.Burnout;
            }

            if (stats.CompanyScore <= StatLimits.MinGauge)
            {
                return FailureReason.Fired;
            }

            return FailureReason.None;
        }

        public static List<FailureReason> GetAll(PlayerStats stats)
        {
            var reasons = new List<FailureReason>(4);
            if (stats == null)
            {
                return reasons;
            }

            if (stats.Cash < 0)
            {
                reasons.Add(FailureReason.Bankruptcy);
            }

            if (stats.Health <= StatLimits.MinGauge)
            {
                reasons.Add(FailureReason.Hospitalization);
            }

            if (stats.Stress >= StatLimits.MaxGauge)
            {
                reasons.Add(FailureReason.Burnout);
            }

            if (stats.CompanyScore <= StatLimits.MinGauge)
            {
                reasons.Add(FailureReason.Fired);
            }

            return reasons;
        }

        public static string ToDisplayName(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.Bankruptcy:
                    return "파산";
                case FailureReason.Hospitalization:
                    return "병원 입원";
                case FailureReason.Burnout:
                    return "번아웃";
                case FailureReason.Fired:
                    return "해고";
                default:
                    return "없음";
            }
        }

        /// <summary>실패명 + 조사(으로/로) 결합. 받침 유무에 맞춤.</summary>
        public static string ToDisplayPhraseEnded(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.Bankruptcy:
                    return "파산으로";
                case FailureReason.Hospitalization:
                    return "병원 입원으로";
                case FailureReason.Burnout:
                    return "번아웃으로";
                case FailureReason.Fired:
                    return "해고로";
                default:
                    return "실패로";
            }
        }
    }
}
