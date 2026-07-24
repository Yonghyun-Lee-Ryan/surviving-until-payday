using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.Audio
{
    /// <summary>
    /// BGM 루프 1개 + SFX PlayOneShot. 클립 null이면 재생하지 않는다.
    /// </summary>
    public sealed class UnityAudioService : MonoBehaviour, IAudioService
    {
        [Header("BGM")]
        [SerializeField] private AudioClip bgmMain;
        [SerializeField] private AudioClip bgmPlay;
        [SerializeField] private AudioClip bgmCrisis;
        [SerializeField] private AudioClip bgmResult;

        [Header("SFX")]
        [SerializeField] private AudioClip sfxClick;
        [SerializeField] private AudioClip sfxCashGain;
        [SerializeField] private AudioClip sfxCashLoss;
        [SerializeField] private AudioClip sfxStressUp;
        [SerializeField] private AudioClip sfxSuccess;
        [SerializeField] private AudioClip sfxFail;
        [SerializeField] private AudioClip sfxPayday;

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private BgmId currentBgm = BgmId.None;
        private bool soundEnabled = true;
        private float volume = 1f;

        public static UnityAudioService Create(Transform parent)
        {
            var go = new GameObject("AudioService");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var service = go.AddComponent<UnityAudioService>();
            service.EnsureSources();
            service.TryLoadPlaceholdersFromResources();
            return service;
        }

        private void Awake()
        {
            EnsureSources();
            TryLoadPlaceholdersFromResources();
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
            }
        }

        public void BindClips(
            AudioClip main,
            AudioClip play,
            AudioClip crisis,
            AudioClip result,
            AudioClip click,
            AudioClip cashGain,
            AudioClip cashLoss,
            AudioClip stressUp,
            AudioClip success,
            AudioClip fail,
            AudioClip payday)
        {
            bgmMain = main;
            bgmPlay = play;
            bgmCrisis = crisis;
            bgmResult = result;
            sfxClick = click;
            sfxCashGain = cashGain;
            sfxCashLoss = cashLoss;
            sfxStressUp = stressUp;
            sfxSuccess = success;
            sfxFail = fail;
            sfxPayday = payday;
        }

        public void PlaySfx(SfxId id)
        {
            EnsureSources();
            if (!soundEnabled || volume <= 0.0001f)
            {
                return;
            }

            var clip = ResolveSfx(id);
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, volume);
        }

        public void SetBgm(BgmId id)
        {
            EnsureSources();
            if (id == BgmId.None)
            {
                StopBgm();
                return;
            }

            if (!soundEnabled || volume <= 0.0001f)
            {
                StopBgm();
                currentBgm = id;
                return;
            }

            var clip = ResolveBgm(id);
            if (clip == null)
            {
                // 클립 없어도 상태는 기록해 두어 설정 복구 시 재시도 가능
                currentBgm = id;
                return;
            }

            if (currentBgm == id && bgmSource.isPlaying && bgmSource.clip == clip)
            {
                return;
            }

            currentBgm = id;
            bgmSource.clip = clip;
            bgmSource.volume = volume;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            EnsureSources();
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }

            currentBgm = BgmId.None;
        }

        public void ApplySettings(bool enabled, float soundVolume)
        {
            soundEnabled = enabled;
            volume = Mathf.Clamp01(soundVolume);
            AudioListener.volume = soundEnabled ? volume : 0f;

            EnsureSources();
            if (bgmSource != null)
            {
                bgmSource.volume = volume;
            }

            if (!soundEnabled || volume <= 0.0001f)
            {
                if (bgmSource != null && bgmSource.isPlaying)
                {
                    bgmSource.Stop();
                }

                return;
            }

            // 뮤트 해제 시 마지막 BGM 복구
            if (currentBgm != BgmId.None)
            {
                var resume = currentBgm;
                currentBgm = BgmId.None;
                SetBgm(resume);
            }
        }

        public void ApplyFromSettings(AppSettingsService settings)
        {
            if (settings == null)
            {
                return;
            }

            ApplySettings(settings.SoundEnabled, settings.SoundVolume);
        }

        /// <summary>
        /// Resources/Audio 플레이스홀더 클립을 비어 있는 슬롯에만 채운다.
        /// </summary>
        public void TryLoadPlaceholdersFromResources()
        {
            bgmMain = bgmMain != null ? bgmMain : Resources.Load<AudioClip>("Audio/bgm_main");
            bgmPlay = bgmPlay != null ? bgmPlay : Resources.Load<AudioClip>("Audio/bgm_play");
            bgmCrisis = bgmCrisis != null ? bgmCrisis : Resources.Load<AudioClip>("Audio/bgm_crisis");
            bgmResult = bgmResult != null ? bgmResult : Resources.Load<AudioClip>("Audio/bgm_result");
            sfxClick = sfxClick != null ? sfxClick : Resources.Load<AudioClip>("Audio/sfx_click");
            sfxCashGain = sfxCashGain != null ? sfxCashGain : Resources.Load<AudioClip>("Audio/sfx_cash_gain");
            sfxCashLoss = sfxCashLoss != null ? sfxCashLoss : Resources.Load<AudioClip>("Audio/sfx_cash_loss");
            sfxStressUp = sfxStressUp != null ? sfxStressUp : Resources.Load<AudioClip>("Audio/sfx_stress_up");
            sfxSuccess = sfxSuccess != null ? sfxSuccess : Resources.Load<AudioClip>("Audio/sfx_success");
            sfxFail = sfxFail != null ? sfxFail : Resources.Load<AudioClip>("Audio/sfx_fail");
            sfxPayday = sfxPayday != null ? sfxPayday : Resources.Load<AudioClip>("Audio/sfx_payday");
        }

        private AudioClip ResolveBgm(BgmId id)
        {
            switch (id)
            {
                case BgmId.Main:
                    return bgmMain;
                case BgmId.Play:
                    return bgmPlay;
                case BgmId.Crisis:
                    return bgmCrisis;
                case BgmId.Result:
                    return bgmResult;
                default:
                    return null;
            }
        }

        private AudioClip ResolveSfx(SfxId id)
        {
            switch (id)
            {
                case SfxId.Click:
                    return sfxClick;
                case SfxId.CashGain:
                    return sfxCashGain;
                case SfxId.CashLoss:
                    return sfxCashLoss;
                case SfxId.StressUp:
                    return sfxStressUp;
                case SfxId.Success:
                    return sfxSuccess;
                case SfxId.Fail:
                    return sfxFail;
                case SfxId.Payday:
                    return sfxPayday;
                default:
                    return null;
            }
        }
    }
}
