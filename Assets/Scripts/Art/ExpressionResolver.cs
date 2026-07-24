using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.Art
{
    /// <summary>
    /// 선택 결과 능력치 변화 → 표정.
    /// </summary>
    public static class ExpressionResolver
    {
        public static ExpressionId FromChoiceResult(ChoiceResult result, ExpressionId fallback = ExpressionId.Default)
        {
            if (result?.StatsBefore == null || result.StatsAfter == null)
            {
                return fallback;
            }

            var before = result.StatsBefore;
            var after = result.StatsAfter;
            var dHealth = after.Health - before.Health;
            var dStress = after.Stress - before.Stress;
            var dHappiness = after.Happiness - before.Happiness;
            var dCash = after.Cash - before.Cash;

            if (after.Health <= 20 || (dHealth <= -15 && after.Health <= 40))
            {
                return ExpressionId.Despair;
            }

            if (dStress >= 12 || after.Stress >= 85)
            {
                return ExpressionId.Angry;
            }

            if (dStress >= 6 || dHealth <= -5 || after.Stress >= 70)
            {
                return ExpressionId.Tired;
            }

            if (dHappiness >= 6 || (dCash >= 50_000L && dHappiness >= 0))
            {
                return ExpressionId.Happy;
            }

            if (dStress >= 3 || dCash <= -80_000L || dHappiness <= -4)
            {
                return ExpressionId.Surprised;
            }

            return fallback;
        }
    }
}
