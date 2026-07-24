using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 날짜 구간별 난도 보정 계수.
    /// <para>
    /// Unit 19 적용 지점: <see cref="SurviveUntilPayday.Events.EffectResolver"/>에서
    /// <b>현금 감소(StatType.Cash &lt; 0)</b>에만 곱한다.
    /// EventSelector 가중치·회복(양수) 효과는 스케일하지 않아 후반에도 회복 사건이 남는다.
    /// </para>
    /// </summary>
    public static class DifficultyScaler
    {
        public static float GetMultiplier(int day)
        {
            if (day < GameState.MinDay || day > GameState.MaxDay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    $"Day must be between {GameState.MinDay} and {GameState.MaxDay}.");
            }

            if (day <= 7)
            {
                return 1.0f;
            }

            if (day <= 14)
            {
                return 1.1f;
            }

            if (day <= 21)
            {
                return 1.2f;
            }

            if (day <= 27)
            {
                return 1.35f;
            }

            return 1.5f;
        }

        /// <summary>
        /// 현금 감소량에 난도 계수를 적용한다. 양수(획득)는 그대로 둔다.
        /// </summary>
        public static long ScaleCashDelta(long cashDelta, float multiplier)
        {
            if (cashDelta >= 0L || Math.Abs(multiplier - 1f) < 0.0001f)
            {
                return cashDelta;
            }

            return (long)Math.Round(cashDelta * multiplier);
        }
    }
}
