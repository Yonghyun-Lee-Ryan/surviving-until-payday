using System;
using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Tests
{
    public sealed class DayManagerTests
    {
        [Test]
        public void StartDay_IsDayOne_MondayByDefault()
        {
            var state = new GameState { CurrentDay = 1 };
            var days = new DayManager(state);

            Assert.AreEqual(1, days.CurrentDay);
            Assert.AreEqual(DayOfWeek.Monday, days.CurrentDayOfWeek);
            Assert.IsFalse(days.IsWeekend);
            Assert.AreEqual(1.0f, days.DifficultyMultiplier, 0.0001f);
        }

        [Test]
        public void Weekday_Calculation_CoversFullWeek()
        {
            var state = new GameState { CurrentDay = 1 };
            var days = new DayManager(state);

            Assert.AreEqual(DayOfWeek.Monday, DayCalendar.GetDayOfWeek(1));
            Assert.AreEqual(DayOfWeek.Friday, DayCalendar.GetDayOfWeek(5));
            Assert.AreEqual(DayOfWeek.Saturday, DayCalendar.GetDayOfWeek(6));
            Assert.AreEqual(DayOfWeek.Sunday, DayCalendar.GetDayOfWeek(7));
            Assert.IsTrue(DayCalendar.IsWeekend(6));
            Assert.IsTrue(DayCalendar.IsWeekend(7));
            Assert.IsFalse(DayCalendar.IsWeekend(5));

            days.SetDay(6);
            Assert.IsTrue(days.IsWeekend);
        }

        [Test]
        public void DifficultyScaler_MatchesDesignTable()
        {
            Assert.AreEqual(1.0f, DifficultyScaler.GetMultiplier(1), 0.0001f);
            Assert.AreEqual(1.0f, DifficultyScaler.GetMultiplier(7), 0.0001f);
            Assert.AreEqual(1.1f, DifficultyScaler.GetMultiplier(8), 0.0001f);
            Assert.AreEqual(1.1f, DifficultyScaler.GetMultiplier(14), 0.0001f);
            Assert.AreEqual(1.2f, DifficultyScaler.GetMultiplier(15), 0.0001f);
            Assert.AreEqual(1.2f, DifficultyScaler.GetMultiplier(21), 0.0001f);
            Assert.AreEqual(1.35f, DifficultyScaler.GetMultiplier(22), 0.0001f);
            Assert.AreEqual(1.35f, DifficultyScaler.GetMultiplier(27), 0.0001f);
            Assert.AreEqual(1.5f, DifficultyScaler.GetMultiplier(28), 0.0001f);
            Assert.AreEqual(1.5f, DifficultyScaler.GetMultiplier(30), 0.0001f);
        }

        [Test]
        public void TryAdvanceDay_StopsAtDay30()
        {
            var state = new GameState { CurrentDay = 29 };
            var days = new DayManager(state);

            Assert.IsTrue(days.TryAdvanceDay());
            Assert.AreEqual(30, days.CurrentDay);
            Assert.IsFalse(days.TryAdvanceDay());
            Assert.AreEqual(30, days.CurrentDay);
            Assert.IsTrue(days.IsFinalDay);
        }

        [Test]
        public void WeeklySummaryDays_Are_7_14_21()
        {
            Assert.IsTrue(DayManager.IsWeeklySummaryDay(7));
            Assert.IsTrue(DayManager.IsWeeklySummaryDay(14));
            Assert.IsTrue(DayManager.IsWeeklySummaryDay(21));
            Assert.IsFalse(DayManager.IsWeeklySummaryDay(30));
            Assert.AreEqual(1, DayManager.GetWeekNumber(7));
            Assert.AreEqual(2, DayManager.GetWeekNumber(14));
            Assert.AreEqual(3, DayManager.GetWeekNumber(21));
        }

        [Test]
        public void LateCrisisDays_Are_28_29()
        {
            Assert.IsTrue(DayManager.IsLateCrisisDay(28));
            Assert.IsTrue(DayManager.IsLateCrisisDay(29));
            Assert.IsFalse(DayManager.IsLateCrisisDay(30));
        }
    }

    public sealed class RunManagerTests
    {
        [Test]
        public void StartRunWithState_InitializesDayOneAndRaisesEvents()
        {
            var run = new RunManager();
            var startedDays = new List<int>();
            var runStarted = false;

            run.RunStarted += _ => runStarted = true;
            run.DayStarted += (_, day) => startedDays.Add(day);

            run.StartRunWithState(CreateHealthyState(1));

            Assert.IsTrue(runStarted);
            CollectionAssert.AreEqual(new[] { 1 }, startedDays);
            Assert.AreEqual(RunStatus.InProgress, run.Status);
            Assert.AreEqual(1, run.State.CurrentDay);
        }

        [Test]
        public void CompleteCurrentDay_AdvancesToNextDay()
        {
            var run = new RunManager();
            run.StartRunWithState(CreateHealthyState(1));

            var result = run.CompleteCurrentDay();

            Assert.IsTrue(result.Accepted);
            Assert.IsFalse(result.RunSucceeded);
            Assert.IsFalse(result.WeeklySummaryTriggered);
            Assert.AreEqual(1, result.DayBefore);
            Assert.AreEqual(2, result.DayAfter);
            Assert.AreEqual(2, run.State.CurrentDay);
        }

        [Test]
        public void CompleteCurrentDay_OnDay7_RaisesWeeklySummaryThenAdvances()
        {
            var run = new RunManager();
            run.StartRunWithState(CreateHealthyState(7));

            WeeklySummaryInfo summary = null;
            run.WeeklySummary += info => summary = info;

            var result = run.CompleteCurrentDay();

            Assert.IsTrue(result.WeeklySummaryTriggered);
            Assert.IsNotNull(summary);
            Assert.AreEqual(7, summary.Day);
            Assert.AreEqual(1, summary.WeekNumber);
            Assert.AreEqual(8, run.State.CurrentDay);
            Assert.AreEqual(RunStatus.InProgress, run.Status);
        }

        [Test]
        public void CompleteCurrentDay_OnDay14And21_RaisesWeeklySummary()
        {
            var daysTriggered = new List<int>();
            var run = new RunManager();
            run.WeeklySummary += info => daysTriggered.Add(info.Day);

            run.StartRunWithState(CreateHealthyState(14));
            run.CompleteCurrentDay();

            run.StartRunWithState(CreateHealthyState(21));
            run.CompleteCurrentDay();

            CollectionAssert.AreEqual(new[] { 14, 21 }, daysTriggered);
        }

        [Test]
        public void CompleteCurrentDay_OnDay30_SucceedsWithoutAdvancing()
        {
            var run = new RunManager();
            run.StartRunWithState(CreateHealthyState(30));

            var succeeded = false;
            run.RunSucceeded += _ => succeeded = true;

            var result = run.CompleteCurrentDay();

            Assert.IsTrue(result.RunSucceeded);
            Assert.IsTrue(succeeded);
            Assert.AreEqual(30, run.State.CurrentDay);
            Assert.AreEqual(RunStatus.Succeeded, run.Status);
        }

        [Test]
        public void CompleteCurrentDay_WhenFailed_DoesNotAdvance()
        {
            var state = CreateHealthyState(5);
            state.Stats.Cash = -1;

            var run = new RunManager();
            run.StartRunWithState(state);

            FailureReason? failedReason = null;
            run.RunFailed += (_, reason) => failedReason = reason;

            var result = run.CompleteCurrentDay();

            Assert.IsTrue(result.RunFailed);
            Assert.AreEqual(FailureReason.Bankruptcy, result.FailureReason);
            Assert.AreEqual(FailureReason.Bankruptcy, failedReason);
            Assert.AreEqual(5, run.State.CurrentDay);
            Assert.AreEqual(RunStatus.Failed, run.Status);
        }

        [Test]
        public void CompleteCurrentDay_AfterSuccess_IsRejected()
        {
            var run = new RunManager();
            run.StartRunWithState(CreateHealthyState(30));
            run.CompleteCurrentDay();

            var second = run.CompleteCurrentDay();

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual(RunStatus.Succeeded, run.Status);
        }

        [Test]
        public void FullRun_Survive30Days_Succeeds()
        {
            var run = new RunManager();
            run.StartRunWithState(CreateHealthyState(1));

            var weeklyCount = 0;
            run.WeeklySummary += _ => weeklyCount++;

            for (var i = 0; i < 30; i++)
            {
                var result = run.CompleteCurrentDay();
                Assert.IsTrue(result.Accepted, $"Day resolve failed at loop {i}, dayBefore={result.DayBefore}");
                if (result.RunSucceeded)
                {
                    break;
                }
            }

            Assert.AreEqual(RunStatus.Succeeded, run.Status);
            Assert.AreEqual(3, weeklyCount);
            Assert.AreEqual(30, run.State.CurrentDay);
        }

        private static GameState CreateHealthyState(int day)
        {
            var state = new GameState
            {
                CurrentDay = day,
                JobId = "test_job",
                Salary = 2_800_000L
            };
            state.Stats.Cash = 500_000L;
            state.Stats.Health = 80;
            state.Stats.Stress = 20;
            state.Stats.Happiness = 50;
            state.Stats.CompanyScore = 50;
            return state;
        }
    }
}
