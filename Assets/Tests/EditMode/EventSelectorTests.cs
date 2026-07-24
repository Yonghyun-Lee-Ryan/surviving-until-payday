using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class EventSelectorTests
    {
        [Test]
        public void Select_FiltersByDayRange()
        {
            var early = CreateEvent("early", minDay: 1, maxDay: 5, weight: 100);
            var late = CreateEvent("late", minDay: 10, maxDay: 20, weight: 100);
            var fallback = CreateRestFallback();
            var selector = new EventSelector(new[] { early, late }, fallback, new SeededRandomService(1));
            var state = CreateState(day: 3);

            Assert.AreEqual("early", selector.Select(state, isWeekend: false).Id);
        }

        [Test]
        public void Select_FiltersByStressCondition()
        {
            var lowStressOnly = CreateEvent("calm", minDay: 1, maxDay: 30, weight: 100);
            lowStressOnly.Conditions.EditorConfigure(newMaxStress: 30);

            var any = CreateEvent("any", minDay: 1, maxDay: 30, weight: 100);
            var fallback = CreateRestFallback();
            var selector = new EventSelector(new[] { lowStressOnly, any }, fallback, new SeededRandomService(7));

            var state = CreateState(day: 5);
            state.Stats.Stress = 80;

            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual("any", selector.Select(state, false).Id);
            }
        }

        [Test]
        public void Select_FiltersByJobAndWeekend()
        {
            var weekendEvent = CreateEvent("weekend_party", 1, 30, 100);
            weekendEvent.Conditions.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekendOnly);

            var jobEvent = CreateEvent("job_only", 1, 30, 100);
            jobEvent.Conditions.EditorConfigure(newRequiredJobId: "job_a");

            var fallback = CreateRestFallback();
            var selector = new EventSelector(
                new[] { weekendEvent, jobEvent },
                fallback,
                new SeededRandomService(3));

            var state = CreateState(day: 6);
            state.JobId = "job_b";

            Assert.AreEqual(fallback.Id, selector.Select(state, isWeekend: false).Id);

            state.JobId = "job_a";
            Assert.AreEqual("job_only", selector.Select(state, isWeekend: false).Id);

            state.JobId = "job_b";
            Assert.AreEqual("weekend_party", selector.Select(state, isWeekend: true).Id);
        }

        [Test]
        public void Select_FixedEvent_HasPriorityOnMatchingDay()
        {
            var normal = CreateEvent("normal", 1, 30, 1000);
            var fixedRent = CreateEvent("rent", 1, 30, 1);
            fixedRent.EditorSetFixed(true, 15);

            var fallback = CreateRestFallback();
            var selector = new EventSelector(new[] { normal, fixedRent }, fallback, new SeededRandomService(99));
            var state = CreateState(day: 15);

            Assert.AreEqual("rent", selector.Select(state, false).Id);

            state.CurrentDay = 16;
            Assert.AreEqual("normal", selector.Select(state, false).Id);
        }

        [Test]
        public void Select_SameSeed_ProducesSameSequence()
        {
            var catalog = new[]
            {
                CreateEvent("a", 1, 30, 100),
                CreateEvent("b", 1, 30, 100),
                CreateEvent("c", 1, 30, 100)
            };
            var fallback = CreateRestFallback();

            var sequence1 = DrawSequence(catalog, fallback, seed: 12345, count: 12);
            var sequence2 = DrawSequence(catalog, fallback, seed: 12345, count: 12);

            CollectionAssert.AreEqual(sequence1, sequence2);
        }

        [Test]
        public void Select_DifferentSeed_CanProduceDifferentSequence()
        {
            var catalog = new[]
            {
                CreateEvent("a", 1, 30, 100),
                CreateEvent("b", 1, 30, 100),
                CreateEvent("c", 1, 30, 100)
            };
            var fallback = CreateRestFallback();

            var sequence1 = DrawSequence(catalog, fallback, seed: 1, count: 20);
            var sequence2 = DrawSequence(catalog, fallback, seed: 2, count: 20);

            CollectionAssert.AreNotEqual(sequence1, sequence2);
        }

        [Test]
        public void Select_DoesNotRepeatImmediately_WhenAlternativesExist()
        {
            var catalog = new[]
            {
                CreateEvent("a", 1, 30, 100),
                CreateEvent("b", 1, 30, 100)
            };
            var fallback = CreateRestFallback();
            var selector = new EventSelector(catalog, fallback, new SeededRandomService(42));
            var state = CreateState(1);

            var previous = selector.Select(state, false).Id;
            for (var i = 0; i < 30; i++)
            {
                state.CurrentDay = Math.Min(30, i + 1);
                var next = selector.Select(state, false).Id;
                Assert.AreNotEqual(previous, next, $"Consecutive repeat at step {i}");
                previous = next;
            }
        }

        [Test]
        public void Select_ReturnsFallback_WhenNoCandidates()
        {
            var onlyLate = CreateEvent("late", 20, 30, 100);
            var fallback = CreateRestFallback();
            var selector = new EventSelector(new[] { onlyLate }, fallback, new SeededRandomService(1));

            Assert.AreEqual("event_rest_fallback", selector.Select(CreateState(day: 2), false).Id);
        }

        [Test]
        public void WeightedSelect_UsesRollAgainstCumulativeWeights()
        {
            // weight: a=10, b=90, total=100
            // roll 0..9 => a, roll 10..99 => b
            var a = CreateEvent("a", 1, 30, 10);
            var b = CreateEvent("b", 1, 30, 90);
            var fallback = CreateRestFallback();

            var pickA = new EventSelector(
                new[] { a, b },
                fallback,
                new ScriptedRandomService(0),
                recentHistorySize: 0,
                recentWeightMultiplier: 1f);
            Assert.AreEqual("a", pickA.Select(CreateState(1), false).Id);

            var pickB = new EventSelector(
                new[] { a, b },
                fallback,
                new ScriptedRandomService(10),
                recentHistorySize: 0,
                recentWeightMultiplier: 1f);
            Assert.AreEqual("b", pickB.Select(CreateState(1), false).Id);
        }

        [Test]
        public void SeededRandomService_IsDeterministic()
        {
            var randomA = new SeededRandomService(99);
            var randomB = new SeededRandomService(99);

            for (var i = 0; i < 50; i++)
            {
                Assert.AreEqual(randomA.Next(1000), randomB.Next(1000));
                Assert.AreEqual(randomA.NextFloat(), randomB.NextFloat(), 0.000001f);
            }
        }

        [Test]
        public void ConditionEvaluator_RejectsOutOfRangeCompanyScore()
        {
            var condition = new EventCondition();
            condition.EditorConfigure(newMinCompanyScore: 40, newMaxCompanyScore: 70);

            var state = CreateState(1);
            state.Stats.CompanyScore = 20;

            Assert.IsFalse(EventConditionEvaluator.Matches(condition, state, false));

            state.Stats.CompanyScore = 55;
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, state, false));
        }

        private static List<string> DrawSequence(
            EventData[] catalog,
            EventData fallback,
            int seed,
            int count)
        {
            var selector = new EventSelector(catalog, fallback, new SeededRandomService(seed));
            var state = CreateState(1);
            var result = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                state.CurrentDay = (i % 30) + 1;
                result.Add(selector.Select(state, false).Id);
            }

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

        private static EventData CreateRestFallback()
        {
            return CreateEvent("event_rest_fallback", 1, 30, 100, EventCategory.Rest, "집에서 쉬기");
        }

        private static EventData CreateEvent(
            string id,
            int minDay,
            int maxDay,
            int weight,
            EventCategory category = EventCategory.Work,
            string title = null)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData("c1", "선택1"),
                new EventChoiceData("c2", "선택2"),
                new EventChoiceData("c3", "선택3")
            };

            eventData.EditorSetCore(
                id,
                title ?? id,
                "test description",
                category,
                minDay,
                maxDay,
                weight,
                new EventCondition(),
                choices);

            return eventData;
        }

        /// <summary>
        /// Next(int maxExclusive)만 순서대로 반환하는 테스트용 난수.
        /// </summary>
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
