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
    public sealed class Unit25DailyContentTests
    {
        [Test]
        public void DailyChallenge_SameDateKey_SameSeed()
        {
            var key = "2026-07-26";
            Assert.AreEqual(DailyChallenge.SeedFromDateKey(key), DailyChallenge.SeedFromDateKey(key));
            Assert.AreNotEqual(
                DailyChallenge.SeedFromDateKey("2026-07-26"),
                DailyChallenge.SeedFromDateKey("2026-07-27"));
        }

        [Test]
        public void DailyChallenge_LocalDateKey_UsesCalendarDate()
        {
            var noon = new DateTime(2026, 7, 26, 12, 0, 0);
            var late = new DateTime(2026, 7, 26, 23, 59, 0);
            var next = new DateTime(2026, 7, 27, 0, 0, 0);
            Assert.AreEqual("2026-07-26", DailyChallenge.LocalDateKey(noon));
            Assert.AreEqual("2026-07-26", DailyChallenge.LocalDateKey(late));
            Assert.AreEqual("2026-07-27", DailyChallenge.LocalDateKey(next));
        }

        [Test]
        public void EventSelector_SameDailySeed_ReproducesSequence()
        {
            var a = CreateEvent("a", 1, 30, 50);
            var b = CreateEvent("b", 1, 30, 50);
            var fallback = CreateRestFallback();
            var seed = DailyChallenge.SeedFromDateKey("2026-07-26");

            var first = new List<string>();
            var second = new List<string>();
            var state = CreateState(1);

            var selector1 = new EventSelector(new[] { a, b }, fallback, new SeededRandomService(seed));
            var selector2 = new EventSelector(new[] { a, b }, fallback, new SeededRandomService(seed));
            for (var i = 0; i < 8; i++)
            {
                first.Add(selector1.Select(state, false).Id);
                second.Add(selector2.Select(state, false).Id);
            }

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void DailyContentState_MidnightRefresh_ReassignsMissions()
        {
            var pool = CreateMissionPool();
            var daily = new DailyContentState();
            daily.EnsureForLocalDate(pool, new DateTime(2026, 7, 26, 10, 0, 0));
            var day1Ids = CaptureIds(daily);
            Assert.GreaterOrEqual(day1Ids.Count, 1);
            Assert.LessOrEqual(day1Ids.Count, 3);

            var refreshed = daily.EnsureForLocalDate(pool, new DateTime(2026, 7, 27, 0, 5, 0));
            Assert.IsTrue(refreshed);
            Assert.AreEqual("2026-07-27", daily.DateKey);
            Assert.IsFalse(daily.HasBestRecord);
        }

        [Test]
        public void DailyContentState_SameDay_DoesNotReset()
        {
            var pool = CreateMissionPool();
            var daily = new DailyContentState();
            daily.EnsureForLocalDate(pool, new DateTime(2026, 7, 26, 8, 0, 0));
            var first = CaptureIds(daily);
            var changed = daily.EnsureForLocalDate(pool, new DateTime(2026, 7, 26, 22, 0, 0));
            Assert.IsFalse(changed);
            CollectionAssert.AreEqual(first, CaptureIds(daily));
        }

        [Test]
        public void DailyMissionAssigner_SameSeed_SameMissions()
        {
            var pool = CreateMissionPool();
            var seed = DailyChallenge.SeedFromDateKey("2026-07-26");
            var a = DailyMissionAssigner.Assign(pool, seed);
            var b = DailyMissionAssigner.Assign(pool, seed);
            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
            }
        }

        [Test]
        public void DailyMissionEvaluator_CompanyAndForbiddenFlag()
        {
            var company = ScriptableObject.CreateInstance<DailyMissionData>();
            company.Configure("m_company", "회사", "", DailyMissionGoalType.MinCompanyScore, 0, 80, null, 10, 1);

            var noDelivery = ScriptableObject.CreateInstance<DailyMissionData>();
            noDelivery.Configure(
                "m_nodelivery",
                "배달금지",
                "",
                DailyMissionGoalType.ForbiddenFlagThroughDays,
                0,
                10,
                RunFlags.OrderedDelivery,
                10,
                1);

            var state = CreateState(12);
            state.Stats.CompanyScore = 85;
            Assert.IsTrue(DailyMissionEvaluator.IsCompleted(company, state, isSuccess: false));
            Assert.IsTrue(DailyMissionEvaluator.IsCompleted(noDelivery, state, isSuccess: false));

            state.SetFlag(RunFlags.OrderedDelivery);
            Assert.IsFalse(DailyMissionEvaluator.IsCompleted(noDelivery, state, isSuccess: false));
        }

        [Test]
        public void DailyContentState_ApplyRun_GrantsRewardsAndUpdatesBest()
        {
            var mission = ScriptableObject.CreateInstance<DailyMissionData>();
            mission.Configure("m_survive", "생존", "", DailyMissionGoalType.SurviveSuccess, 0, 0, null, 40, 2);

            var daily = new DailyContentState();
            daily.EnsureForLocalDate(new[] { mission }, new DateTime(2026, 7, 26));
            daily.BindMissionDefinitions(new[] { mission });

            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null);
            var beforeXp = meta.TotalExperience;
            var beforeFrag = meta.TraitFragmentCount;

            var state = CreateState(30);
            state.Stats.Cash = 600_000;
            var result = ResultData.Create(state, isSuccess: true, FailureReason.None, ending: null);
            Assert.IsTrue(daily.TryUpdateBest(result));
            var completed = daily.ApplyRunToMissions(state, isSuccess: true, meta);

            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(beforeXp + 40, meta.TotalExperience);
            Assert.AreEqual(beforeFrag + 2, meta.TraitFragmentCount);
            Assert.IsTrue(daily.HasBestRecord);
            Assert.AreEqual(600_000, daily.BestCash);
        }

        [Test]
        public void SaveMapper_CapturesDailyFields()
        {
            var mission = ScriptableObject.CreateInstance<DailyMissionData>();
            mission.Configure("m1", "미션", "", DailyMissionGoalType.SurviveMinDays, 0, 5, null, 10, 1);
            var meta = new MetaProgressionManager();
            meta.Load(10, null, null, null, null, null, 3);
            meta.Daily.EnsureForLocalDate(new[] { mission }, new DateTime(2026, 7, 26));
            meta.Daily.BindMissionDefinitions(new[] { mission });

            var captured = SaveMapper.CaptureMeta(meta);
            Assert.AreEqual("2026-07-26", captured.dailyDateKey);
            Assert.AreEqual(1, captured.dailyMissions.Count);

            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(captured, loaded);
            Assert.AreEqual("2026-07-26", loaded.Daily.DateKey);
            Assert.AreEqual(1, loaded.Daily.Missions.Count);
        }

        private static List<string> CaptureIds(DailyContentState daily)
        {
            var ids = new List<string>();
            for (var i = 0; i < daily.Missions.Count; i++)
            {
                ids.Add(daily.Missions[i].MissionId);
            }

            return ids;
        }

        private static List<DailyMissionData> CreateMissionPool()
        {
            var list = new List<DailyMissionData>();
            for (var i = 0; i < 6; i++)
            {
                var m = ScriptableObject.CreateInstance<DailyMissionData>();
                m.Configure($"mission_{i}", $"미션{i}", "", DailyMissionGoalType.SurviveMinDays, 0, i + 1, null, 10, 1);
                list.Add(m);
            }

            return list;
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

        private static GameState CreateState(int day)
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            job.EditorSet("job_test", "테스트", "", 0, 2_000_000, 1_000_000, 80, 20, 50, 50);
            var state = GameState.CreateFromJob(job, null, seed: 1);
            state.CurrentDay = day;
            return state;
        }
    }
}
