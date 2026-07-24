using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class EffectResolverTests
    {
        [Test]
        public void TryResolveChoice_AppliesFixedEffects()
        {
            var state = CreateState(1);
            var days = new DayManager(state);
            var history = new RunHistory();
            var resolver = new EffectResolver(state, new SeededRandomService(1), history, days);
            var eventData = CreateEventWithFixedChoice(
                "event_ot",
                new StatEffect(StatType.Health, -5),
                new StatEffect(StatType.Stress, 12),
                new StatEffect(StatType.CompanyScore, 10));

            resolver.BeginEvent(eventData);

            Assert.IsTrue(resolver.TryResolveChoice(0, out var result, out var error), error);
            Assert.AreEqual(75, state.Stats.Health);
            Assert.AreEqual(32, state.Stats.Stress);
            Assert.AreEqual(60, state.Stats.CompanyScore);
            Assert.AreEqual("event_ot", result.EventId);
            Assert.AreEqual(0, result.ChoiceIndex);
            Assert.AreEqual(1, history.Count);
            Assert.IsTrue(days.ReadyForNextDay);
            Assert.AreEqual(ChoicePhase.ResultReady, resolver.Phase);
        }

        [Test]
        public void TryResolveChoice_LocksDuplicateSelection()
        {
            var state = CreateState(1);
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(1), new RunHistory(), days);
            var eventData = CreateEventWithFixedChoice("e1", new StatEffect(StatType.Happiness, 1));

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out _));
            Assert.IsFalse(resolver.CanSelectChoice);
            Assert.IsFalse(resolver.TryResolveChoice(1, out _, out var error));
            Assert.IsTrue(error.Contains("already resolved"));
            Assert.AreEqual(51, state.Stats.Happiness);
        }

        [Test]
        public void TryResolveChoice_RandomOutcome_IsSeedDeterministic()
        {
            var eventData = CreatePhoneRepairStyleEvent();

            var resultA = ResolveRandomOnce(eventData, seed: 42);
            var resultB = ResolveRandomOnce(eventData, seed: 42);
            var resultC = ResolveRandomOnce(eventData, seed: 7);

            Assert.AreEqual(resultA.RandomOutcomeId, resultB.RandomOutcomeId);
            Assert.AreEqual(resultA.Message, resultB.Message);
            // 다른 시드는 달라질 수 있음(동일할 수도 있으나 여러 번 중 하나는 다른 경우가 많음)
            // 동일 시드 재현만 강하게 검증하고, 다른 시드는 최소 한 번은 다른 결과가 나오는지 여러 시드로 확인
            var seen = new HashSet<string> { resultA.RandomOutcomeId };
            for (var seed = 0; seed < 40; seed++)
            {
                seen.Add(ResolveRandomOnce(eventData, seed).RandomOutcomeId);
            }

            Assert.Greater(seen.Count, 1, "Expected multiple random outcomes across seeds.");
        }

        [Test]
        public void TryResolveChoice_RecordsHistoryWithBeforeAfterStats()
        {
            var state = CreateState(3);
            state.Stats.Cash = 200_000L;
            var days = new DayManager(state);
            var history = new RunHistory();
            var resolver = new EffectResolver(state, new SeededRandomService(1), history, days);
            var eventData = CreateEventWithFixedChoice(
                "event_cash",
                new StatEffect(StatType.Cash, -50_000L));

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out var result, out _));

            Assert.AreEqual(200_000L, result.StatsBefore.Cash);
            Assert.AreEqual(150_000L, result.StatsAfter.Cash);
            Assert.AreEqual(3, result.Day);
            Assert.IsTrue(history.TryGetLast(out var last));
            Assert.AreEqual(result.EventId, last.EventId);
        }

        [Test]
        public void RunManager_RejectsAdvance_BeforeChoiceResolved()
        {
            var state = CreateState(1);
            var run = new RunManager();
            run.StartRunWithState(state);

            var history = new RunHistory();
            var resolver = new EffectResolver(state, new SeededRandomService(1), history, run.Days);
            var eventData = CreateEventWithFixedChoice("e", new StatEffect(StatType.Stress, 1));
            resolver.BeginEvent(eventData);

            var rejected = run.TryCompleteCurrentDayAfterChoice(resolver);
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(1, run.State.CurrentDay);

            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out _));
            var advanced = run.TryCompleteCurrentDayAfterChoice(resolver);
            Assert.IsTrue(advanced.Accepted);
            Assert.AreEqual(2, run.State.CurrentDay);
            Assert.AreEqual(ChoicePhase.NoActiveEvent, resolver.Phase);
            Assert.IsFalse(run.Days.ReadyForNextDay);
        }

        [Test]
        public void TryResolveChoice_InvalidIndex_FailsWithoutLocking()
        {
            var state = CreateState(1);
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(1), new RunHistory(), days);
            var eventData = CreateEventWithFixedChoice("e", new StatEffect(StatType.Stress, 1));
            resolver.BeginEvent(eventData);

            Assert.IsFalse(resolver.TryResolveChoice(99, out _, out var error));
            Assert.IsTrue(error.Contains("Invalid choiceIndex"));
            Assert.IsTrue(resolver.CanSelectChoice);
            Assert.IsFalse(days.ReadyForNextDay);
        }

        [Test]
        public void WeightedRandomOutcome_UsesScriptedRoll()
        {
            // outcomes: 70 / 20 / 10
            var eventData = CreatePhoneRepairStyleEvent();
            var state = CreateState(1);
            var days = new DayManager(state);
            var resolver = new EffectResolver(
                state,
                new ScriptedRandomService(0),
                new RunHistory(),
                days);

            resolver.BeginEvent(eventData);
            // choice index 1 = 사설 수리 (random outcomes)
            Assert.IsTrue(resolver.TryResolveChoice(1, out var result, out _));
            Assert.AreEqual("phone_ok", result.RandomOutcomeId);

            var state2 = CreateState(1);
            var days2 = new DayManager(state2);
            var resolver2 = new EffectResolver(
                state2,
                new ScriptedRandomService(70),
                new RunHistory(),
                days2);
            resolver2.BeginEvent(eventData);
            Assert.IsTrue(resolver2.TryResolveChoice(1, out var result2, out _));
            Assert.AreEqual("phone_fail_again", result2.RandomOutcomeId);
        }

        private static ChoiceResult ResolveRandomOnce(EventData eventData, int seed)
        {
            var state = CreateState(1);
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(seed), new RunHistory(), days);
            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(1, out var result, out var error), error);
            return result;
        }

        private static GameState CreateState(int day)
        {
            var state = new GameState
            {
                CurrentDay = day,
                JobId = "job_junior_office"
            };
            state.Stats.Cash = 1_000_000L;
            state.Stats.Health = 80;
            state.Stats.Stress = 20;
            state.Stats.Happiness = 50;
            state.Stats.CompanyScore = 50;
            return state;
        }

        private static EventData CreateEventWithFixedChoice(string id, params StatEffect[] effects)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData("c0", "선택 A", new List<StatEffect>(effects)),
                new EventChoiceData("c1", "선택 B"),
                new EventChoiceData("c2", "선택 C")
            };

            eventData.EditorSetCore(
                id,
                id,
                "desc",
                EventCategory.Work,
                1,
                30,
                100,
                new EventCondition(),
                choices);
            return eventData;
        }

        private static EventData CreatePhoneRepairStyleEvent()
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "official",
                    "공식",
                    new List<StatEffect> { new StatEffect(StatType.Cash, -280_000L) }),
                new EventChoiceData(
                    "private",
                    "사설 수리점",
                    new List<StatEffect> { new StatEffect(StatType.Cash, -110_000L) },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome("phone_ok", "정상 수리", 70),
                        new RandomOutcome(
                            "phone_fail_again",
                            "다시 고장",
                            20,
                            new StatEffect(StatType.Stress, 8)),
                        new RandomOutcome(
                            "phone_data_loss",
                            "데이터 손실",
                            10,
                            new StatEffect(StatType.Happiness, -10))
                    }),
                new EventChoiceData(
                    "ignore",
                    "그냥 사용",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 10),
                        new StatEffect(StatType.Happiness, -5)
                    })
            };

            eventData.EditorSetCore(
                "event_phone",
                "액정 파손",
                "desc",
                EventCategory.Accident,
                1,
                30,
                100,
                new EventCondition(),
                choices);
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
                var value = values.Dequeue();
                if (value < 0 || value >= maxExclusive)
                {
                    throw new InvalidOperationException($"Scripted value {value} outside [0,{maxExclusive}).");
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
