using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.Audio
{
    /// <summary>
    /// 플레이 중 BGM 상태 판정.
    /// </summary>
    public static class GameAudioRules
    {
        public const int CrisisStressThreshold = 80;

        public static BgmId ResolvePlayBgm(GameState state, DayManager days)
        {
            if (state?.Stats == null)
            {
                return BgmId.Play;
            }

            if (state.Stats.Stress >= CrisisStressThreshold)
            {
                return BgmId.Crisis;
            }

            if (days != null && days.IsLateCrisisDay())
            {
                return BgmId.Crisis;
            }

            return BgmId.Play;
        }

        public static void PlayChoiceResultSfx(IAudioService audio, ChoiceResult result)
        {
            if (audio == null || result?.StatChanges == null)
            {
                return;
            }

            var playedCash = false;
            var playedStress = false;
            for (var i = 0; i < result.StatChanges.Count; i++)
            {
                var change = result.StatChanges[i];
                if (!change.Changed)
                {
                    continue;
                }

                if (change.StatType == StatType.Cash && !playedCash)
                {
                    audio.PlaySfx(change.ActualDelta >= 0 ? SfxId.CashGain : SfxId.CashLoss);
                    playedCash = true;
                }
                else if (change.StatType == StatType.Stress && change.ActualDelta > 0 && !playedStress)
                {
                    audio.PlaySfx(SfxId.StressUp);
                    playedStress = true;
                }
            }
        }
    }
}
