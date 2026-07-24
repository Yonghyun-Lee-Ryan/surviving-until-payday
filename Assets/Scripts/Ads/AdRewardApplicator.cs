using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 확정된 광고 보상을 GameState/세션 플래그에 반영한다.
    /// </summary>
    public static class AdRewardApplicator
    {
        public static void ApplyCash(GameState state, AdRewardGrant reward)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (reward.CashDelta == 0)
            {
                return;
            }

            state.ApplyEffect(new StatEffect(StatType.Cash, reward.CashDelta));
        }
    }
}
