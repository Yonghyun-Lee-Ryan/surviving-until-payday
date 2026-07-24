using System;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 사건 등장 조건. 런타임 GameState와 비교한다.
    /// </summary>
    [Serializable]
    public sealed class EventCondition
    {
        [Header("능력치 범위")]
        [SerializeField] private int minHealth = StatLimits.MinGauge;
        [SerializeField] private int maxHealth = StatLimits.MaxGauge;
        [SerializeField] private int minStress = StatLimits.MinGauge;
        [SerializeField] private int maxStress = StatLimits.MaxGauge;
        [SerializeField] private int minHappiness = StatLimits.MinGauge;
        [SerializeField] private int maxHappiness = StatLimits.MaxGauge;
        [SerializeField] private int minCompanyScore = StatLimits.MinGauge;
        [SerializeField] private int maxCompanyScore = StatLimits.MaxGauge;

        [Header("현금")]
        [SerializeField] private bool useMinCash;
        [SerializeField] private long minCash;
        [SerializeField] private bool useMaxCash;
        [SerializeField] private long maxCash;

        [Header("기타")]
        [SerializeField] private string requiredJobId = string.Empty;
        [SerializeField] private DayOfWeekConstraint dayOfWeekConstraint = DayOfWeekConstraint.Any;

        public int MinHealth => minHealth;
        public int MaxHealth => maxHealth;
        public int MinStress => minStress;
        public int MaxStress => maxStress;
        public int MinHappiness => minHappiness;
        public int MaxHappiness => maxHappiness;
        public int MinCompanyScore => minCompanyScore;
        public int MaxCompanyScore => maxCompanyScore;
        public bool UseMinCash => useMinCash;
        public long MinCash => minCash;
        public bool UseMaxCash => useMaxCash;
        public long MaxCash => maxCash;
        public string RequiredJobId => requiredJobId;
        public DayOfWeekConstraint DayOfWeekConstraint => dayOfWeekConstraint;

#if UNITY_EDITOR
        public void EditorConfigure(
            int newMinHealth = StatLimits.MinGauge,
            int newMaxHealth = StatLimits.MaxGauge,
            int newMinStress = StatLimits.MinGauge,
            int newMaxStress = StatLimits.MaxGauge,
            int newMinHappiness = StatLimits.MinGauge,
            int newMaxHappiness = StatLimits.MaxGauge,
            int newMinCompanyScore = StatLimits.MinGauge,
            int newMaxCompanyScore = StatLimits.MaxGauge,
            string newRequiredJobId = "",
            DayOfWeekConstraint newDayOfWeekConstraint = DayOfWeekConstraint.Any)
        {
            minHealth = newMinHealth;
            maxHealth = newMaxHealth;
            minStress = newMinStress;
            maxStress = newMaxStress;
            minHappiness = newMinHappiness;
            maxHappiness = newMaxHappiness;
            minCompanyScore = newMinCompanyScore;
            maxCompanyScore = newMaxCompanyScore;
            requiredJobId = newRequiredJobId ?? string.Empty;
            dayOfWeekConstraint = newDayOfWeekConstraint;
        }
#endif

        public string Validate(string context)
        {
            if (minHealth > maxHealth)
            {
                return $"{context}: minHealth({minHealth}) > maxHealth({maxHealth})";
            }

            if (minStress > maxStress)
            {
                return $"{context}: minStress({minStress}) > maxStress({maxStress})";
            }

            if (minHappiness > maxHappiness)
            {
                return $"{context}: minHappiness({minHappiness}) > maxHappiness({maxHappiness})";
            }

            if (minCompanyScore > maxCompanyScore)
            {
                return $"{context}: minCompanyScore({minCompanyScore}) > maxCompanyScore({maxCompanyScore})";
            }

            if (useMinCash && useMaxCash && minCash > maxCash)
            {
                return $"{context}: minCash({minCash}) > maxCash({maxCash})";
            }

            return null;
        }
    }
}
