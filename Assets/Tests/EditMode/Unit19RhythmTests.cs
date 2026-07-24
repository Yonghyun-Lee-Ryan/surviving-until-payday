using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit19RhythmTests
    {
        [Test]
        public void LateCrisisDays_Are_28_And_29()
        {
            Assert.IsTrue(DayManager.IsLateCrisisDay(28));
            Assert.IsTrue(DayManager.IsLateCrisisDay(29));
            Assert.IsFalse(DayManager.IsLateCrisisDay(27));
            Assert.IsFalse(DayManager.IsLateCrisisDay(30));

            var state = new GameState { CurrentDay = 28 };
            var days = new DayManager(state);
            Assert.IsTrue(days.IsLateCrisisDay());
        }

        [Test]
        public void DifficultyScaler_ScaleCashDelta_OnlyAmplifiesLosses()
        {
            Assert.AreEqual(-150_000L, DifficultyScaler.ScaleCashDelta(-100_000L, 1.5f));
            Assert.AreEqual(50_000L, DifficultyScaler.ScaleCashDelta(50_000L, 1.5f));
            Assert.AreEqual(-100_000L, DifficultyScaler.ScaleCashDelta(-100_000L, 1f));
        }

        [Test]
        public void EffectResolver_AppliesDifficultyToCashLoss_OnLateDays()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var state = GameState.CreateFromJob(job, null, 11);
            state.CurrentDay = 28;
            var cashBefore = state.Stats.Cash;
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(11), new RunHistory(), days);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_bill",
                "청구서",
                "desc",
                EventCategory.FixedExpense,
                1,
                30,
                100,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "pay",
                        "낸다",
                        new List<StatEffect> { new StatEffect(StatType.Cash, -100_000L) })
                });

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out var error), error);
            Assert.AreEqual(cashBefore - 150_000L, state.Stats.Cash);
            Assert.AreEqual(1.5f, days.DifficultyMultiplier, 0.0001f);
        }

        [Test]
        public void EffectResolver_DoesNotScaleCashGains()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var state = GameState.CreateFromJob(job, null, 3);
            state.CurrentDay = 28;
            var cashBefore = state.Stats.Cash;
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(3), new RunHistory(), days);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_bonus",
                "보너스",
                "desc",
                EventCategory.Opportunity,
                1,
                30,
                100,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "take",
                        "받는다",
                        new List<StatEffect> { new StatEffect(StatType.Cash, 40_000L) })
                });

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out var error), error);
            Assert.AreEqual(cashBefore + 40_000L, state.Stats.Cash);
        }

        [Test]
        public void WeeklySummaryFormatter_BuildsTitleAndWarnings()
        {
            var state = new GameState { CurrentDay = 7 };
            state.Stats.CopyFrom(new PlayerStats(100_000L, 20, 80, 20, 25));
            var info = new WeeklySummaryInfo(1, 7, state);

            Assert.AreEqual("1주차 결산", WeeklySummaryFormatter.BuildTitle(info));
            StringAssert.Contains("현금", WeeklySummaryFormatter.BuildBody(info));
            StringAssert.Contains("스트레스", WeeklySummaryFormatter.BuildWarnings(info));
        }

        [Test]
        public void PaydaySalary_AddsSalaryToCash()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var state = GameState.CreateFromJob(job, null, 1);
            state.CurrentDay = 30;
            var before = state.Stats.Cash;
            var salary = state.Salary;
            Assert.Greater(salary, 0L);

            state.ApplyEffect(new StatEffect(StatType.Cash, salary));
            Assert.AreEqual(before + salary, state.Stats.Cash);
        }
    }
}
