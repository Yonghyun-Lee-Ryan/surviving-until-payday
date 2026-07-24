using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class TraitApplicationTests
    {
        [Test]
        public void CreateFromJob_AppliesStartingModifiers()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_test", "테스트", "desc", 0);
            SetStartingModifiers(trait, new StatEffect(StatType.Cash, 100_000L), new StatEffect(StatType.Health, 5));

            var without = GameState.CreateFromJob(job, null, 1);
            var withTrait = GameState.CreateFromJob(job, trait, 1);

            Assert.AreEqual(without.Stats.Cash + 100_000L, withTrait.Stats.Cash);
            Assert.AreEqual(without.Stats.Health + 5, withTrait.Stats.Health);
            Assert.AreEqual("trait_test", withTrait.TraitId);
        }

        [Test]
        public void TraitRuntimeModifier_ThriftyReducesCashLossAndHappinessGain()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_thrifty", "짠돌이", "desc", 0);
            trait.EditorSetRuntimeMultipliers(0.95f, 0.5f, 1f);

            var cashLoss = TraitRuntimeModifier.Adjust(
                trait,
                EventCategory.Consumption,
                new StatEffect(StatType.Cash, -100_000L));
            var happinessGain = TraitRuntimeModifier.Adjust(
                trait,
                EventCategory.Consumption,
                new StatEffect(StatType.Happiness, 10));

            Assert.AreEqual(-95_000L, cashLoss.Value);
            Assert.AreEqual(5L, happinessGain.Value);
        }

        [Test]
        public void TraitRuntimeModifier_OvertimeProReducesWorkStressOnly()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_overtime_pro", "야근전문가", "desc", 0);
            trait.EditorSetRuntimeMultipliers(1f, 1f, 0.7f);

            var workStress = TraitRuntimeModifier.Adjust(
                trait,
                EventCategory.Work,
                new StatEffect(StatType.Stress, 10));
            var otherStress = TraitRuntimeModifier.Adjust(
                trait,
                EventCategory.Health,
                new StatEffect(StatType.Stress, 10));

            Assert.AreEqual(7L, workStress.Value);
            Assert.AreEqual(10L, otherStress.Value);
        }

        [Test]
        public void EffectResolver_AppliesTraitRuntimeModifiersOnChoice()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_thrifty", "짠돌이", "desc", 0);
            trait.EditorSetRuntimeMultipliers(0.95f, 1f, 1f);

            var state = GameState.CreateFromJob(job, trait, 7);
            var cashBefore = state.Stats.Cash;
            var days = new DayManager(state);
            var history = new RunHistory();
            var resolver = new EffectResolver(state, new SeededRandomService(7), history, days, trait);

            var eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.EditorSetCore(
                "event_spend",
                "지출",
                "테스트",
                EventCategory.Consumption,
                1,
                30,
                100,
                new EventCondition(),
                new List<EventChoiceData>
                {
                    new EventChoiceData(
                        "c1",
                        "산다",
                        new List<StatEffect> { new StatEffect(StatType.Cash, -100_000L) },
                        new List<RandomOutcome>())
                });

            resolver.BeginEvent(eventData);
            Assert.IsTrue(resolver.TryResolveChoice(0, out var result, out var error), error);
            Assert.AreEqual(cashBefore - 95_000L, state.Stats.Cash);
            Assert.AreEqual(cashBefore - 95_000L, result.StatsAfter.Cash);
        }

        [Test]
        public void GameSession_PendingSelection_ClearsAfterConsume()
        {
            var job = ScriptableObject.CreateInstance<JobData>();
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_thrifty", "짠돌이", "desc", 0);

            var session = new GameSession();
            session.SetPendingNewRun(job, trait);
            Assert.IsTrue(session.UsePendingRunSelection);
            Assert.AreEqual(GameStartMode.NewRun, session.StartMode);
            Assert.AreSame(trait, session.PendingTrait);

            session.ClearPendingRunSelection();
            Assert.IsFalse(session.UsePendingRunSelection);
            Assert.IsNull(session.PendingTrait);
        }

#if UNITY_EDITOR
        private static void SetStartingModifiers(TraitData trait, params StatEffect[] effects)
        {
            var so = new UnityEditor.SerializedObject(trait);
            var prop = so.FindProperty("startingStatModifiers");
            prop.ClearArray();
            for (var i = 0; i < effects.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("statType").enumValueIndex = (int)effects[i].StatType;
                element.FindPropertyRelative("value").longValue = effects[i].Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
#endif
    }
}
