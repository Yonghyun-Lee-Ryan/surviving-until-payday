using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class EndingEvaluatorTests
    {
        [Test]
        public void FailureEvaluator_Priority_BankruptcyFirst()
        {
            var stats = new PlayerStats(-1, 0, 100, 0, 0);
            Assert.AreEqual(FailureReason.Bankruptcy, FailureEvaluator.Evaluate(stats));
            Assert.AreEqual(4, FailureEvaluator.GetAll(stats).Count);
        }

        [Test]
        public void EndingEvaluator_PicksHighestPrioritySuccessEnding()
        {
            var cashKing = CreateSuccessEnding("cash", "현금왕", 100, requireCash: 1_000_000L);
            var barely = CreateSuccessEnding("barely", "겨우", 1, requireCash: null);
            var healthy = CreateSuccessEnding("healthy", "건강", 90, requireCash: null);
            // healthy needs health>=70 - configure via condition
            healthy.Condition.EditorSetHealth(true, 70, false, 0);
            healthy.Condition.EditorSetStress(false, 0, true, 40);

            var evaluator = new EndingEvaluator(new[] { barely, healthy, cashKing }, barely);
            var state = CreateState(cash: 2_000_000L, health: 80, stress: 20, happiness: 50, company: 50);

            var ending = evaluator.Evaluate(state, survivedToPayday: true, FailureReason.None);
            Assert.AreEqual("cash", ending.Id);
        }

        [Test]
        public void EndingEvaluator_WhenMultipleMatch_UsesPriority()
        {
            var low = CreateSuccessEnding("low", "낮음", 10, null);
            var high = CreateSuccessEnding("high", "높음", 50, null);
            // both match empty conditions
            var evaluator = new EndingEvaluator(new[] { low, high }, low);
            var state = CreateState(100_000, 50, 50, 50, 50);

            Assert.AreEqual("high", evaluator.Evaluate(state, true, FailureReason.None).Id);
        }

        [Test]
        public void EndingEvaluator_FailureEnding_ByReason()
        {
            var bankruptcy = CreateFailureEnding("broke", "파산", FailureReason.Bankruptcy, 200);
            var fired = CreateFailureEnding("fired", "해고", FailureReason.Fired, 200);
            var barely = CreateSuccessEnding("barely", "겨우", 1, null);

            var evaluator = new EndingEvaluator(new[] { bankruptcy, fired, barely }, barely);
            var state = CreateState(-10, 50, 50, 50, 50);

            Assert.AreEqual("broke", evaluator.Evaluate(state, false, FailureReason.Bankruptcy).Id);
            Assert.AreEqual("fired", evaluator.Evaluate(state, false, FailureReason.Fired).Id);
        }

        [Test]
        public void EndingEvaluator_FallsBack_WhenNoSuccessMatch()
        {
            var cashKing = CreateSuccessEnding("cash", "현금왕", 100, 1_000_000L);
            var barely = CreateSuccessEnding("barely", "겨우", 1, null);
            var evaluator = new EndingEvaluator(new[] { cashKing }, barely);
            var state = CreateState(10_000, 50, 50, 50, 50);

            // cashKing doesn't match; empty condition on barely as fallback
            Assert.AreEqual("barely", evaluator.Evaluate(state, true, FailureReason.None).Id);
        }

        [Test]
        public void EndingCodex_UnlocksOnce()
        {
            var codex = new EndingCodex();
            Assert.IsTrue(codex.TryUnlock("ending_a"));
            Assert.IsFalse(codex.TryUnlock("ending_a"));
            Assert.IsTrue(codex.IsUnlocked("ending_a"));
            Assert.AreEqual(1, codex.UnlockedCount);
        }

        [Test]
        public void ResultData_CalculatesExperience()
        {
            var ending = CreateSuccessEnding("barely", "겨우", 1, null);
            var state = CreateState(500_000, 70, 30, 60, 80);
            state.CurrentDay = 30;

            var result = ResultData.Create(state, true, FailureReason.None, ending);
            var meta = new MetaProgressionManager();
            var progress = meta.ApplyRunResult(result, null, null);
            result = result.WithMeta(progress);

            Assert.AreEqual(30, result.DaysSurvived);
            Assert.IsTrue(result.IsSuccess);
            Assert.GreaterOrEqual(result.ExperienceGained, 100);
            Assert.IsTrue(result.EndingNewlyUnlocked);
        }

        private static GameState CreateState(long cash, int health, int stress, int happiness, int company)
        {
            var state = new GameState { CurrentDay = 30 };
            state.Stats.Cash = cash;
            state.Stats.Health = health;
            state.Stats.Stress = stress;
            state.Stats.Happiness = happiness;
            state.Stats.CompanyScore = company;
            return state;
        }

        private static EndingData CreateSuccessEnding(string id, string title, int priority, long? requireCash)
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            var condition = new EndingCondition();
            if (requireCash.HasValue)
            {
                condition.EditorSetCash(true, requireCash.Value, false, 0);
            }

            ending.EditorSet(id, title, "desc", priority, false, FailureReason.None, condition);
            return ending;
        }

        private static EndingData CreateFailureEnding(
            string id,
            string title,
            FailureReason reason,
            int priority)
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet(id, title, "desc", priority, true, reason, new EndingCondition());
            return ending;
        }
    }
}
