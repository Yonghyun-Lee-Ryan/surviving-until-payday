using NUnit.Framework;
using SurviveUntilPayday.Data;
using UnityEditor;

namespace SurviveUntilPayday.Tests
{
    /// <summary>
    /// 개발 단위 16: Assets/Data/Events에 유효한 MVP 사건이 20개 이상 있는지 검증한다.
    /// </summary>
    public sealed class MvpEventPackTests
    {
        private const string EventsFolder = "Assets/Data/Events";
        private const int MinimumEventCount = 20;

        [Test]
        public void EventsFolder_HasAtLeastTwentyEvents()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });

            Assert.GreaterOrEqual(
                guids.Length,
                MinimumEventCount,
                $"{EventsFolder}에 EventData가 {MinimumEventCount}개 이상 있어야 합니다. (현재 {guids.Length}개)");
        }

        [Test]
        public void EventsFolder_AllEventsLoadAndValidateWithoutErrors()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });
            Assert.Greater(guids.Length, 0, $"{EventsFolder}에서 EventData를 찾지 못했습니다.");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);

                Assert.IsNotNull(eventData, $"{path} 를 EventData로 로드하지 못했습니다.");

                var errors = eventData.Validate();
                Assert.AreEqual(
                    0,
                    errors.Count,
                    $"{eventData.name} Validate() 오류: {string.Join(" | ", errors)}");
            }
        }

        [Test]
        public void EventsFolder_HasNoDuplicateIds()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });
            var seenIds = new System.Collections.Generic.HashSet<string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData == null)
                {
                    continue;
                }

                Assert.IsTrue(
                    seenIds.Add(eventData.Id),
                    $"중복된 사건 id 발견: {eventData.Id} ({path})");
            }
        }
    }
}
