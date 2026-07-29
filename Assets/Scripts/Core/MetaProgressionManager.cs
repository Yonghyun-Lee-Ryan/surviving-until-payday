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
        public List<string> NewlyUnlockedJobs { get; } = new List<string>();
        public List<string> NewlyUnlockedEndings { get; } = new List<string>();
        public List<string> NewlyUnlockedEvents { get; } = new List<string>();
        /// <summary>NewlyUnlockedEvents와 같은 순서의 표시 제목.</summary>
        public List<string> NewlyUnlockedEventTitles { get; } = new List<string>();
        /// <summary>NewlyUnlockedTraits와 같은 순서의 표시 이름.</summary>
        public List<string> NewlyUnlockedTraitNames { get; } = new List<string>();
        /// <summary>NewlyUnlockedJobs와 같은 순서의 표시 이름.</summary>
        public List<string> NewlyUnlockedJobNames { get; } = new List<string>();
        public List<string> NewlyUnlockedAchievements { get; } = new List<string>();
        public int TraitFragmentsGained { get; set; }
    }

    /// <summary>
    /// 인생 경험치, 레벨, 특성/직업/사건/엔딩/업적 해금.
    /// </summary>
    public sealed class MetaProgressionManager
    {
        public UnlockCodex Endings { get; } = new UnlockCodex();
        public UnlockCodex Events { get; } = new UnlockCodex();
        public UnlockCodex Traits { get; } = new UnlockCodex();
        public UnlockCodex Jobs { get; } = new UnlockCodex();
        public UnlockCodex Achievements { get; } = new UnlockCodex();

        public DailyContentState Daily { get; } = new DailyContentState();

        public int TotalExperience { get; private set; }

        /// <summary>업적·일일 미션 등으로 쌓인 특성 조각(표시·메타용).</summary>
        public int TraitFragmentCount { get; private set; }

        /// <summary>Unit 26: 첫 실행 튜토리얼 완료/스킵 여부.</summary>
        public bool FirstRunTutorialCompleted { get; private set; }

        /// <summary>전면 광고 제거 소유(레거시 세이브 호환). 상점 제거 후 신규 구매 경로는 없음.</summary>
        public bool HasNoAds { get; private set; }

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
            IEnumerable<string> achievementIds,
            IEnumerable<string> jobIds = null,
            int traitFragmentCount = 0,
            bool firstRunTutorialCompleted = false,
            bool hasNoAds = false)
        {
            TotalExperience = Math.Max(0, totalExperience);
            TraitFragmentCount = Math.Max(0, traitFragmentCount);
            FirstRunTutorialCompleted = firstRunTutorialCompleted;
            HasNoAds = hasNoAds;
            Endings.LoadFrom(endingIds);
            Events.LoadFrom(eventIds);
            Traits.LoadFrom(traitIds);
            Achievements.LoadFrom(achievementIds);
            Jobs.LoadFrom(jobIds);
        }

        public void MarkFirstRunTutorialCompleted()
        {
            FirstRunTutorialCompleted = true;
        }

        public void SetHasNoAds(bool owned)
        {
            HasNoAds = owned;
        }

        public void AddTraitFragments(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            TraitFragmentCount += amount;
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

        public bool IsJobUnlocked(JobData job)
        {
            if (job == null)
            {
                return false;
            }

            if (job.UnlockLevel <= 1)
            {
                return true;
            }

            return Jobs.IsUnlocked(job.Id) || Level >= job.UnlockLevel;
        }

        public MetaProgressResult ApplyRunResult(
            ResultData result,
            IEnumerable<TraitData> allTraits,
            IEnumerable<string> discoveredEventIdsThisRun,
            IEnumerable<JobData> allJobs = null,
            IEnumerable<EventData> allEvents = null)
        {
            var progress = new MetaProgressResult
            {
                LevelBefore = Level
            };

            var eventTitles = BuildEventTitleMap(allEvents);
            if (discoveredEventIdsThisRun != null)
            {
                foreach (var eventId in discoveredEventIdsThisRun)
                {
                    if (string.IsNullOrWhiteSpace(eventId))
                    {
                        continue;
                    }

                    eventTitles.TryGetValue(eventId, out var title);
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = eventId;
                    }

                    if (DiscoverEvent(eventId, title))
                    {
                        progress.NewlyUnlockedEvents.Add(eventId);
                        progress.NewlyUnlockedEventTitles.Add(title);
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
            UnlockEligibleJobs(allJobs, progress);

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
            UnlockEligibleJobs(allJobs, progress);
            var achievementsBeforeSecond = progress.NewlyUnlockedAchievements.Count;
            EvaluateAchievements(result, progress);
            var secondWave = progress.NewlyUnlockedAchievements.Count - achievementsBeforeSecond;
            if (secondWave > 0)
            {
                var extra = secondWave * ExperienceCalculator.NewAchievementBonus;
                TotalExperience += extra;
                gained += extra;
                UnlockEligibleTraits(allTraits, progress);
                UnlockEligibleJobs(allJobs, progress);
            }

            progress.ExperienceGained = gained;
            progress.TotalExperience = TotalExperience;
            progress.LevelAfter = Level;
            return progress;
        }

        private static Dictionary<string, string> BuildEventTitleMap(IEnumerable<EventData> allEvents)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (allEvents == null)
            {
                return map;
            }

            foreach (var eventData in allEvents)
            {
                if (eventData == null || string.IsNullOrWhiteSpace(eventData.Id))
                {
                    continue;
                }

                map[eventData.Id] = string.IsNullOrWhiteSpace(eventData.Title)
                    ? eventData.Id
                    : eventData.Title;
            }

            return map;
        }

        /// <summary>
        /// 결과 화면 보상형 광고(경험치 2배) 등 보너스 XP.
        /// </summary>
        public void AddBonusExperience(
            int amount,
            IEnumerable<TraitData> allTraits = null,
            IEnumerable<JobData> allJobs = null)
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

            if (allJobs != null)
            {
                UnlockEligibleJobs(allJobs, progress: null);
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

                if (trait.UnlockLevel <= 1)
                {
                    if (Traits.TryUnlock(trait.Id))
                    {
                        progress?.NewlyUnlockedTraits.Add(trait.Id);
                        progress?.NewlyUnlockedTraitNames.Add(
                            string.IsNullOrWhiteSpace(trait.DisplayName) ? trait.Id : trait.DisplayName);
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
                    progress?.NewlyUnlockedTraitNames.Add(
                        string.IsNullOrWhiteSpace(trait.DisplayName) ? trait.Id : trait.DisplayName);
                    RaiseUnlock("trait", trait.Id, trait.DisplayName);
                }
            }
        }

        private void UnlockEligibleJobs(IEnumerable<JobData> allJobs, MetaProgressResult progress)
        {
            if (allJobs == null)
            {
                return;
            }

            foreach (var job in allJobs)
            {
                if (job == null || string.IsNullOrWhiteSpace(job.Id))
                {
                    continue;
                }

                if (job.UnlockLevel <= 1)
                {
                    if (Jobs.TryUnlock(job.Id))
                    {
                        progress?.NewlyUnlockedJobs.Add(job.Id);
                        progress?.NewlyUnlockedJobNames.Add(
                            string.IsNullOrWhiteSpace(job.DisplayName) ? job.Id : job.DisplayName);
                        RaiseUnlock("job", job.Id, job.DisplayName);
                    }

                    continue;
                }

                if (Level < job.UnlockLevel)
                {
                    continue;
                }

                if (Jobs.TryUnlock(job.Id))
                {
                    progress?.NewlyUnlockedJobs.Add(job.Id);
                    progress?.NewlyUnlockedJobNames.Add(
                        string.IsNullOrWhiteSpace(job.DisplayName) ? job.Id : job.DisplayName);
                    RaiseUnlock("job", job.Id, job.DisplayName);
                }
            }
        }

        private void EvaluateAchievements(ResultData result, MetaProgressResult progress)
        {
            if (result == null)
            {
                return;
            }

            var stats = result.FinalStats;
            var endingId = result.Ending != null ? result.Ending.Id : null;

            TryAchieve(AchievementIds.Survive7Days, result.DaysSurvived >= 7, progress);
            TryAchieve(AchievementIds.Survive15Days, result.DaysSurvived >= 15, progress);
            TryAchieve(AchievementIds.Survive30Days, result.IsSuccess && result.DaysSurvived >= 30, progress);
            TryAchieve(AchievementIds.PaydaySuccess, result.IsSuccess, progress);
            TryAchieve(
                AchievementIds.CashHalfMillion,
                stats != null && stats.Cash >= 500_000L,
                progress);
            TryAchieve(
                AchievementIds.CashOneMillion,
                stats != null && stats.Cash >= 1_000_000L,
                progress);
            TryAchieve(
                AchievementIds.HealthNinety,
                stats != null && stats.Health >= 90,
                progress);
            TryAchieve(
                AchievementIds.StressTenOrLess,
                stats != null && stats.Stress <= 10,
                progress);
            TryAchieve(
                AchievementIds.HappinessNinety,
                stats != null && stats.Happiness >= 90,
                progress);
            TryAchieve(
                AchievementIds.CompanyNinety,
                stats != null && stats.CompanyScore >= 90,
                progress);
            TryAchieve(AchievementIds.FirstEnding, Endings.UnlockedCount >= 1, progress);
            TryAchieve(AchievementIds.EndingsFive, Endings.UnlockedCount >= 5, progress);
            TryAchieve(AchievementIds.UnlockThreeTraits, Traits.UnlockedCount >= 3, progress);
            TryAchieve(AchievementIds.EventsTen, Events.UnlockedCount >= 10, progress);
            TryAchieve(AchievementIds.EventsThirty, Events.UnlockedCount >= 30, progress);
            TryAchieve(AchievementIds.JobsTwo, Jobs.UnlockedCount >= 2, progress);
            TryAchieve(AchievementIds.JobsThree, Jobs.UnlockedCount >= 3, progress);
            TryAchieve(
                AchievementIds.CardJuggleEnding,
                endingId == "ending_card_juggle",
                progress);
            TryAchieve(
                AchievementIds.OneBigShotEnding,
                endingId == "ending_one_big_shot",
                progress);
            TryAchieve(
                AchievementIds.ResignReadyEnding,
                endingId == "ending_resign_ready",
                progress);
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
            const int fragmentPerAchievement = 1;
            AddTraitFragments(fragmentPerAchievement);
            if (progress != null)
            {
                progress.TraitFragmentsGained += fragmentPerAchievement;
            }

            RaiseUnlock("achievement", id, AchievementIds.GetDisplayName(id));
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
