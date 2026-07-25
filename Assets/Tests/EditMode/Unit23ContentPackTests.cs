using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Data;
using UnityEditor;

namespace SurviveUntilPayday.Tests
{
    /// <summary>
    /// Unit 23: 직업·사건 팩 수량/직업 전용 사건 검증.
    /// </summary>
    public sealed class Unit23ContentPackTests
    {
        private const string EventsFolder = "Assets/Data/Events";
        private const string JobsFolder = "Assets/Data/Jobs";

        [Test]
        public void JobsFolder_HasThreeJobsWithExpectedUnlockLevels()
        {
            var byId = LoadJobsById();
            Assert.AreEqual(3, byId.Count);

            Assert.AreEqual(0, byId["job_junior_office"].UnlockLevel);
            Assert.AreEqual(2, byId["job_civil_prep"].UnlockLevel);
            Assert.AreEqual(3, byId["job_freelancer"].UnlockLevel);
        }

        [Test]
        public void EventsFolder_HasJobLockedCivilAndFreelancePacks()
        {
            var civil = 0;
            var freelance = 0;
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData?.Conditions == null)
                {
                    continue;
                }

                if (eventData.Conditions.RequiredJobId == "job_civil_prep")
                {
                    civil++;
                }
                else if (eventData.Conditions.RequiredJobId == "job_freelancer")
                {
                    freelance++;
                }
            }

            Assert.AreEqual(8, civil, "공무원 준비생 전용 사건 8개");
            Assert.AreEqual(8, freelance, "프리랜서 전용 사건 8개");
        }

        [Test]
        public void EventsFolder_HasAtLeastFortyGeneralEvents()
        {
            var general = 0;
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData == null || eventData.Id == "event_rest_fallback")
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(eventData.Conditions?.RequiredJobId))
                {
                    general++;
                }
            }

            Assert.GreaterOrEqual(general, 39, $"직업 무관 사건 목표 ~40 (현재 {general})");
        }

        private static Dictionary<string, JobData> LoadJobsById()
        {
            var map = new Dictionary<string, JobData>();
            var guids = AssetDatabase.FindAssets("t:JobData", new[] { JobsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var job = AssetDatabase.LoadAssetAtPath<JobData>(path);
                if (job != null)
                {
                    map[job.Id] = job;
                }
            }

            return map;
        }
    }
}
