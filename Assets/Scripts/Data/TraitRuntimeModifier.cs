using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 선택 특성의 런타임 수치 보정. ScriptableObject 원본 효과는 바꾸지 않는다.
    /// </summary>
    public static class TraitRuntimeModifier
    {
        public static List<StatEffect> Apply(
            TraitData trait,
            EventCategory category,
            IReadOnlyList<StatEffect> source)
        {
            var result = new List<StatEffect>();
            if (source == null)
            {
                return result;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var effect = source[i];
                if (effect == null)
                {
                    continue;
                }

                result.Add(Adjust(trait, category, effect));
            }

            return result;
        }

        public static StatEffect Adjust(TraitData trait, EventCategory category, StatEffect effect)
        {
            if (effect == null)
            {
                return null;
            }

            if (trait == null)
            {
                return effect;
            }

            var value = effect.Value;
            switch (effect.StatType)
            {
                case StatType.Cash when value < 0L:
                    value = ScaleLong(value, trait.CashLossMultiplier);
                    break;
                case StatType.Happiness when value > 0L:
                    value = ScaleLong(value, trait.HappinessGainMultiplier);
                    break;
                case StatType.Stress when value > 0L && category == EventCategory.Work:
                    value = ScaleLong(value, trait.WorkStressGainMultiplier);
                    break;
            }

            return value == effect.Value ? effect : new StatEffect(effect.StatType, value);
        }

        private static long ScaleLong(long value, float multiplier)
        {
            if (Math.Abs(multiplier - 1f) < 0.0001f)
            {
                return value;
            }

            return (long)Math.Round(value * multiplier);
        }
    }
}
