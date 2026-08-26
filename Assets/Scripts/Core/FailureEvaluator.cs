using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 실패 조건 판정. 우선순위: 파산 → 입원 → 번아웃 → 해고.
    /// </summary>
    public static class FailureEvaluator
    {
        /// <summary>
        /// 건강 방치 플래그가 있을 때 입원으로 보는 체력 상한 (R-QA-04).
        /// 체력 0까지 깎이기 전에 감기/점심 거르기 경로가 입원으로 이어지게 한다.
        /// </summary>
        public const int NeglectedHealthHospitalMax = 35;

        public static FailureReason Evaluate(GameState state)
        {
            if (state == null)
            {
                return FailureReason.None;
            }

            return EvaluateCore(state.Stats, state);
        }

        public static FailureReason Evaluate(PlayerStats stats)
        {
            return EvaluateCore(stats, null);
        }

        public static List<FailureReason> GetAll(GameState state)
        {
            return GetAllCore(state?.Stats, state);
        }

        public static List<FailureReason> GetAll(PlayerStats stats)
        {
            return GetAllCore(stats, null);
        }

        private static FailureReason EvaluateCore(PlayerStats stats, GameState state)
        {
            if (stats == null)
            {
                return FailureReason.None;
            }

            if (stats.Cash < 0)
            {
                return FailureReason.Bankruptcy;
            }

            if (IsHospitalization(stats, state))
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

        private static List<FailureReason> GetAllCore(PlayerStats stats, GameState state)
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

            if (IsHospitalization(stats, state))
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

        private static bool IsHospitalization(PlayerStats stats, GameState state)
        {
            if (stats.Health <= StatLimits.MinGauge)
            {
                return true;
            }

            return stats.Health <= NeglectedHealthHospitalMax
                   && state != null
                   && state.HasFlag(RunFlags.NeglectedHealth);
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
