using SurviveUntilPayday.Audio;
using UnityEngine;

namespace SurviveUntilPayday.Audio
{
    /// <summary>
    /// 클립이 전부 null인 no-op 구현. 테스트·에디터 기본값용.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public void PlaySfx(SfxId id)
        {
        }

        public void SetBgm(BgmId id)
        {
        }

        public void StopBgm()
        {
        }

        public void ApplySettings(bool soundEnabled, float volume)
        {
            AudioListener.volume = soundEnabled ? Mathf.Clamp01(volume) : 0f;
        }
    }
}
