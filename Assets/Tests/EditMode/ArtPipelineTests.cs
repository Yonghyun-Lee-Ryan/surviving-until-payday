using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.Tests
{
    public sealed class ArtPipelineTests
    {
        [Test]
        public void CategoryDefaults_MapDistinctBackgrounds()
        {
            Assert.AreEqual(BackgroundId.Office, ArtCategoryDefaults.BackgroundFor(EventCategory.Work));
            Assert.AreEqual(BackgroundId.Hospital, ArtCategoryDefaults.BackgroundFor(EventCategory.Health));
            Assert.AreEqual(BackgroundId.Restaurant, ArtCategoryDefaults.BackgroundFor(EventCategory.Consumption));
            Assert.AreEqual(BackgroundId.Home, ArtCategoryDefaults.BackgroundFor(EventCategory.Rest));
            Assert.AreEqual(BackgroundId.Subway, ArtCategoryDefaults.BackgroundFor(EventCategory.Accident));
        }

        [Test]
        public void EventData_ResolveBackground_UsesCategoryUnlessOverride()
        {
            var eventData = UnityEngine.ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "e1",
                "t",
                "d",
                EventCategory.Health,
                1,
                30,
                100,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData("a", "1"),
                    new EventChoiceData("b", "2"),
                    new EventChoiceData("c", "3")
                });

            Assert.AreEqual(BackgroundId.Hospital, eventData.ResolveBackground());

            eventData.EditorSetArt(true, BackgroundId.Home, false, ExpressionId.Default);
            Assert.AreEqual(BackgroundId.Home, eventData.ResolveBackground());
        }

        [Test]
        public void ExpressionResolver_PrefersDespairOnLowHealth()
        {
            var result = new ChoiceResult(
                5,
                "e",
                "t",
                0,
                "c",
                "x",
                "m",
                null,
                null,
                new PlayerStats(100_000L, 25, 20, 50, 50),
                new PlayerStats(100_000L, 15, 20, 50, 50),
                null,
                FailureReason.None);

            Assert.AreEqual(ExpressionId.Despair, ExpressionResolver.FromChoiceResult(result));
        }

        [Test]
        public void ExpressionResolver_HappyOnHappinessGain()
        {
            var result = new ChoiceResult(
                5,
                "e",
                "t",
                0,
                "c",
                "x",
                "m",
                null,
                null,
                new PlayerStats(100_000L, 70, 20, 40, 50),
                new PlayerStats(100_000L, 70, 20, 50, 50),
                null,
                FailureReason.None);

            Assert.AreEqual(ExpressionId.Happy, ExpressionResolver.FromChoiceResult(result));
        }
    }
}
