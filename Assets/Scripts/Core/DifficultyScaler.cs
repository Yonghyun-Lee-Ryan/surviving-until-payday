using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 날짜 구간별 난도 보정 계수.
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
    }
}
