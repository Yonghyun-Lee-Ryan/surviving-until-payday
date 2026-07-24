using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 메인 메뉴 도감/메타 진행 표시.
    /// </summary>
    public sealed class CodexPanelView : MonoBehaviour
    {
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text experienceLabel;
        [SerializeField] private Text endingRateLabel;
        [SerializeField] private Text eventRateLabel;
        [SerializeField] private Text traitRateLabel;
        [SerializeField] private Text achievementRateLabel;
        [SerializeField] private Text unlockToastLabel;

        public void Bind(
            Text level,
            Text experience,
            Text endingRate,
            Text eventRate,
            Text traitRate,
            Text achievementRate,
            Text unlockToast)
        {
            levelLabel = level;
            experienceLabel = experience;
            endingRateLabel = endingRate;
            eventRateLabel = eventRate;
            traitRateLabel = traitRate;
            achievementRateLabel = achievementRate;
            unlockToastLabel = unlockToast;
        }

        public void Refresh(
            MetaProgressionManager meta,
            int totalEndings,
            int totalEvents,
            int totalTraits,
            int totalAchievements)
        {
            if (meta == null)
            {
                return;
            }

            var into = PlayerLevel.GetXpIntoCurrentLevel(meta.TotalExperience, out var level, out var toNext);
            if (levelLabel != null)
            {
                levelLabel.text = $"Lv.{level}";
            }

            if (experienceLabel != null)
            {
                experienceLabel.text = toNext > 0
                    ? $"인생 경험치 {meta.TotalExperience} ({into}/{toNext})"
                    : $"인생 경험치 {meta.TotalExperience} (MAX)";
            }

            SetRate(endingRateLabel, "엔딩", meta.Endings.UnlockedCount, totalEndings);
            SetRate(eventRateLabel, "사건", meta.Events.UnlockedCount, totalEvents);
            SetRate(traitRateLabel, "특성", meta.Traits.UnlockedCount, totalTraits);
            SetRate(achievementRateLabel, "업적", meta.Achievements.UnlockedCount, totalAchievements);
        }

        public void ShowUnlockToast(string message)
        {
            if (unlockToastLabel != null)
            {
                unlockToastLabel.text = message ?? string.Empty;
            }
        }

        private static void SetRate(Text label, string title, int unlocked, int total)
        {
            if (label == null)
            {
                return;
            }

            var safeTotal = Mathf.Max(total, unlocked);
            var percent = safeTotal <= 0 ? 0 : Mathf.RoundToInt(100f * unlocked / safeTotal);
            label.text = $"{title} {unlocked}/{safeTotal} ({percent}%)";
        }
    }
}
