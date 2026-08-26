using System;
using UnityEngine;

namespace SurviveUntilPayday.Settings
{
    /// <summary>
    /// 로컬 플레이어 설정. 개인 식별 정보 없이 PlayerPrefs에 저장한다.
    /// </summary>
    [Serializable]
    public sealed class AppSettingsData
    {
        public int schemaVersion = 4;
        public bool soundEnabled = true;
        /// <summary>구버전 호환용. schema 2부터는 bgm/sfx를 우선한다.</summary>
        public float soundVolume = 1f;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;
        public bool privacyAccepted;
        public bool adsConsentGranted;
        public bool consentFlowCompleted;
        public bool showChoicePreview;
    }

    public interface IAppSettingsStore
    {
        AppSettingsData Load();

        void Save(AppSettingsData data);
    }

    public sealed class PlayerPrefsAppSettingsStore : IAppSettingsStore
    {
        private const string Key = "survive_until_payday_app_settings_v1";

        public AppSettingsData Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                return new AppSettingsData();
            }

            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettingsData();
            }

            try
            {
                var data = JsonUtility.FromJson<AppSettingsData>(json);
                return data ?? new AppSettingsData();
            }
            catch (Exception)
            {
                return new AppSettingsData();
            }
        }

        public void Save(AppSettingsData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 사운드/진동/동의 상태를 적용·보관한다.
    /// </summary>
    public sealed class AppSettingsService
    {
        private const int CurrentSchema = 4;

        private readonly IAppSettingsStore store;
        private AppSettingsData data;

        public AppSettingsService(IAppSettingsStore store = null)
        {
            this.store = store ?? new PlayerPrefsAppSettingsStore();
            data = this.store.Load() ?? new AppSettingsData();
            MigrateIfNeeded();
            ApplyAudio();
        }

        public AppSettingsData Current => data;

        /// <summary>(enabled, bgmVolume, sfxVolume)</summary>
        public event Action<bool, float, float> AudioSettingsChanged;

        /// <summary>선택 미리보기(경향 표시) 켜짐/꺼짐.</summary>
        public event Action<bool> ChoicePreviewChanged;

        public bool SoundEnabled
        {
            get => data.soundEnabled;
            set
            {
                data.soundEnabled = value;
                ApplyAudio();
                Persist();
            }
        }

        /// <summary>구 API: BGM·SFX를 함께 설정한다.</summary>
        public float SoundVolume
        {
            get => (data.bgmVolume + data.sfxVolume) * 0.5f;
            set
            {
                var clamped = Mathf.Clamp01(value);
                data.soundVolume = clamped;
                data.bgmVolume = clamped;
                data.sfxVolume = clamped;
                ApplyAudio();
                Persist();
            }
        }

        public float BgmVolume
        {
            get => data.bgmVolume;
            set
            {
                data.bgmVolume = Mathf.Clamp01(value);
                data.soundVolume = (data.bgmVolume + data.sfxVolume) * 0.5f;
                ApplyAudio();
                Persist();
            }
        }

        public float SfxVolume
        {
            get => data.sfxVolume;
            set
            {
                data.sfxVolume = Mathf.Clamp01(value);
                data.soundVolume = (data.bgmVolume + data.sfxVolume) * 0.5f;
                ApplyAudio();
                Persist();
            }
        }

        public bool VibrationEnabled
        {
            get => data.vibrationEnabled;
            set
            {
                data.vibrationEnabled = value;
                Persist();
            }
        }

        public bool ShowChoicePreview
        {
            get => data.showChoicePreview;
            set
            {
                if (data.showChoicePreview == value)
                {
                    return;
                }

                data.showChoicePreview = value;
                Persist();
                ChoicePreviewChanged?.Invoke(value);
            }
        }

        public bool ConsentFlowCompleted => data.consentFlowCompleted;

        public bool AdsConsentGranted => data.adsConsentGranted;

        public void CompleteConsent(bool privacyAccepted, bool adsConsentGranted)
        {
            data.privacyAccepted = privacyAccepted;
            data.adsConsentGranted = adsConsentGranted;
            data.consentFlowCompleted = privacyAccepted;
            Persist();
        }

        public void ResetToDefaultsKeepingConsent(bool keepConsent)
        {
            var privacy = data.privacyAccepted;
            var ads = data.adsConsentGranted;
            var completed = data.consentFlowCompleted;
            data = new AppSettingsData();
            if (keepConsent)
            {
                data.privacyAccepted = privacy;
                data.adsConsentGranted = ads;
                data.consentFlowCompleted = completed;
            }

            ApplyAudio();
            Persist();
            ChoicePreviewChanged?.Invoke(data.showChoicePreview);
        }

        public void TryVibrate()
        {
            if (!data.vibrationEnabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#else
            Debug.Log("[AppSettings] Vibrate (editor/desktop no-op)");
#endif
        }

        private void MigrateIfNeeded()
        {
            data.bgmVolume = Mathf.Clamp01(data.bgmVolume);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
            if (data.schemaVersion >= CurrentSchema)
            {
                return;
            }

            if (data.schemaVersion < 2)
            {
                var legacy = Mathf.Clamp01(data.soundVolume);
                data.bgmVolume = legacy;
                data.sfxVolume = legacy;
            }

            // 기본은 끄기. 유저가 설정에서 직접 켠다.
            if (data.schemaVersion < 4)
            {
                data.showChoicePreview = false;
            }

            data.schemaVersion = CurrentSchema;
            Persist();
        }

        private void ApplyAudio()
        {
            var bgm = Mathf.Clamp01(data.bgmVolume);
            var sfx = Mathf.Clamp01(data.sfxVolume);
            // 구독자(AppRoot)가 없을 때도 테스트·에디터에서 마스터 뮤트가 반영되게 한다.
            AudioListener.volume = data.soundEnabled ? 1f : 0f;
            AudioSettingsChanged?.Invoke(data.soundEnabled, bgm, sfx);
        }

        private void Persist()
        {
            store.Save(data);
        }
    }
}
