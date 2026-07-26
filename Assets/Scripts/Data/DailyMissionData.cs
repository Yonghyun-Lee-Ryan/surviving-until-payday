using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 일일 미션 정의 (Unit 25).
    /// </summary>
    [CreateAssetMenu(
        fileName = "DailyMission",
        menuName = "Surviving Until Payday/Data/Daily Mission")]
    public sealed class DailyMissionData : ScriptableObject
    {
        [SerializeField] private string id = "mission_";
        [SerializeField] private string title = "일일 미션";
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private DailyMissionGoalType goalType = DailyMissionGoalType.SurviveMinDays;
        [SerializeField] private long longThreshold;
        [SerializeField] private int intThreshold;
        [SerializeField] private string flagId = string.Empty;
        [SerializeField] private int rewardExperience = 20;
        [SerializeField] private int rewardTraitFragments = 1;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public DailyMissionGoalType GoalType => goalType;
        public long LongThreshold => longThreshold;
        public int IntThreshold => intThreshold;
        public string FlagId => flagId ?? string.Empty;
        public int RewardExperience => rewardExperience;
        public int RewardTraitFragments => rewardTraitFragments;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("id가 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add("title이 비어 있습니다.");
            }

            if (rewardExperience < 0)
            {
                errors.Add("rewardExperience는 0 이상이어야 합니다.");
            }

            if (rewardTraitFragments < 0)
            {
                errors.Add("rewardTraitFragments는 0 이상이어야 합니다.");
            }

            if (goalType == DailyMissionGoalType.ForbiddenFlagThroughDays &&
                string.IsNullOrWhiteSpace(flagId))
            {
                errors.Add("ForbiddenFlagThroughDays는 flagId가 필요합니다.");
            }

            return errors;
        }

        public void Configure(
            string newId,
            string newTitle,
            string newDescription,
            DailyMissionGoalType type,
            long newLongThreshold,
            int newIntThreshold,
            string newFlagId,
            int xp,
            int fragments)
        {
            id = newId;
            title = newTitle;
            description = newDescription;
            goalType = type;
            longThreshold = newLongThreshold;
            intThreshold = newIntThreshold;
            flagId = newFlagId ?? string.Empty;
            rewardExperience = xp;
            rewardTraitFragments = fragments;
        }

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            string newTitle,
            string newDescription,
            DailyMissionGoalType type,
            long newLongThreshold,
            int newIntThreshold,
            string newFlagId,
            int xp,
            int fragments)
        {
            Configure(
                newId,
                newTitle,
                newDescription,
                type,
                newLongThreshold,
                newIntThreshold,
                newFlagId,
                xp,
                fragments);
        }
#endif
    }
}
