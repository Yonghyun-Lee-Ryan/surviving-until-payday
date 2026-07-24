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
        }

        [Test]
        public void Meta_SaveRoundTrip_PreservesCodex()
        {
            var meta = new MetaProgressionManager();
            meta.DiscoverEnding("ending_a");
            meta.DiscoverEvent("event_b");
            meta.ApplyRunResult(
                new ResultData(7, false, FailureReason.None, new PlayerStats(10_000, 50, 50, 50, 50), null, 0, false),
                null,
                null);

            var saveMeta = SaveMapper.CaptureMeta(meta);
            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(saveMeta, loaded);

            Assert.IsTrue(loaded.Endings.IsUnlocked("ending_a"));
            Assert.IsTrue(loaded.Events.IsUnlocked("event_b"));
            Assert.AreEqual(meta.TotalExperience, loaded.TotalExperience);
        }
    }
}
