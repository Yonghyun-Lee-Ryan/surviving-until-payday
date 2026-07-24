using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Tests
{
    public sealed class GameStateStatTests
    {
        [Test]
        public void ApplyEffect_ChangesCashWithoutClampingBelowZero()
        {
            var state = CreateState(cash: 10_000L, health: 50, stress: 10, happiness: 50, companyScore: 50);

            var result = state.ApplyEffect(new StatEffect(StatType.Cash, -15_000L));

            Assert.AreEqual(10_000L, result.Before);
            Assert.AreEqual(-5_000L, result.After);
            Assert.AreEqual(-5_000L, state.Stats.Cash);
            Assert.AreEqual(FailureReason.Bankruptcy, state.EvaluateFailure());
        }

        [Test]
        public void ApplyEffect_ClampsHealthToZero()
        {
            var state = CreateState(cash: 100_000L, health: 5, stress: 10, happiness: 50, companyScore: 50);

            var result = state.ApplyEffect(new StatEffect(StatType.Health, -20));

            Assert.AreEqual(5, result.Before);
            Assert.AreEqual(0, result.After);
            Assert.IsTrue(result.WasClamped);
            Assert.AreEqual(0, state.Stats.Health);
            Assert.AreEqual(FailureReason.Hospitalization, state.EvaluateFailure());
        }

        [Test]
        public void ApplyEffect_ClampsStressToOneHundred()
        {
            var state = CreateState(cash: 100_000L, health: 50, stress: 95, happiness: 50, companyScore: 50);

            var result = state.ApplyEffect(new StatEffect(StatType.Stress, 20));

            Assert.AreEqual(95, result.Before);
            Assert.AreEqual(100, result.After);
            Assert.IsTrue(result.WasClamped);
            Assert.AreEqual(FailureReason.Burnout, state.EvaluateFailure());
        }

        [Test]
        public void ApplyEffect_ClampsHappinessAndCompanyScore()
        {
            var state = CreateState(cash: 100_000L, health: 50, stress: 10, happiness: 98, companyScore: 2);

            var happiness = state.ApplyEffect(new StatEffect(StatType.Happiness, 10));
            var company = state.ApplyEffect(new StatEffect(StatType.CompanyScore, -10));

            Assert.AreEqual(100, happiness.After);
            Assert.AreEqual(0, company.After);
            Assert.AreEqual(FailureReason.Fired, state.EvaluateFailure());
        }

        [Test]
        public void ApplyEffects_AppliesMultipleEffectsAndRaisesStatsChanged()
        {
            var state = CreateState(cash: 100_000L, health: 80, stress: 20, happiness: 50, companyScore: 50);
            IReadOnlyList<StatChangeResult> received = null;
            var eventCount = 0;

            state.StatsChanged += (_, changes) =>
            {
                eventCount++;
                received = changes;
            };

            var results = state.ApplyEffects(new[]
            {
                new StatEffect(StatType.Cash, -15_000L),
                new StatEffect(StatType.Health, -5),
                new StatEffect(StatType.Stress, 12),
                new StatEffect(StatType.CompanyScore, 10)
            });

            Assert.AreEqual(4, results.Count);
            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(received);
            Assert.AreEqual(4, received.Count);
            Assert.AreEqual(85_000L, state.Stats.Cash);
            Assert.AreEqual(75, state.Stats.Health);
            Assert.AreEqual(32, state.Stats.Stress);
            Assert.AreEqual(60, state.Stats.CompanyScore);
            Assert.AreEqual(FailureReason.None, state.EvaluateFailure());
        }

        [Test]
        public void ApplyEffects_RaisesFailureDetected_WhenBankrupt()
        {
            var state = CreateState(cash: 1_000L, health: 50, stress: 10, happiness: 50, companyScore: 50);
            FailureReason? detected = null;
            state.FailureDetected += (_, reason) => detected = reason;

            state.ApplyEffect(new StatEffect(StatType.Cash, -5_000L));

            Assert.AreEqual(FailureReason.Bankruptcy, detected);
        }

        [Test]
        public void EvaluateFailure_UsesPriority_BankruptcyOverHospitalization()
        {
            var state = CreateState(cash: -1L, health: 0, stress: 100, happiness: 0, companyScore: 0);

            Assert.AreEqual(FailureReason.Bankruptcy, state.EvaluateFailure());
            CollectionAssert.AreEqual(
                new[]
                {
                    FailureReason.Bankruptcy,
                    FailureReason.Hospitalization,
                    FailureReason.Burnout,
                    FailureReason.Fired
                },
                state.GetAllFailureReasons());
        }

        [Test]
        public void Snapshot_Restore_RevertsStats()
        {
            var state = CreateState(cash: 50_000L, health: 70, stress: 30, happiness: 40, companyScore: 60);
            var snapshot = state.CreateSnapshot();

            state.ApplyEffects(new[]
            {
                new StatEffect(StatType.Cash, -20_000L),
                new StatEffect(StatType.Health, -50)
            });

            state.RestoreSnapshot(snapshot);

            Assert.AreEqual(50_000L, state.Stats.Cash);
            Assert.AreEqual(70, state.Stats.Health);
            Assert.AreEqual(30, state.Stats.Stress);
        }

        [Test]
        public void CreateSnapshot_IsIndependentCopy()
        {
            var state = CreateState(cash: 10_000L, health: 50, stress: 10, happiness: 50, companyScore: 50);
            var snapshot = state.CreateSnapshot();

            state.ApplyEffect(new StatEffect(StatType.Cash, -1_000L));

            Assert.AreEqual(10_000L, snapshot.Stats.Cash);
            Assert.AreEqual(9_000L, state.Stats.Cash);
        }

        [Test]
        public void Boundary_HealthStayWithinRange_WhenBuffedAboveMax()
        {
            var state = CreateState(cash: 10_000L, health: 100, stress: 0, happiness: 100, companyScore: 100);

            var health = state.ApplyEffect(new StatEffect(StatType.Health, 5));
            var stress = state.ApplyEffect(new StatEffect(StatType.Stress, -10));

            Assert.AreEqual(100, health.After);
            Assert.AreEqual(0, stress.After);
            Assert.IsTrue(health.WasClamped);
            Assert.IsTrue(stress.WasClamped);
        }

        private static GameState CreateState(
            long cash,
            int health,
            int stress,
            int happiness,
            int companyScore)
        {
            var state = new GameState
            {
                CurrentDay = 1,
                JobId = "test_job"
            };

            state.Stats.Cash = cash;
            state.Stats.Health = health;
            state.Stats.Stress = stress;
            state.Stats.Happiness = happiness;
            state.Stats.CompanyScore = companyScore;
            return state;
        }
    }
}
