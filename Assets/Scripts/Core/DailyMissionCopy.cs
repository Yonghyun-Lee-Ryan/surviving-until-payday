using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 일일 미션 한 줄 카피 (보상 수치 포함).
    /// </summary>
    public static class DailyMissionCopy
    {
        public static string FormatLine(DailyMissionRuntime slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            var title = slot.Definition != null ? slot.Definition.Title : slot.MissionId;
            var mark = slot.Completed ? "[완료]" : "[진행]";
            var reward = FormatReward(slot.Definition);
            return string.IsNullOrEmpty(reward) ? $"{mark} {title}" : $"{mark} {title}  ·  {reward}";
        }

        public static string FormatReward(DailyMissionData definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var xp = definition.RewardExperience;
            var fragments = definition.RewardTraitFragments;
            if (xp <= 0 && fragments <= 0)
            {
                return string.Empty;
            }

            if (xp > 0 && fragments > 0)
            {
                return $"+{xp} XP · 조각 +{fragments}";
            }

            if (xp > 0)
            {
                return $"+{xp} XP";
            }

            return $"조각 +{fragments}";
        }
    }
}
