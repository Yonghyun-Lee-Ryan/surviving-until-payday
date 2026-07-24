namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 게이지형 능력치(건강/스트레스/행복도/회사 평가) 범위 상수.
    /// </summary>
    public static class StatLimits
    {
        public const int MinGauge = 0;
        public const int MaxGauge = 100;

        public static bool IsGaugeStat(StatType statType)
        {
            return statType != StatType.Cash;
        }

        public static int ClampGauge(int value)
        {
            if (value < MinGauge)
            {
                return MinGauge;
            }

            if (value > MaxGauge)
            {
                return MaxGauge;
            }

            return value;
        }
    }
}
