using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class RunSimulatorTests
    {
        [Test]
        public void RunOnce_FirstChoice_CompletesWithoutThrowing()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var rest = CreateRestEvent("rest");
            var ending = CreateSuccessEnding("barely");
            var simulator = new RunSimulator(
                job,
                null,
                new List<EventData> { rest },
                rest,
                new List<EndingData> { ending },
                ending);

            var result = simulator.RunOnce(seed: 11, SimulatorChoicePolicy.Safe);

            Assert.IsNotNull(result);
            Assert.GreaterOrEqual(result.DaysSurvived, 1);
            Assert.LessOrEqual(result.DaysSurvived, GameState.MaxDay);
            Assert.IsNotNull(result.Ending);
        }

        [Test]
        public void Run_SameSeedAndPolicy_IsDeterministic()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var rest = CreateRestEvent("rest");
            var ending = CreateSuccessEnding("barely");
            var simulator = new RunSimulator(
                job,
                null,
                new List<EventData> { rest },
                rest,
                new List<EndingData> { ending },
                ending);

            var a = simulator.Run(iterations: 5, baseSeed: 42, SimulatorChoicePolicy.Safe);
            var b = simulator.Run(iterations: 5, baseSeed: 42, SimulatorChoicePolicy.Safe);

            Assert.AreEqual(a.SuccessCount, b.SuccessCount);
            Assert.AreEqual(a.AverageDaysSurvived, b.AverageDaysSurvived);
            Assert.AreEqual(a.AverageCash, b.AverageCash);
            Assert.AreEqual(a.ToString(), b.ToString());
        }

        [Test]
        public void Run_RestOnly_FirstChoice_SurvivesAllDays()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var rest = CreateRestEvent("rest");
            var ending = CreateSuccessEnding("barely");
            var simulator = new RunSimulator(
                job,
                null,
                new List<EventData> { rest },
                rest,
                new List<EndingData> { ending },
                ending);

            var summary = simulator.Run(iterations: 3, baseSeed: 1, SimulatorChoicePolicy.Safe);

            Assert.AreEqual(3, summary.Iterations);
            Assert.AreEqual(3, summary.SuccessCount);
            Assert.AreEqual(1.0, summary.SuccessRate, 0.0001);
            Assert.AreEqual(30.0, summary.AverageDaysSurvived, 0.0001);
        }

        [Test]
        public void SimulationSummary_ToString_IncludesFailureRatios()
        {
            var summary = new SimulationSummary { Iterations = 10, SuccessCount = 7 };
            summary.FailureCounts[FailureReason.Bankruptcy] = 2;
            summary.FailureCounts[FailureReason.Burnout] = 1;

            var text = summary.ToString();
            StringAssert.Contains("Fail:Bankruptcy=2", text);
            StringAssert.Contains("전체", text);
            StringAssert.Contains("실패 중", text);
        }

        [Test]
        public void PickPolicy_SafeThriftyRisky_MapToChoiceIndices()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var rest = CreateRestEvent("rest");
            var ending = CreateSuccessEnding("barely");
            var simulator = new RunSimulator(
                job,
                null,
                new List<EventData> { rest },
                rest,
                new List<EndingData> { ending },
                ending);

            Assert.DoesNotThrow(() => simulator.RunOnce(1, SimulatorChoicePolicy.Safe));
            Assert.DoesNotThrow(() => simulator.RunOnce(2, SimulatorChoicePolicy.Thrifty));
            Assert.DoesNotThrow(() => simulator.RunOnce(3, SimulatorChoicePolicy.Risky));
        }

        private static EventData CreateRestEvent(string id)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "rest",
                    "쉰다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, -2),
                        new StatEffect(StatType.Happiness, 1)
                    }),
                new EventChoiceData("alt", "다른 선택"),
                new EventChoiceData("skip", "패스")
            };

            eventData.EditorSetCore(
                id,
                id,
                "desc",
                EventCategory.Rest,
                1,
                30,
                100,
                new EventCondition(),
                choices);
            return eventData;
        }

        private static EndingData CreateSuccessEnding(string id)
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet(id, id, "desc", 1, false, FailureReason.None, new EndingCondition());
            return ending;
        }
    }
}
