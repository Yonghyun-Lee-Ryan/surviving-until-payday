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
        public bool soundEnabled = true;
        public float soundVolume = 1f;
        public bool vibrationEnabled = true;
        public bool privacyAccepted;
        public bool adsConsentGranted;
        public bool consentFlowCompleted;
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
        private readonly IAppSettingsStore store;
        private AppSettingsData data;

        public AppSettingsService(IAppSettingsStore store = null)
        {
            this.store = store ?? new PlayerPrefsAppSettingsStore();
            data = this.store.Load() ?? new AppSettingsData();
            ApplyAudio();
        }

        public AppSettingsData Current => data;

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

        public float SoundVolume
        {
            get => data.soundVolume;
            set
            {
                data.soundVolume = Mathf.Clamp01(value);
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

        private void ApplyAudio()
        {
            AudioListener.volume = data.soundEnabled ? Mathf.Clamp01(data.soundVolume) : 0f;
        }

        private void Persist()
        {
            store.Save(data);
        }
    }
}
