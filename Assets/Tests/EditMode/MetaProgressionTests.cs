using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class MetaProgressionTests
    {
        [Test]
        public void PlayerLevel_IncreasesWithXpThresholds()
        {
            Assert.AreEqual(1, PlayerLevel.GetLevel(0));
            Assert.AreEqual(1, PlayerLevel.GetLevel(99));
            Assert.AreEqual(2, PlayerLevel.GetLevel(100));
            Assert.AreEqual(3, PlayerLevel.GetLevel(100 + 200));
        }

        [Test]
        public void ExperienceCalculator_AddsNewEndingBonus()
        {
            var stats = new PlayerStats(0, 50, 50, 50, 50);
            var without = ExperienceCalculator.Calculate(10, false, stats, false);
            var withBonus = ExperienceCalculator.Calculate(10, false, stats, true);
            Assert.AreEqual(without + ExperienceCalculator.NewEndingBonus, withBonus);
        }

        [Test]
        public void ExperienceCalculator_AddsNewEventAndAchievementBonus()
        {
            var stats = new PlayerStats(0, 50, 50, 50, 50);
            var baseXp = ExperienceCalculator.Calculate(10, false, stats);
            var withEvents = ExperienceCalculator.Calculate(
                10, false, stats, false, newlyUnlockedEventCount: 2);
            var withAchievements = ExperienceCalculator.Calculate(
                10, false, stats, false, 0, newlyUnlockedAchievementCount: 1);

            Assert.AreEqual(baseXp + ExperienceCalculator.NewEventBonus * 2, withEvents);
            Assert.AreEqual(baseXp + ExperienceCalculator.NewAchievementBonus, withAchievements);
        }

        [Test]
        public void Meta_DiscoverEventAndEnding_NoDuplicates()
        {
            var meta = new MetaProgressionManager();
            var notifications = new List<string>();
            meta.UnlockNotified += (cat, id, name) => notifications.Add($"{cat}:{id}");

            Assert.IsTrue(meta.DiscoverEvent("e1", "사건1"));
            Assert.IsFalse(meta.DiscoverEvent("e1", "사건1"));
            Assert.IsTrue(meta.DiscoverEnding("ending_a", "엔딩A"));
            Assert.IsFalse(meta.DiscoverEnding("ending_a", "엔딩A"));

            Assert.AreEqual(1, meta.Events.UnlockedCount);
            Assert.AreEqual(1, meta.Endings.UnlockedCount);
            Assert.AreEqual(2, notifications.Count);
        }

        [Test]
        public void Meta_ApplyRunResult_GrantsEventXpOnce()
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet("e", "엔딩", "d", 1, false, FailureReason.None, new EndingCondition());
            var draft = new ResultData(
                5,
                false,
                FailureReason.None,
                new PlayerStats(0, 50, 50, 50, 50),
                ending,
                0,
                false);

            var meta = new MetaProgressionManager();
            var first = meta.ApplyRunResult(draft, null, new[] { "event_once" });
            var xpAfterFirst = meta.TotalExperience;
            Assert.Contains("event_once", first.NewlyUnlockedEvents);
            Assert.GreaterOrEqual(
                first.ExperienceGained,
                5 * 10 + ExperienceCalculator.NewEventBonus + ExperienceCalculator.NewEndingBonus);

            var second = meta.ApplyRunResult(draft, null, new[] { "event_once" });
            Assert.IsEmpty(second.NewlyUnlockedEvents);
            Assert.IsEmpty(second.NewlyUnlockedEndings);
            Assert.AreEqual(
                xpAfterFirst + ExperienceCalculator.Calculate(5, false, draft.FinalStats, false, 0, 0),
                meta.TotalExperience);
        }

        [Test]
        public void Meta_ApplyRunResult_AchievementUnlocksOnceWithXp()
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet("e", "엔딩", "d", 1, false, FailureReason.None, new EndingCondition());
            var draft = new ResultData(
                7,
                false,
                FailureReason.None,
                new PlayerStats(0, 50, 50, 50, 50),
                ending,
                0,
                false);

            var meta = new MetaProgressionManager();
            var first = meta.ApplyRunResult(draft, null, null);
            Assert.Contains(AchievementIds.Survive7Days, first.NewlyUnlockedAchievements);
            Assert.Contains(AchievementIds.FirstEnding, first.NewlyUnlockedAchievements);
            Assert.GreaterOrEqual(
                first.ExperienceGained,
                ExperienceCalculator.NewAchievementBonus * 2);

            var second = meta.ApplyRunResult(draft, null, null);
            Assert.IsEmpty(second.NewlyUnlockedAchievements);
            Assert.IsTrue(meta.Achievements.IsUnlocked(AchievementIds.Survive7Days));
            Assert.IsTrue(meta.Achievements.IsUnlocked(AchievementIds.FirstEnding));
        }

        [Test]
        public void Meta_UnlocksTrait_WhenLevelReached()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_gym", "체력왕", "체력 증가", 2);

            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet("e", "엔딩", "d", 1, false, FailureReason.None, new EndingCondition());

            var draft = new ResultData(
                30,
                true,
                FailureReason.None,
                new PlayerStats(1_000_000, 80, 20, 80, 80),
                ending,
                0,
                false);

            var meta = new MetaProgressionManager();
            var result = meta.ApplyRunResult(draft, new[] { trait }, new[] { "event_x" });

            Assert.GreaterOrEqual(result.LevelAfter, 2);
            Assert.IsTrue(meta.IsTraitUnlocked(trait));
            Assert.Contains("event_x", result.NewlyUnlockedEvents);
            Assert.Contains("e", result.NewlyUnlockedEndings);
            Assert.Contains("trait_gym", result.NewlyUnlockedTraits);
        }

        [Test]
        public void Meta_RegistersStarterTrait_InCodexOnce()
        {
            var starter = ScriptableObject.CreateInstance<TraitData>();
            starter.EditorSet("trait_thrifty", "짠돌이", "기본", 0);

            var draft = new ResultData(
                1,
                false,
                FailureReason.None,
                new PlayerStats(0, 50, 50, 50, 50),
                null,
                0,
                false);

            var meta = new MetaProgressionManager();
            var first = meta.ApplyRunResult(draft, new[] { starter }, null);
            Assert.Contains("trait_thrifty", first.NewlyUnlockedTraits);

            var second = meta.ApplyRunResult(draft, new[] { starter }, null);
            Assert.IsFalse(second.NewlyUnlockedTraits.Contains("trait_thrifty"));
            Assert.AreEqual(1, meta.Traits.UnlockedCount);
        }

        [Test]
        public void Meta_SaveRoundTrip_PreservesCodex()
        {
            var meta = new MetaProgressionManager();
            meta.DiscoverEnding("ending_a");
            meta.DiscoverEvent("event_b");
            meta.Load(300, new[] { "ending_a" }, new[] { "event_b" }, null, null, new[] { "job_civil_prep" }, 2);
            meta.ApplyRunResult(
                new ResultData(7, false, FailureReason.None, new PlayerStats(10_000, 50, 50, 50, 50), null, 0, false),
                null,
                null);

            var saveMeta = SaveMapper.CaptureMeta(meta);
            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(saveMeta, loaded);

            Assert.IsTrue(loaded.Endings.IsUnlocked("ending_a"));
            Assert.IsTrue(loaded.Events.IsUnlocked("event_b"));
            Assert.IsTrue(loaded.Jobs.IsUnlocked("job_civil_prep"));
            Assert.AreEqual(meta.TotalExperience, loaded.TotalExperience);
            Assert.AreEqual(meta.TraitFragmentCount, loaded.TraitFragmentCount);
        }

        [Test]
        public void Meta_IsJobUnlocked_RespectsUnlockLevelGate()
        {
            var starter = ScriptableObject.CreateInstance<JobData>();
            starter.EditorSet(
                "job_junior_office",
                "신입",
                "기본",
                0,
                2_800_000L,
                2_800_000L,
                80,
                20,
                50,
                50);

            var civil = ScriptableObject.CreateInstance<JobData>();
            civil.EditorSet(
                "job_civil_prep",
                "공준",
                "레벨2",
                2,
                1_200_000L,
                1_800_000L,
                75,
                35,
                45,
                20);

            var freelance = ScriptableObject.CreateInstance<JobData>();
            freelance.EditorSet(
                "job_freelancer",
                "프리",
                "레벨3",
                3,
                2_200_000L,
                2_400_000L,
                70,
                30,
                55,
                15);

            var meta = new MetaProgressionManager();
            Assert.IsTrue(meta.IsJobUnlocked(starter));
            Assert.IsFalse(meta.IsJobUnlocked(civil));
            Assert.IsFalse(meta.IsJobUnlocked(freelance));

            meta.Load(100, null, null, null, null, null);
            Assert.AreEqual(2, meta.Level);
            Assert.IsTrue(meta.IsJobUnlocked(civil));
            Assert.IsFalse(meta.IsJobUnlocked(freelance));

            meta.Load(100 + 200, null, null, null, null, null);
            Assert.AreEqual(3, meta.Level);
            Assert.IsTrue(meta.IsJobUnlocked(freelance));
        }

        [Test]
        public void Meta_ApplyRunResult_UnlocksEligibleJobs()
        {
            var civil = ScriptableObject.CreateInstance<JobData>();
            civil.EditorSet(
                "job_civil_prep",
                "공준",
                "레벨2",
                2,
                1_200_000L,
                1_800_000L,
                75,
                35,
                45,
                20);

            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet("e", "엔딩", "d", 1, false, FailureReason.None, new EndingCondition());

            var draft = new ResultData(
                30,
                true,
                FailureReason.None,
                new PlayerStats(1_000_000, 80, 20, 80, 80),
                ending,
                0,
                false);

            var meta = new MetaProgressionManager();
            var result = meta.ApplyRunResult(draft, null, null, new[] { civil });

            Assert.GreaterOrEqual(result.LevelAfter, 2);
            Assert.IsTrue(meta.IsJobUnlocked(civil));
            Assert.Contains("job_civil_prep", result.NewlyUnlockedJobs);
        }
    }
}
