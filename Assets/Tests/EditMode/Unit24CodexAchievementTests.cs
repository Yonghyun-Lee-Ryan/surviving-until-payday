using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit24CodexAchievementTests
    {
        [Test]
        public void AchievementCatalog_HasTwentyDefinitions()
        {
            Assert.AreEqual(20, AchievementIds.Catalog.Count);
            Assert.AreEqual(20, AchievementIds.CatalogCount);
            Assert.AreEqual("첫 엔딩", AchievementIds.GetDisplayName(AchievementIds.FirstEnding));
        }

        [Test]
        public void Meta_GrantsTraitFragment_OnAchievementUnlock()
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet("e", "엔딩", "d", 1, false, FailureReason.None, new EndingCondition());
            var draft = new ResultData(
                7,
                false,
                FailureReason.None,
                new PlayerStats(10_000, 50, 50, 50, 50),
                ending,
                0,
                false);

            var meta = new MetaProgressionManager();
            var result = meta.ApplyRunResult(draft, null, null);
            Assert.Greater(result.NewlyUnlockedAchievements.Count, 0);
            Assert.AreEqual(result.NewlyUnlockedAchievements.Count, result.TraitFragmentsGained);
            Assert.AreEqual(result.TraitFragmentsGained, meta.TraitFragmentCount);
        }

        [Test]
        public void Meta_SaveRoundTrip_PreservesTraitFragments()
        {
            var meta = new MetaProgressionManager();
            meta.Load(50, null, null, null, null, null, traitFragmentCount: 4);
            var save = SaveMapper.CaptureMeta(meta);
            Assert.AreEqual(4, save.traitFragmentCount);

            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(save, loaded);
            Assert.AreEqual(4, loaded.TraitFragmentCount);
        }

        [Test]
        public void EndingEvaluator_ResignReady_WhenCashHighAndCompanyLow()
        {
            var resign = ScriptableObject.CreateInstance<EndingData>();
            var condition = new EndingCondition();
            condition.EditorSetCash(true, 800_000L, false, 0);
            condition.EditorSetCompanyScore(false, 0, true, 35);
            resign.EditorSet(
                "ending_resign_ready",
                "퇴사 준비 완료",
                "d",
                72,
                false,
                FailureReason.None,
                condition);

            var barely = ScriptableObject.CreateInstance<EndingData>();
            barely.EditorSet(
                "ending_barely_survived",
                "겨우",
                "d",
                1,
                false,
                FailureReason.None,
                new EndingCondition());

            var evaluator = new EndingEvaluator(new[] { barely, resign }, barely);
            var state = new GameState { CurrentDay = 30 };
            state.Stats.Cash = 900_000L;
            state.Stats.Health = 50;
            state.Stats.Stress = 50;
            state.Stats.Happiness = 50;
            state.Stats.CompanyScore = 20;

            Assert.AreEqual("ending_resign_ready", evaluator.Evaluate(state, true, FailureReason.None).Id);
        }

        [Test]
        public void Meta_UnlocksResignReadyAchievement_WhenThatEndingReached()
        {
            var resign = ScriptableObject.CreateInstance<EndingData>();
            resign.EditorSet(
                "ending_resign_ready",
                "퇴사 준비 완료",
                "d",
                72,
                false,
                FailureReason.None,
                new EndingCondition());

            var draft = new ResultData(
                30,
                true,
                FailureReason.None,
                new PlayerStats(900_000, 50, 50, 50, 20),
                resign,
                0,
                false);

            var meta = new MetaProgressionManager();
            var result = meta.ApplyRunResult(draft, null, null);
            Assert.Contains(AchievementIds.ResignReadyEnding, result.NewlyUnlockedAchievements);
            Assert.IsTrue(meta.Achievements.IsUnlocked(AchievementIds.ResignReadyEnding));
        }
    }
}
