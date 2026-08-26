using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 당일 로컬 베스트·배정 미션·보상 상태 (Unit 25).
    /// </summary>
    public sealed class DailyContentState
    {
        public string DateKey { get; private set; } = string.Empty;
        public long BestCash { get; private set; }
        public bool BestSurvived { get; private set; }
        public int BestStress { get; private set; } = 999;
        public int BestCompanyScore { get; private set; }
        public int BestDaysSurvived { get; private set; }
        public bool HasBestRecord { get; private set; }
        public int LoginStreak { get; private set; } = 1;
        public string LastVisitDateKey { get; private set; } = string.Empty;
        public bool StreakBonusGrantedToday { get; private set; }
        public int LastVisitBonusExperience { get; private set; }

        public const int VisitBonusXpPerStreakDay = 5;
        public const int MaxVisitBonusMultiplier = 7;

        private readonly List<DailyMissionRuntime> missions = new List<DailyMissionRuntime>();

        public IReadOnlyList<DailyMissionRuntime> Missions => missions;

        public void Load(
            string dateKey,
            long bestCash,
            bool bestSurvived,
            int bestStress,
            int bestCompanyScore,
            int bestDaysSurvived,
            bool hasBestRecord,
            IEnumerable<DailyMissionSaveEntry> entries,
            int loginStreak = 1,
            string lastVisitDateKey = null,
            bool streakBonusGrantedToday = false)
        {
            DateKey = dateKey ?? string.Empty;
            BestCash = bestCash;
            BestSurvived = bestSurvived;
            BestStress = bestStress;
            BestCompanyScore = bestCompanyScore;
            BestDaysSurvived = bestDaysSurvived;
            HasBestRecord = hasBestRecord;
            LoginStreak = loginStreak < 1 ? 1 : loginStreak;
            LastVisitDateKey = lastVisitDateKey ?? string.Empty;
            StreakBonusGrantedToday = streakBonusGrantedToday;
            LastVisitBonusExperience = 0;
            missions.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.missionId))
                {
                    continue;
                }

                missions.Add(new DailyMissionRuntime(
                    entry.missionId,
                    entry.completed,
                    entry.rewardClaimed));
            }
        }

        /// <summary>
        /// 로컬 날짜가 바뀌면 미션을 다시 배정하고 당일 베스트를 초기화한다.
        /// </summary>
        public bool EnsureForLocalDate(
            IReadOnlyList<DailyMissionData> pool,
            DateTime? localNow = null)
        {
            var key = DailyChallenge.LocalDateKey(localNow);
            if (string.Equals(DateKey, key, StringComparison.Ordinal))
            {
                if (missions.Count == 0)
                {
                    AssignMissions(key, pool);
                }

                return false;
            }

            var previousKey = string.IsNullOrEmpty(LastVisitDateKey) ? DateKey : LastVisitDateKey;
            LoginStreak = IsConsecutiveDay(previousKey, key)
                ? Math.Min(99, Math.Max(1, LoginStreak) + 1)
                : 1;
            LastVisitDateKey = key;
            StreakBonusGrantedToday = false;
            LastVisitBonusExperience = 0;

            DateKey = key;
            HasBestRecord = false;
            BestCash = 0;
            BestSurvived = false;
            BestStress = 999;
            BestCompanyScore = 0;
            BestDaysSurvived = 0;
            AssignMissions(key, pool);
            return true;
        }

        private void AssignMissions(string key, IReadOnlyList<DailyMissionData> pool)
        {
            missions.Clear();
            var assigned = DailyMissionAssigner.Assign(pool, DailyChallenge.SeedFromDateKey(key));
            for (var i = 0; i < assigned.Count; i++)
            {
                missions.Add(new DailyMissionRuntime(assigned[i].Id, completed: false, rewardClaimed: false));
            }
        }

        public void BindMissionDefinitions(IReadOnlyList<DailyMissionData> pool)
        {
            if (pool == null)
            {
                return;
            }

            for (var i = 0; i < missions.Count; i++)
            {
                var slot = missions[i];
                for (var p = 0; p < pool.Count; p++)
                {
                    if (pool[p] != null && pool[p].Id == slot.MissionId)
                    {
                        slot.Definition = pool[p];
                        break;
                    }
                }
            }
        }

        public bool TryUpdateBest(ResultData result)
        {
            if (result == null || result.FinalStats == null)
            {
                return false;
            }

            var cash = result.FinalStats.Cash;
            var stress = result.FinalStats.Stress;
            var company = result.FinalStats.CompanyScore;
            var days = result.DaysSurvived;
            var survived = result.IsSuccess;

            if (!HasBestRecord || IsBetter(survived, cash, stress, company, days))
            {
                HasBestRecord = true;
                BestSurvived = survived;
                BestCash = cash;
                BestStress = stress;
                BestCompanyScore = company;
                BestDaysSurvived = days;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 미션 달성·보상 반영. 새로 완료된 미션 정의를 반환한다.
        /// </summary>
        public List<DailyMissionData> ApplyRunToMissions(
            GameState state,
            bool isSuccess,
            MetaProgressionManager meta)
        {
            var newlyCompleted = new List<DailyMissionData>();
            for (var i = 0; i < missions.Count; i++)
            {
                var slot = missions[i];
                var definition = slot.Definition;
                if (definition == null || slot.Completed)
                {
                    continue;
                }

                if (!DailyMissionEvaluator.IsCompleted(definition, state, isSuccess))
                {
                    continue;
                }

                slot.Completed = true;
                newlyCompleted.Add(definition);

                if (!slot.RewardClaimed && meta != null)
                {
                    if (definition.RewardExperience > 0)
                    {
                        meta.AddBonusExperience(definition.RewardExperience);
                    }

                    if (definition.RewardTraitFragments > 0)
                    {
                        meta.AddTraitFragments(definition.RewardTraitFragments);
                    }

                    slot.RewardClaimed = true;
                }
            }

            return newlyCompleted;
        }

        /// <summary>
        /// 날짜가 바뀐 뒤 한 번만 출석 XP. 상점이 아닌 재접속 훅.
        /// </summary>
        public int TryGrantVisitBonus(
            MetaProgressionManager meta,
            IEnumerable<TraitData> traits = null,
            IEnumerable<JobData> jobs = null)
        {
            if (StreakBonusGrantedToday || meta == null)
            {
                return 0;
            }

            var xp = VisitBonusXpPerStreakDay * Math.Min(LoginStreak, MaxVisitBonusMultiplier);
            if (xp <= 0)
            {
                return 0;
            }

            meta.AddBonusExperience(xp, traits, jobs);
            StreakBonusGrantedToday = true;
            LastVisitBonusExperience = xp;
            return xp;
        }

        public static bool IsConsecutiveDay(string previousKey, string nextKey)
        {
            if (!DailyChallenge.TryParseDateKey(previousKey, out var previous))
            {
                return false;
            }

            if (!DailyChallenge.TryParseDateKey(nextKey, out var next))
            {
                return false;
            }

            return (next.Date - previous.Date).Days == 1;
        }

        public List<DailyMissionSaveEntry> CaptureEntries()
        {
            var list = new List<DailyMissionSaveEntry>(missions.Count);
            for (var i = 0; i < missions.Count; i++)
            {
                var slot = missions[i];
                list.Add(new DailyMissionSaveEntry
                {
                    missionId = slot.MissionId,
                    completed = slot.Completed,
                    rewardClaimed = slot.RewardClaimed
                });
            }

            return list;
        }

        private bool IsBetter(bool survived, long cash, int stress, int company, int days)
        {
            if (survived != BestSurvived)
            {
                return survived;
            }

            if (cash != BestCash)
            {
                return cash > BestCash;
            }

            if (days != BestDaysSurvived)
            {
                return days > BestDaysSurvived;
            }

            if (stress != BestStress)
            {
                return stress < BestStress;
            }

            return company > BestCompanyScore;
        }
    }

    public sealed class DailyMissionRuntime
    {
        public string MissionId { get; }
        public bool Completed { get; set; }
        public bool RewardClaimed { get; set; }
        public DailyMissionData Definition { get; set; }

        public DailyMissionRuntime(string missionId, bool completed, bool rewardClaimed)
        {
            MissionId = missionId ?? string.Empty;
            Completed = completed;
            RewardClaimed = rewardClaimed;
        }
    }
}
