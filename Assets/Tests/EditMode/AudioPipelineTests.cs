using NUnit.Framework;
using SurviveUntilPayday.Audio;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class AudioPipelineTests
    {
        [Test]
        public void NullAudioService_MuteAndPlay_DoNotThrow()
        {
            var audio = new NullAudioService();
            Assert.DoesNotThrow(() => audio.ApplySettings(false, 0f));
            Assert.DoesNotThrow(() => audio.PlaySfx(SfxId.Click));
            Assert.DoesNotThrow(() => audio.SetBgm(BgmId.Main));
            Assert.DoesNotThrow(() => audio.StopBgm());
            Assert.DoesNotThrow(() => audio.ApplySettings(true, 1f));
        }

        [Test]
        public void UnityAudioService_NullClips_DoNotThrow()
        {
            var go = new GameObject("AudioTest");
            try
            {
                var audio = go.AddComponent<UnityAudioService>();
                Assert.DoesNotThrow(() => audio.ApplySettings(true, 1f));
                Assert.DoesNotThrow(() => audio.PlaySfx(SfxId.CashGain));
                Assert.DoesNotThrow(() => audio.SetBgm(BgmId.Crisis));
                Assert.DoesNotThrow(() => audio.ApplySettings(false, 0.5f));
                Assert.DoesNotThrow(() => audio.PlaySfx(SfxId.Payday));
                Assert.DoesNotThrow(() => audio.StopBgm());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GameAudioRules_CrisisWhenStressHigh()
        {
            var state = new GameState { CurrentDay = 10 };
            state.Stats.Stress = 90;

            Assert.AreEqual(BgmId.Crisis, GameAudioRules.ResolvePlayBgm(state, days: null));
        }

        [Test]
        public void GameAudioRules_CrisisOnLateDays()
        {
            var state = new GameState { CurrentDay = 28 };
            state.Stats.Stress = 20;
            var days = new DayManager(state);

            Assert.AreEqual(BgmId.Crisis, GameAudioRules.ResolvePlayBgm(state, days));
        }

        [Test]
        public void GameAudioRules_PlayOtherwise()
        {
            var state = new GameState { CurrentDay = 5 };
            state.Stats.Stress = 20;
            var days = new DayManager(state);

            Assert.AreEqual(BgmId.Play, GameAudioRules.ResolvePlayBgm(state, days));
        }

        [Test]
        public void GameAudioRules_PlayChoiceResultSfx_NullSafe()
        {
            Assert.DoesNotThrow(() => GameAudioRules.PlayChoiceResultSfx(null, null));
            Assert.DoesNotThrow(() => GameAudioRules.PlayChoiceResultSfx(new NullAudioService(), null));
        }
    }
}
