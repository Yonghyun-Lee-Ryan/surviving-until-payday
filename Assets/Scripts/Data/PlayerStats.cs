using System;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 런타임 플레이어 능력치. ScriptableObject와 분리된 복사본으로만 사용한다.
    /// </summary>
    [Serializable]
    public sealed class PlayerStats
    {
        [SerializeField] private long cash;
        [SerializeField] private int health = 80;
        [SerializeField] private int stress = 20;
        [SerializeField] private int happiness = 50;
        [SerializeField] private int companyScore = 50;

        public long Cash
        {
            get => cash;
            set => cash = value;
        }

        public int Health
        {
            get => health;
            set => health = value;
        }

        public int Stress
        {
            get => stress;
            set => stress = value;
        }

        public int Happiness
        {
            get => happiness;
            set => happiness = value;
        }

        public int CompanyScore
        {
            get => companyScore;
            set => companyScore = value;
        }

        public PlayerStats()
        {
        }

        public PlayerStats(long cash, int health, int stress, int happiness, int companyScore)
        {
            this.cash = cash;
            this.health = health;
            this.stress = stress;
            this.happiness = happiness;
            this.companyScore = companyScore;
        }

        public PlayerStats Clone()
        {
            return new PlayerStats(cash, health, stress, happiness, companyScore);
        }

        public void CopyFrom(PlayerStats source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            cash = source.cash;
            health = source.health;
            stress = source.stress;
            happiness = source.happiness;
            companyScore = source.companyScore;
        }

        public long GetStat(StatType statType)
        {
            switch (statType)
            {
                case StatType.Cash:
                    return cash;
                case StatType.Health:
                    return health;
                case StatType.Stress:
                    return stress;
                case StatType.Happiness:
                    return happiness;
                case StatType.CompanyScore:
                    return companyScore;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown StatType.");
            }
        }

        public void SetStat(StatType statType, long value)
        {
            switch (statType)
            {
                case StatType.Cash:
                    cash = value;
                    break;
                case StatType.Health:
                    health = (int)value;
                    break;
                case StatType.Stress:
                    stress = (int)value;
                    break;
                case StatType.Happiness:
                    happiness = (int)value;
                    break;
                case StatType.CompanyScore:
                    companyScore = (int)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown StatType.");
            }
        }

        public bool AreGaugesInValidRange()
        {
            return IsGaugeInRange(health)
                   && IsGaugeInRange(stress)
                   && IsGaugeInRange(happiness)
                   && IsGaugeInRange(companyScore);
        }

        private static bool IsGaugeInRange(int value)
        {
            return value >= StatLimits.MinGauge && value <= StatLimits.MaxGauge;
        }
    }
}
