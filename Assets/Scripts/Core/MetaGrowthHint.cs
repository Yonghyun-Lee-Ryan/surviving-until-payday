using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 해금률·다음 목표 문구 (R-QA-05). UI는 표시만 한다.
    /// </summary>
    public static class MetaGrowthHint
    {
        public static int OverallPercent(
            int endingUnlocked,
            int endingTotal,
            int eventUnlocked,
            int eventTotal,
            int traitUnlocked,
            int traitTotal,
            int jobUnlocked,
            int jobTotal,
            int achievementUnlocked,
            int achievementTotal)
        {
            var unlocked = endingUnlocked + eventUnlocked + traitUnlocked + jobUnlocked + achievementUnlocked;
            var total = endingTotal + eventTotal + traitTotal + jobTotal + achievementTotal;
            if (total <= 0)
            {
                return 0;
            }

            return (unlocked * 100) / total;
        }

        public static int XpRemainingToReachLevel(int totalExperience, int targetLevel)
        {
            if (targetLevel <= 1)
            {
                return 0;
            }

            var needed = 0;
            for (var level = 1; level < targetLevel; level++)
            {
                needed += PlayerLevel.GetXpToNextLevel(level);
            }

            var remaining = needed - totalExperience;
            return remaining < 0 ? 0 : remaining;
        }

        public static string BuildNextGoal(
            MetaProgressionManager meta,
            IReadOnlyList<JobData> jobs,
            IReadOnlyList<TraitData> traits)
        {
            if (meta == null)
            {
                return "다음 목표: 한 회차를 플레이해 경험치를 쌓으세요.";
            }

            var nextName = string.Empty;
            var nextKind = string.Empty;
            var nextLevel = int.MaxValue;

            if (jobs != null)
            {
                for (var i = 0; i < jobs.Count; i++)
                {
                    var job = jobs[i];
                    if (job == null || job.UnlockLevel <= 1 || meta.IsJobUnlocked(job))
                    {
                        continue;
                    }

                    if (job.UnlockLevel < nextLevel)
                    {
                        nextLevel = job.UnlockLevel;
                        nextName = string.IsNullOrWhiteSpace(job.DisplayName) ? job.Id : job.DisplayName;
                        nextKind = "직업";
                    }
                }
            }

            if (traits != null)
            {
                for (var i = 0; i < traits.Count; i++)
                {
                    var trait = traits[i];
                    if (trait == null || trait.UnlockLevel <= 1 || meta.IsTraitUnlocked(trait))
                    {
                        continue;
                    }

                    if (trait.UnlockLevel < nextLevel)
                    {
                        nextLevel = trait.UnlockLevel;
                        nextName = string.IsNullOrWhiteSpace(trait.DisplayName) ? trait.Id : trait.DisplayName;
                        nextKind = "특성";
                    }
                }
            }

            if (nextLevel < int.MaxValue && !string.IsNullOrEmpty(nextName))
            {
                var xpLeft = XpRemainingToReachLevel(meta.TotalExperience, nextLevel);
                return xpLeft > 0
                    ? $"다음 목표: Lv.{nextLevel} {nextKind} 「{nextName}」 · 경험치 {xpLeft} 남음"
                    : $"다음 목표: {nextKind} 「{nextName}」 해금 가능";
            }

            var catalog = AchievementIds.Catalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                var def = AchievementCatalog.Get(catalog[i].Id);
                if (!meta.Achievements.IsUnlocked(def.Id))
                {
                    return $"다음 목표: 업적 「{def.Title}」";
                }
            }

            return "다음 목표: 엔딩·사건을 더 모아 도감을 채워 보세요.";
        }
    }
}
