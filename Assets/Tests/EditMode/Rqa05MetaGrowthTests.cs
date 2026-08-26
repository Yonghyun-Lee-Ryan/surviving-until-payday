using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa05MetaGrowthTests
    {
        [Test]
        public void AchievementCatalog_FallsBackToCodeDefinitions()
        {
            Assert.AreEqual(20, AchievementIds.Catalog.Count);
            var first = AchievementCatalog.Get(AchievementIds.FirstEnding);
            Assert.AreEqual("첫 엔딩", first.Title);
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.Description));
            if (AchievementCatalog.ResourceCount > 0)
            {
                Assert.AreEqual(20, AchievementCatalog.ResourceCount);
            }
        }

        [Test]
        public void DailyMissionCopy_IncludesXpAndFragments()
        {
            var definition = ScriptableObject.CreateInstance<DailyMissionData>();
            definition.Configure(
                "m_copy",
                "잔액 지키기",
                "",
                DailyMissionGoalType.MinCashOnEnd,
                500_000L,
                0,
                null,
                25,
                1);
            Assert.AreEqual("+25 XP · 조각 +1", DailyMissionCopy.FormatReward(definition));

            var slot = new DailyMissionRuntime("m_copy", completed: false, rewardClaimed: false)
            {
                Definition = definition
            };
            var line = DailyMissionCopy.FormatLine(slot);
            Assert.IsTrue(line.Contains("[진행]"));
            Assert.IsTrue(line.Contains("잔액 지키기"));
            Assert.IsTrue(line.Contains("+25 XP"));
        }

        [Test]
        public void MetaGrowthHint_NewPlayerPointsToLevel2Job()
        {
            var jobs = CreateJobs();
            var traits = CreateTraits();
            var meta = new MetaProgressionManager();
            var hint = MetaGrowthHint.BuildNextGoal(meta, jobs, traits);
            Assert.IsTrue(hint.Contains("Lv.2"), hint);
            Assert.IsTrue(hint.Contains("100"), hint);
            Assert.AreEqual(100, MetaGrowthHint.XpRemainingToReachLevel(0, 2));
            Assert.AreEqual(300, MetaGrowthHint.XpRemainingToReachLevel(0, 3));
            Assert.AreEqual(600, MetaGrowthHint.XpRemainingToReachLevel(0, 4));
        }

        [Test]
        public void MetaGrowthHint_UnlockCurve_CivilThenFreelancerThenOvertime()
        {
            var jobs = CreateJobs();
            var traits = CreateTraits();
            var meta = new MetaProgressionManager();
            meta.RefreshUnlocksFromLevel(traits, jobs);

            Assert.IsTrue(meta.IsJobUnlocked(jobs[0]));
            Assert.IsFalse(meta.IsJobUnlocked(jobs[1]));
            Assert.IsFalse(meta.IsTraitUnlocked(traits[1]));

            meta.AddBonusExperience(100, traits, jobs);
            Assert.AreEqual(2, meta.Level);
            Assert.IsTrue(meta.IsJobUnlocked(jobs[1]));
            Assert.IsTrue(meta.IsTraitUnlocked(traits[1]));
            Assert.IsFalse(meta.IsJobUnlocked(jobs[2]));

            var afterLv2 = MetaGrowthHint.BuildNextGoal(meta, jobs, traits);
            Assert.IsTrue(afterLv2.Contains("Lv.3"), afterLv2);

            meta.AddBonusExperience(200, traits, jobs);
            Assert.AreEqual(3, meta.Level);
            Assert.IsTrue(meta.IsJobUnlocked(jobs[2]));
            Assert.IsTrue(meta.IsTraitUnlocked(traits[2]));
            Assert.IsFalse(meta.IsTraitUnlocked(traits[3]));

            meta.AddBonusExperience(300, traits, jobs);
            Assert.AreEqual(4, meta.Level);
            Assert.IsTrue(meta.IsTraitUnlocked(traits[3]));
        }

        [Test]
        public void MetaGrowthHint_OverallPercent_UsesAllCodexBuckets()
        {
            Assert.AreEqual(50, MetaGrowthHint.OverallPercent(1, 2, 1, 2, 1, 2, 1, 2, 10, 20));
            Assert.AreEqual(0, MetaGrowthHint.OverallPercent(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        [Test]
        public void DailyContentState_Streak_ConsecutiveSkipAndSameDay()
        {
            var pool = CreateTinyPool();
            var daily = new DailyContentState();
            var meta = new MetaProgressionManager();

            daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 23, 9, 0, 0));
            Assert.AreEqual(1, daily.LoginStreak);
            Assert.AreEqual(5, daily.TryGrantVisitBonus(meta));
            Assert.AreEqual(0, daily.TryGrantVisitBonus(meta));
            Assert.AreEqual(5, meta.TotalExperience);

            var sameDay = daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 23, 21, 0, 0));
            Assert.IsFalse(sameDay);
            Assert.AreEqual(1, daily.LoginStreak);
            Assert.AreEqual(0, daily.TryGrantVisitBonus(meta));

            daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 24, 8, 0, 0));
            Assert.AreEqual(2, daily.LoginStreak);
            Assert.AreEqual(10, daily.TryGrantVisitBonus(meta));
            Assert.AreEqual(15, meta.TotalExperience);

            daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 26, 8, 0, 0));
            Assert.AreEqual(1, daily.LoginStreak);
            Assert.AreEqual(5, daily.TryGrantVisitBonus(meta));
        }

        [Test]
        public void DailyContentState_IsConsecutiveDay_OnlyNextCalendarDay()
        {
            Assert.IsTrue(DailyContentState.IsConsecutiveDay("2026-08-23", "2026-08-24"));
            Assert.IsFalse(DailyContentState.IsConsecutiveDay("2026-08-23", "2026-08-25"));
            Assert.IsFalse(DailyContentState.IsConsecutiveDay("2026-08-24", "2026-08-24"));
            Assert.IsFalse(DailyContentState.IsConsecutiveDay(string.Empty, "2026-08-24"));
        }

        [Test]
        public void SaveMapper_RoundTripsLoginStreak()
        {
            var pool = CreateTinyPool();
            var meta = new MetaProgressionManager();
            meta.Daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 23));
            meta.Daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 24));
            meta.Daily.TryGrantVisitBonus(meta);
            Assert.AreEqual(2, meta.Daily.LoginStreak);

            var captured = SaveMapper.CaptureMeta(meta);
            Assert.AreEqual(2, captured.dailyLoginStreak);
            Assert.AreEqual("2026-08-24", captured.dailyLastVisitDateKey);
            Assert.IsTrue(captured.dailyStreakBonusGranted);

            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(captured, loaded);
            Assert.AreEqual(2, loaded.Daily.LoginStreak);
            Assert.AreEqual("2026-08-24", loaded.Daily.LastVisitDateKey);
            Assert.IsTrue(loaded.Daily.StreakBonusGrantedToday);
        }

        private static List<JobData> CreateJobs()
        {
            var office = ScriptableObject.CreateInstance<JobData>();
            office.EditorSet("job_junior_office", "중소기업 신입사원", "", 0, 2_000_000, 2_000_000, 80, 20, 50, 50);
            var civil = ScriptableObject.CreateInstance<JobData>();
            civil.EditorSet("job_civil_prep", "공무원 준비생", "", 2, 1_200_000, 1_800_000, 75, 35, 45, 20);
            var freelancer = ScriptableObject.CreateInstance<JobData>();
            freelancer.EditorSet("job_freelancer", "프리랜서", "", 3, 0, 1_500_000, 70, 30, 50, 40);
            return new List<JobData> { office, civil, freelancer };
        }

        private static List<TraitData> CreateTraits()
        {
            var thrifty = ScriptableObject.CreateInstance<TraitData>();
            thrifty.EditorSet("trait_thrifty", "알뜰살뜰", "", 0);
            var healthy = ScriptableObject.CreateInstance<TraitData>();
            healthy.EditorSet("trait_healthy", "체력왕", "", 2);
            var positive = ScriptableObject.CreateInstance<TraitData>();
            positive.EditorSet("trait_positive", "긍정왕", "", 3);
            var overtime = ScriptableObject.CreateInstance<TraitData>();
            overtime.EditorSet("trait_overtime_pro", "야근전문가", "", 4);
            return new List<TraitData> { thrifty, healthy, positive, overtime };
        }

        private static List<DailyMissionData> CreateTinyPool()
        {
            var mission = ScriptableObject.CreateInstance<DailyMissionData>();
            mission.Configure(
                "m_tiny",
                "미션",
                "",
                DailyMissionGoalType.SurviveMinDays,
                0,
                1,
                null,
                10,
                0);
            return new List<DailyMissionData> { mission };
        }
    }
}
