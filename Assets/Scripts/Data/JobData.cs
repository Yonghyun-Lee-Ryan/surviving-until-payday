using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 직업 정의 데이터. 런타임에서는 복사본만 사용한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Job_",
        menuName = "Survive Until Payday/Data/Job",
        order = 10)]
    public sealed class JobData : ScriptableObject
    {
        [SerializeField] private string id = "job_junior_office";
        [SerializeField] private string displayName = "중소기업 신입사원";
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private int unlockLevel;
        [SerializeField] private long salary = 2_800_000L;
        [SerializeField] private long startingCash = 2_800_000L;
        [SerializeField] private int startingHealth = 80;
        [SerializeField] private int startingStress = 20;
        [SerializeField] private int startingHappiness = 50;
        [SerializeField] private int startingCompanyScore = 50;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int UnlockLevel => unlockLevel;
        public long Salary => salary;
        public long StartingCash => startingCash;
        public int StartingHealth => startingHealth;
        public int StartingStress => startingStress;
        public int StartingHappiness => startingHappiness;
        public int StartingCompanyScore => startingCompanyScore;

        public PlayerStats CreateStartingStats()
        {
            return new PlayerStats(
                startingCash,
                startingHealth,
                startingStress,
                startingHappiness,
                startingCompanyScore);
        }

        private void OnValidate()
        {
            foreach (var error in Validate())
            {
                Debug.LogWarning($"[JobData:{name}] {error}", this);
            }
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("id가 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add("displayName이 비어 있습니다.");
            }

            if (unlockLevel < 0)
            {
                errors.Add($"unlockLevel({unlockLevel})는 0 이상이어야 합니다.");
            }

            if (salary < 0)
            {
                errors.Add($"salary({salary})는 0 이상이어야 합니다.");
            }

            if (startingCash < 0)
            {
                errors.Add($"startingCash({startingCash})는 0 이상이어야 합니다.");
            }

            ValidateGauge("startingHealth", startingHealth, errors);
            ValidateGauge("startingStress", startingStress, errors);
            ValidateGauge("startingHappiness", startingHappiness, errors);
            ValidateGauge("startingCompanyScore", startingCompanyScore, errors);

            return errors;
        }

        private static void ValidateGauge(string fieldName, int value, List<string> errors)
        {
            if (value < StatLimits.MinGauge || value > StatLimits.MaxGauge)
            {
                errors.Add($"{fieldName}({value})는 {StatLimits.MinGauge}~{StatLimits.MaxGauge} 범위여야 합니다.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            string newDisplayName,
            string newDescription,
            int newUnlockLevel,
            long newSalary,
            long newStartingCash,
            int health,
            int stress,
            int happiness,
            int companyScore)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            unlockLevel = newUnlockLevel;
            salary = newSalary;
            startingCash = newStartingCash;
            startingHealth = health;
            startingStress = stress;
            startingHappiness = happiness;
            startingCompanyScore = companyScore;
        }
#endif
    }
}
