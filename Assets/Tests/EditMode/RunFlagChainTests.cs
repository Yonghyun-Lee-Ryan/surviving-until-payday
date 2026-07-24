using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    /// <summary>
    /// 개발 단위 20: 연쇄·조건부 사건 플래그·큐·엔딩 연동 검증.
    /// </summary>
    public sealed class RunFlagChainTests
    {
        [Test]
        public void EventCondition_RequiredFlag_BlocksWithoutAndAllowsWith()
        {
            var condition = new EventCondition();
            condition.EditorSetFlags(new[] { RunFlags.HasBoughtStock }, null);

            var state = CreateState(day: 10);

            Assert.IsFalse(EventConditionEvaluator.Matches(condition, state, isWeekend: false));

            state.SetFlag(RunFlags.HasBoughtStock);
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, state, isWeekend: false));
        }

        [Test]
        public void EventCondition_ForbiddenFlag_BlocksWhenPresent()
        {
            var condition = new EventCondition();
            condition.EditorSetFlags(null, new[] { RunFlags.OwesDebt });

            var state = CreateState(day: 5);
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, state, isWeekend: false));

            state.SetFlag(RunFlags.OwesDebt);
            Assert.IsFalse(EventConditionEvaluator.Matches(condition, state, isWeekend: false));
        }

        [Test]
        public void EffectResolver_SetsFlagAndQueuesFollowUp_EventSelectorReturnsQueuedEvent()
        {
            var state = CreateState(day: 8);
            var days = new DayManager(state);
            var history = new RunHistory();
            var intro = CreateStockIntroEvent();
            var swing = CreateStockSwingEvent();
            var fallback = CreateRestFallback();

            // roll 70 → stock_small_down (weight 60 up / 40 down, total 100)
            var resolver = new EffectResolver(state, new ScriptedRandomService(70), history, days);
            resolver.BeginEvent(intro);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out var error), error);

            Assert.IsTrue(state.HasFlag(RunFlags.HasBoughtStock));
            CollectionAssert.Contains(state.QueuedFollowUpEventIds, "event_stock_swing_001");

            var selector = new EventSelector(
                new[] { intro, swing, fallback },
                fallback,
                new SeededRandomService(1));

            var selected = selector.Select(state, isWeekend: false);
            Assert.AreEqual("event_stock_swing_001", selected.Id);
        }

        [Test]
        public void SaveMapper_CaptureRunFlagsAndQueue_RoundTripsThroughToGameState()
        {
            var state = CreateState(day: 11);
            state.SetFlag(RunFlags.HasBoughtStock);
            state.SetFlag(RunFlags.OwesDebt);
            state.EnqueueFollowUp("event_stock_swing_001");
            state.EnqueueFollowUp("event_phone_rebreak_001");

            var fallback = CreateRestFallback();
            var random = new SeededRandomService(7);
            var selector = new EventSelector(new[] { fallback }, fallback, random);

            var run = SaveMapper.CaptureRun(state, random, selector, pendingEventId: string.Empty);
            var restored = SaveMapper.ToGameState(run);

            CollectionAssert.AreEquivalent(
                new[] { RunFlags.HasBoughtStock, RunFlags.OwesDebt },
                restored.RunFlags);
            CollectionAssert.AreEqual(
                new[] { "event_stock_swing_001", "event_phone_rebreak_001" },
                restored.QueuedFollowUpEventIds);
        }

        [Test]
        public void EndingConditionMatcher_CardJuggleStyle_RequiresOwesDebt()
        {
            var condition = new EndingCondition();
            condition.EditorSetHappiness(true, 40, false, 0);
            condition.EditorSetFlags(new[] { RunFlags.OwesDebt });

            var statsOk = new PlayerStats { Happiness = 55 };
            var stateWithoutDebt = CreateState(day: 30);
            stateWithoutDebt.Stats.Happiness = 55;

            var stateWithDebt = CreateState(day: 30);
            stateWithDebt.Stats.Happiness = 55;
            stateWithDebt.SetFlag(RunFlags.OwesDebt);

            Assert.IsTrue(EndingConditionMatcher.Matches(condition, statsOk));
            Assert.IsFalse(EndingConditionMatcher.Matches(condition, stateWithoutDebt));
            Assert.IsTrue(EndingConditionMatcher.Matches(condition, stateWithDebt));
        }

        private static GameState CreateState(int day)
        {
            var state = new GameState { CurrentDay = day, JobId = "job_test" };
            state.Stats.Health = 80;
            state.Stats.Stress = 20;
            state.Stats.Happiness = 50;
            state.Stats.CompanyScore = 50;
            state.Stats.Cash = 500_000L;
            return state;
        }

        private static EventData CreateStockIntroEvent()
        {
            var buyFlags = new List<string> { RunFlags.HasBoughtStock };
            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_stock_intro_001",
                "주식 입문",
                "동료가 요즘 뜨는 주식을 추천했다.",
                EventCategory.Opportunity,
                7,
                26,
                70,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "choice_stock_small",
                        "소액만 안전하게 투자해본다",
                        new List<StatEffect> { new StatEffect(StatType.Cash, -50_000L) },
                        new List<RandomOutcome>
                        {
                            new RandomOutcome(
                                "stock_small_up",
                                "약간 올라 소소하게 벌었다.",
                                60,
                                new StatEffect(StatType.Cash, 20_000L)),
                            new RandomOutcome(
                                "stock_small_down",
                                "약간 내려 아쉽게 잃었다.",
                                40,
                                new StatEffect[]
                                {
                                    new StatEffect(StatType.Cash, -10_000L),
                                    new StatEffect(StatType.Stress, 2)
                                },
                                null,
                                null,
                                "event_stock_swing_001")
                        },
                        buyFlags),
                    new EventChoiceData("choice_stock_watch", "투자하지 않고 지켜본다"),
                    new EventChoiceData("choice_stock_allin", "가진 돈을 크게 넣는다")
                });
            return eventData;
        }

        private static EventData CreateStockSwingEvent()
        {
            var conditions = new EventCondition();
            conditions.EditorSetFlags(new[] { RunFlags.HasBoughtStock }, null);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_stock_swing_001",
                "주식 급등락",
                "예전에 산 종목이 요동친다.",
                EventCategory.Opportunity,
                8,
                28,
                120,
                conditions,
                new List<EventChoiceData>
                {
                    new EventChoiceData("choice_stock_swing_hold", "그냥 들고 간다"),
                    new EventChoiceData("choice_stock_swing_sell", "수익 실현하고 판다"),
                    new EventChoiceData("choice_stock_swing_cut", "손절하고 판다")
                });
            return eventData;
        }

        private static EventData CreateRestFallback()
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_rest_fallback",
                "여유로운 하루",
                "특별히 급한 일은 없다.",
                EventCategory.Rest,
                1,
                30,
                80,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData("rest_home", "집에서 쉰다"),
                    new EventChoiceData("rest_walk", "산책한다"),
                    new EventChoiceData("rest_hobby", "취미를 즐긴다")
                });
            return eventData;
        }

        private sealed class ScriptedRandomService : IRandomService
        {
            private readonly Queue<int> values;

            public ScriptedRandomService(params int[] scriptedValues)
            {
                values = new Queue<int>(scriptedValues);
            }

            public int Seed => 0;

            public int Next(int maxExclusive)
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException("ScriptedRandomService exhausted.");
                }

                var value = values.Dequeue();
                if (value < 0 || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Scripted value {value} is outside [0, {maxExclusive}).");
                }

                return value;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                return minInclusive + Next(maxExclusive - minInclusive);
            }

            public float NextFloat()
            {
                return Next(1000) / 1000f;
            }
        }
    }
}
