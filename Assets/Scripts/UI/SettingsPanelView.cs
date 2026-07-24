using SurviveUntilPayday.Core;
using SurviveUntilPayday.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 메인 메뉴 설정: 사운드/진동/개인정보/데이터 초기화/버전.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button resetSaveButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text versionLabel;
        [SerializeField] private PrivacyPolicyConfig privacyConfig;

        private void Awake()
        {
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.AddListener(OnSoundChanged);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (privacyButton != null)
            {
                privacyButton.onClick.AddListener(OnPrivacyClicked);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.AddListener(OnResetSaveClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.RemoveListener(OnSoundChanged);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }

            if (privacyButton != null)
            {
                privacyButton.onClick.RemoveListener(OnPrivacyClicked);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.RemoveListener(OnResetSaveClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }

        public void Bind(
            GameObject panelRoot,
            Toggle sound,
            Toggle vibration,
            Slider volume,
            Button privacy,
            Button resetSave,
            Button close,
            Text version,
            PrivacyPolicyConfig config)
        {
            root = panelRoot;
            soundToggle = sound;
            vibrationToggle = vibration;
            volumeSlider = volume;
            privacyButton = privacy;
            resetSaveButton = resetSave;
            closeButton = close;
            versionLabel = version;
            privacyConfig = config;
        }

        public void Show()
        {
            RefreshFromSettings();
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (root != null && root.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void RefreshFromSettings()
        {
            var settings = AppRoot.Instance != null ? AppRoot.Instance.Settings : null;
            if (settings == null)
            {
                return;
            }

            if (soundToggle != null)
            {
                soundToggle.SetIsOnWithoutNotify(settings.SoundEnabled);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(settings.VibrationEnabled);
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(settings.SoundVolume);
            }

            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version} ({Application.platform})";
            }
        }

        private void OnSoundChanged(bool enabled)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.SoundEnabled = enabled;
        }

        private void OnVibrationChanged(bool enabled)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.VibrationEnabled = enabled;
            if (enabled)
            {
                settings.TryVibrate();
            }
        }

        private void OnVolumeChanged(float value)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.SoundVolume = value;
        }

        private void OnPrivacyClicked()
        {
            PrivacyPolicyOpener.Open(privacyConfig);
        }

        private void OnResetSaveClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.ResetAllSaveData();
            appRoot.Settings?.TryVibrate();
            Debug.Log("[Settings] 저장 데이터를 초기화했습니다.");
        }
    }
}
