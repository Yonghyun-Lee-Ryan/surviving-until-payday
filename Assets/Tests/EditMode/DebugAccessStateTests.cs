using NUnit.Framework;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Tests
{
    public sealed class DebugAccessStateTests
    {
        [Test]
        public void GameState_SetAndClearFlag_RoundTrips()
        {
            var state = new GameState();
            Assert.IsFalse(state.HasFlag(RunFlags.OwesDebt));

            state.SetFlag(RunFlags.OwesDebt);
            Assert.IsTrue(state.HasFlag(RunFlags.OwesDebt));

            state.ClearFlag(RunFlags.OwesDebt);
            Assert.IsFalse(state.HasFlag(RunFlags.OwesDebt));
        }

        [Test]
        public void GameState_ClearRunFlags_RemovesAll()
        {
            var state = new GameState();
            state.SetFlag(RunFlags.HasBoughtStock);
            state.SetFlag(RunFlags.PhoneStillCracked);
            state.ClearRunFlags();
            Assert.AreEqual(0, state.RunFlags.Count);
        }

        [Test]
        public void PlayerStats_CashAdjust_MatchesDelta()
        {
            var stats = new PlayerStats(1_000_000L, 50, 20, 50, 50);
            stats.Cash += 100_000L;
            Assert.AreEqual(1_100_000L, stats.Cash);
            stats.Cash += -500_000L;
            Assert.AreEqual(600_000L, stats.Cash);
        }
    }
}
