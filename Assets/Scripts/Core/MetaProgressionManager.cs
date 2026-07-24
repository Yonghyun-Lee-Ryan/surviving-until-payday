using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 회차 결과 반영 요약.
    /// </summary>
    public sealed class MetaProgressResult
    {
        public int ExperienceGained { get; set; }
        public int TotalExperience { get; set; }
        public int LevelBefore { get; set; }
        public int LevelAfter { get; set; }
        public bool LeveledUp => LevelAfter > LevelBefore;
        public List<string> NewlyUnlockedTraits { get; } = new List<string>();
        public List<string> NewlyUnlockedEndings { get; } = new List<string>();
        public List<string> NewlyUnlockedEvents { get; } = new List<string>();
        public List<string> NewlyUnlockedAchievements { get; } = new List<string>();
    }

    /// <summary>
    /// 인생 경험치, 레벨, 특성/사건/엔딩/업적 해금.
    /// </summary>
    public sealed class MetaProgressionManager
    {
        public UnlockCodex Endings { get; } = new UnlockCodex();
        public UnlockCodex Events { get; } = new UnlockCodex();
        public UnlockCodex Traits { get; } = new UnlockCodex();
        public UnlockCodex Achievements { get; } = new UnlockCodex();

        public int TotalExperience { get; private set; }

        public int Level => PlayerLevel.GetLevel(TotalExperience);

        /// <summary>
        /// (category, id, displayName)
        /// </summary>
        public event Action<string, string, string> UnlockNotified;

        public void Load(
            int totalExperience,
            IEnumerable<string> endingIds,
            IEnumerable<string> eventIds,
            IEnumerable<string> traitIds,
            IEnumerable<string> achievementIds)
        {
            TotalExperience = Math.Max(0, totalExperience);
            Endings.LoadFrom(endingIds);
            Events.LoadFrom(eventIds);
            Traits.LoadFrom(traitIds);
            Achievements.LoadFrom(achievementIds);
        }

        public bool DiscoverEvent(string eventId, string displayName = null)
        {
            if (!Events.TryUnlock(eventId))
            {
                return false;
            }

            RaiseUnlock("event", eventId, displayName ?? eventId);
            return true;
        }

        public bool DiscoverEnding(string endingId, string displayName = null)
        {
            if (!Endings.TryUnlock(endingId))
            {
                return false;
            }

            RaiseUnlock("ending", endingId, displayName ?? endingId);
            return true;
        }

        public bool IsTraitUnlocked(TraitData trait)
        {
            if (trait == null)
            {
                return false;
            }

            if (trait.UnlockLevel <= 1)
            {
                return true;
            }

            return Traits.IsUnlocked(trait.Id) || Level >= trait.UnlockLevel;
        }

        public MetaProgressResult ApplyRunResult(
            ResultData result,
            IEnumerable<TraitData> allTraits,
            IEnumerable<string> discoveredEventIdsThisRun)
        {
            var progress = new MetaProgressResult
            {
                LevelBefore = Level
            };

            if (discoveredEventIdsThisRun != null)
            {
                foreach (var eventId in discoveredEventIdsThisRun)
                {
                    if (DiscoverEvent(eventId))
                    {
                        progress.NewlyUnlockedEvents.Add(eventId);
                    }
                }
            }

            var endingNewlyUnlocked = false;
            if (result?.Ending != null)
            {
                endingNewlyUnlocked = DiscoverEnding(result.Ending.Id, result.Ending.Title);
                if (endingNewlyUnlocked)
                {
                    progress.NewlyUnlockedEndings.Add(result.Ending.Id);
                }
            }

            var gained = ExperienceCalculator.Calculate(
                result != null ? result.DaysSurvived : 0,
                result != null && result.IsSuccess,
                result?.FinalStats,
                endingNewlyUnlocked,
                progress.NewlyUnlockedEvents.Count,
                newlyUnlockedAchievementCount: 0);
            TotalExperience += gained;

            UnlockEligibleTraits(allTraits, progress);

            var achievementsBeforeBonus = progress.NewlyUnlockedAchievements.Count;
            EvaluateAchievements(result, progress);
            var firstWaveAchievements = progress.NewlyUnlockedAchievements.Count - achievementsBeforeBonus;
            var achievementBonus = firstWaveAchievements * ExperienceCalculator.NewAchievementBonus;
            if (achievementBonus > 0)
            {
                TotalExperience += achievementBonus;
                gained += achievementBonus;
            }

            UnlockEligibleTraits(allTraits, progress);
            var achievementsBeforeSecond = progress.NewlyUnlockedAchievements.Count;
            EvaluateAchievements(result, progress);
            var secondWave = progress.NewlyUnlockedAchievements.Count - achievementsBeforeSecond;
            if (secondWave > 0)
            {
                var extra = secondWave * ExperienceCalculator.NewAchievementBonus;
                TotalExperience += extra;
                gained += extra;
                UnlockEligibleTraits(allTraits, progress);
            }

            progress.ExperienceGained = gained;
            progress.TotalExperience = TotalExperience;
            progress.LevelAfter = Level;
            return progress;
        }

        /// <summary>
        /// 결과 화면 보상형 광고(경험치 2배) 등 보너스 XP.
        /// </summary>
        public void AddBonusExperience(int amount, IEnumerable<TraitData> allTraits = null)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalExperience += amount;
            if (allTraits != null)
            {
                UnlockEligibleTraits(allTraits, progress: null);
            }
        }

        private void UnlockEligibleTraits(IEnumerable<TraitData> allTraits, MetaProgressResult progress)
        {
            if (allTraits == null)
            {
                return;
            }

            foreach (var trait in allTraits)
            {
                if (trait == null || string.IsNullOrWhiteSpace(trait.Id))
                {
                    continue;
                }

                // unlockLevel 0~1: 기본 해금. 도감률용으로 한 번만 등록한다.
                if (trait.UnlockLevel <= 1)
                {
                    if (Traits.TryUnlock(trait.Id))
                    {
                        progress?.NewlyUnlockedTraits.Add(trait.Id);
                        RaiseUnlock("trait", trait.Id, trait.DisplayName);
                    }

                    continue;
                }

                if (Level < trait.UnlockLevel)
                {
                    continue;
                }

                if (Traits.TryUnlock(trait.Id))
                {
                    progress?.NewlyUnlockedTraits.Add(trait.Id);
                    RaiseUnlock("trait", trait.Id, trait.DisplayName);
                }
            }
        }

        private void EvaluateAchievements(ResultData result, MetaProgressResult progress)
        {
            if (result == null)
            {
                return;
            }

            TryAchieve(AchievementIds.Survive7Days, result.DaysSurvived >= 7, progress);
            TryAchieve(AchievementIds.Survive30Days, result.IsSuccess && result.DaysSurvived >= 30, progress);
            TryAchieve(
                AchievementIds.CashHalfMillion,
                result.FinalStats != null && result.FinalStats.Cash >= 500_000L,
                progress);
            TryAchieve(AchievementIds.FirstEnding, Endings.UnlockedCount >= 1, progress);
            TryAchieve(AchievementIds.UnlockThreeTraits, Traits.UnlockedCount >= 3, progress);
        }

        private void TryAchieve(string id, bool condition, MetaProgressResult progress)
        {
            if (!condition)
            {
                return;
            }

            if (!Achievements.TryUnlock(id))
            {
                return;
            }

            progress?.NewlyUnlockedAchievements.Add(id);
            RaiseUnlock("achievement", id, id);
        }

        private void RaiseUnlock(string category, string id, string displayName)
        {
            try
            {
                UnlockNotified?.Invoke(category, id, displayName);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
