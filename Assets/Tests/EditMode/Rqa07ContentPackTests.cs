using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa07ContentPackTests
    {
        [Test]
        public void RunFlags_RelationshipIds_AreDistinct()
        {
            var ids = new[]
            {
                RunFlags.CloseWithCoworker,
                RunFlags.Dating,
                RunFlags.MentorBond,
                RunFlags.NeighborFeud,
                RunFlags.FamilySupport,
                RunFlags.HasBoughtStock,
                RunFlags.PromotionTrack
            };
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void CoworkerCover_RequiresCloseFlag()
        {
            var condition = new EventCondition();
            condition.EditorSetFlags(new[] { RunFlags.CloseWithCoworker }, null);
            var state = CreateState("job_junior_office");

            Assert.IsFalse(EventConditionEvaluator.Matches(condition, state, false));
            state.SetFlag(RunFlags.CloseWithCoworker);
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, state, false));
        }

        [Test]
        public void DatingFollowUp_BlockedByForbiddenWhenAlreadyDatingOnIntro()
        {
            var intro = new EventCondition();
            intro.EditorSetFlags(null, new[] { RunFlags.Dating });
            var state = CreateState("job_junior_office");
            Assert.IsTrue(EventConditionEvaluator.Matches(intro, state, false));

            state.SetFlag(RunFlags.Dating);
            Assert.IsFalse(EventConditionEvaluator.Matches(intro, state, false));
        }

        [Test]
        public void CorpWorkshop_OnlyMatchesCorpJob()
        {
            var condition = new EventCondition();
            condition.EditorConfigure(newRequiredJobId: "job_corp_associate");
            var office = CreateState("job_junior_office");
            var corp = CreateState("job_corp_associate");

            Assert.IsFalse(EventConditionEvaluator.Matches(condition, office, false));
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, corp, false));
        }

        [Test]
        public void WeekendGroup_OnlyMatchesWeekend()
        {
            var condition = new EventCondition();
            condition.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekendOnly);
            var state = CreateState("job_junior_office");
            Assert.IsFalse(EventConditionEvaluator.Matches(condition, state, isWeekend: false));
            Assert.IsTrue(EventConditionEvaluator.Matches(condition, state, isWeekend: true));
        }

        [Test]
        public void EffectResolver_CoworkerLunch_SetsRelationshipFlag()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var state = GameState.CreateFromJob(job, null, 7);
            state.CurrentDay = 5;
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(7), new RunHistory(), days);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_coworker_lunch_001",
                "동료 점심 제안",
                "desc",
                EventCategory.Relationship,
                1,
                30,
                36,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "a",
                        "같이 간다",
                        new List<StatEffect> { new StatEffect(StatType.Cash, -18_000L) },
                        null,
                        new List<string> { RunFlags.CloseWithCoworker },
                        null),
                    new EventChoiceData("b", "김밥"),
                    new EventChoiceData("c", "거절")
                });

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out var error), error);
            Assert.IsTrue(state.HasFlag(RunFlags.CloseWithCoworker));
        }

        [Test]
        public void EffectResolver_CoworkerCover_ClearsRelationshipFlag()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var state = GameState.CreateFromJob(job, null, 7);
            state.CurrentDay = 6;
            state.SetFlag(RunFlags.CloseWithCoworker);
            var days = new DayManager(state);
            var resolver = new EffectResolver(state, new SeededRandomService(7), new RunHistory(), days);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_coworker_cover_001",
                "동료의 부탁",
                "desc",
                EventCategory.Work,
                1,
                30,
                92,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "a",
                        "대신 처리한다",
                        new List<StatEffect> { new StatEffect(StatType.Stress, 8) },
                        null,
                        null,
                        new List<string> { RunFlags.CloseWithCoworker }),
                    new EventChoiceData("b", "반만"),
                    new EventChoiceData("c", "거절")
                });

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out _, out var error), error);
            Assert.IsFalse(state.HasFlag(RunFlags.CloseWithCoworker));
        }

        [Test]
        public void EventArtResolver_MissingIllustration_UsesCategoryFallback()
        {
            var fallback = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            var resolved = EventArtResolver.ResolveBackgroundSprite("event_rqa07_missing_illustration", fallback);
            Assert.AreSame(fallback, resolved);
            Assert.IsNull(EventArtResolver.TryLoadEventIllustration("event_rqa07_missing_illustration"));
        }

        [Test]
        public void RelationshipEvent_ResolveBackground_UsesHomeOrOverride()
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_family_visit_001",
                "본가 방문",
                "d",
                EventCategory.Relationship,
                1,
                30,
                36,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData("a", "1"),
                    new EventChoiceData("b", "2"),
                    new EventChoiceData("c", "3")
                });
            Assert.AreEqual(BackgroundId.Home, eventData.ResolveBackground());

            eventData.EditorSetArt(true, BackgroundId.Restaurant, false, ExpressionId.Default);
            Assert.AreEqual(BackgroundId.Restaurant, eventData.ResolveBackground());
        }

        [Test]
        public void NewTraitUnlockLevels_ExtendPastOvertimePro()
        {
            var overtime = ScriptableObject.CreateInstance<TraitData>();
            overtime.EditorSet("trait_overtime_pro", "야근 전문가", "", 4);
            var networker = ScriptableObject.CreateInstance<TraitData>();
            networker.EditorSet("trait_networker", "인맥왕", "", 5);
            var boundary = ScriptableObject.CreateInstance<TraitData>();
            boundary.EditorSet("trait_boundary", "선 긋기", "", 7);

            Assert.Greater(networker.UnlockLevel, overtime.UnlockLevel);
            Assert.Greater(boundary.UnlockLevel, networker.UnlockLevel);
        }

        [Test]
        public void CorpJob_UnlocksAfterFreelancer()
        {
            var freelancer = ScriptableObject.CreateInstance<JobData>();
            freelancer.EditorSet("job_freelancer", "프리랜서", "", 3, 0, 0, 70, 30, 50, 40);
            var corp = ScriptableObject.CreateInstance<JobData>();
            corp.EditorSet("job_corp_associate", "대기업 사원", "", 5, 3_500_000L, 3_200_000L, 78, 32, 48, 58);
            Assert.Greater(corp.UnlockLevel, freelancer.UnlockLevel);
        }

        private static GameState CreateState(string jobId)
        {
            var state = new GameState { CurrentDay = 10, JobId = jobId };
            state.Stats.Health = 80;
            state.Stats.Stress = 20;
            state.Stats.Happiness = 50;
            state.Stats.CompanyScore = 50;
            state.Stats.Cash = 500_000L;
            return state;
        }
    }
}
