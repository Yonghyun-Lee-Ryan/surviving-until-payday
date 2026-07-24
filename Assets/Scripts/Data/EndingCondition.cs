using System;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 엔딩 조건. EndingEvaluator(개발 단위 8)에서 평가한다.
    /// </summary>
    [Serializable]
    public sealed class EndingCondition
    {
        [SerializeField] private bool requireMinCash;
        [SerializeField] private long minCash;
        [SerializeField] private bool requireMaxCash;
        [SerializeField] private long maxCash;

        [SerializeField] private bool requireMinHealth;
        [SerializeField] private int minHealth;
        [SerializeField] private bool requireMaxHealth;
        [SerializeField] private int maxHealth;

        [SerializeField] private bool requireMinStress;
        [SerializeField] private int minStress;
        [SerializeField] private bool requireMaxStress;
        [SerializeField] private int maxStress;

        [SerializeField] private bool requireMinHappiness;
        [SerializeField] private int minHappiness;
        [SerializeField] private bool requireMaxHappiness;
        [SerializeField] private int maxHappiness;

        [SerializeField] private bool requireMinCompanyScore;
        [SerializeField] private int minCompanyScore;
        [SerializeField] private bool requireMaxCompanyScore;
        [SerializeField] private int maxCompanyScore;

        public bool RequireMinCash => requireMinCash;
        public long MinCash => minCash;
        public bool RequireMaxCash => requireMaxCash;
        public long MaxCash => maxCash;
        public bool RequireMinHealth => requireMinHealth;
        public int MinHealth => minHealth;
        public bool RequireMaxHealth => requireMaxHealth;
        public int MaxHealth => maxHealth;
        public bool RequireMinStress => requireMinStress;
        public int MinStress => minStress;
        public bool RequireMaxStress => requireMaxStress;
        public int MaxStress => maxStress;
        public bool RequireMinHappiness => requireMinHappiness;
        public int MinHappiness => minHappiness;
        public bool RequireMaxHappiness => requireMaxHappiness;
        public int MaxHappiness => maxHappiness;
        public bool RequireMinCompanyScore => requireMinCompanyScore;
        public int MinCompanyScore => minCompanyScore;
        public bool RequireMaxCompanyScore => requireMaxCompanyScore;
        public int MaxCompanyScore => maxCompanyScore;

#if UNITY_EDITOR
        public void EditorSetCash(bool useMin, long min, bool useMax, long max)
        {
            requireMinCash = useMin;
            minCash = min;
            requireMaxCash = useMax;
            maxCash = max;
        }

        public void EditorSetHealth(bool useMin, int min, bool useMax, int max)
        {
            requireMinHealth = useMin;
            minHealth = min;
            requireMaxHealth = useMax;
            maxHealth = max;
        }

        public void EditorSetStress(bool useMin, int min, bool useMax, int max)
        {
            requireMinStress = useMin;
            minStress = min;
            requireMaxStress = useMax;
            maxStress = max;
        }

        public void EditorSetHappiness(bool useMin, int min, bool useMax, int max)
        {
            requireMinHappiness = useMin;
            minHappiness = min;
            requireMaxHappiness = useMax;
            maxHappiness = max;
        }

        public void EditorSetCompanyScore(bool useMin, int min, bool useMax, int max)
        {
            requireMinCompanyScore = useMin;
            minCompanyScore = min;
            requireMaxCompanyScore = useMax;
            maxCompanyScore = max;
        }
#endif

        public string Validate(string context)
        {
            if (requireMinCash && requireMaxCash && minCash > maxCash)
            {
                return $"{context}: minCash > maxCash";
            }

            if (requireMinHealth && requireMaxHealth && minHealth > maxHealth)
            {
                return $"{context}: minHealth > maxHealth";
            }

            if (requireMinStress && requireMaxStress && minStress > maxStress)
            {
                return $"{context}: minStress > maxStress";
            }

            if (requireMinHappiness && requireMaxHappiness && minHappiness > maxHappiness)
            {
                return $"{context}: minHappiness > maxHappiness";
            }

            if (requireMinCompanyScore && requireMaxCompanyScore && minCompanyScore > maxCompanyScore)
            {
                return $"{context}: minCompanyScore > maxCompanyScore";
            }

            return null;
        }
    }
}
