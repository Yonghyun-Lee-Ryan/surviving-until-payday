using NUnit.Framework;
using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class ReleasePrepTests
    {
        [Test]
        public void AppSettings_PersistsSoundAndVibration()
        {
            var store = new MemoryAppSettingsStore();
            var settings = new AppSettingsService(store);

            settings.SoundEnabled = false;
            settings.BgmVolume = 0.4f;
            settings.SfxVolume = 0.6f;
            settings.VibrationEnabled = false;

            var reloaded = new AppSettingsService(store);
            Assert.IsFalse(reloaded.SoundEnabled);
            Assert.AreEqual(0.4f, reloaded.BgmVolume, 0.001f);
            Assert.AreEqual(0.6f, reloaded.SfxVolume, 0.001f);
            Assert.IsFalse(reloaded.VibrationEnabled);
            Assert.AreEqual(0f, AudioListener.volume, 0.001f);
        }

        [Test]
        public void AppSettings_MigratesLegacySoundVolumeToBgmAndSfx()
        {
            var store = new MemoryAppSettingsStore();
            store.Save(new AppSettingsData
            {
                schemaVersion = 1,
                soundEnabled = true,
                soundVolume = 0.35f,
                bgmVolume = 0f,
                sfxVolume = 0f,
                vibrationEnabled = true
            });

            var settings = new AppSettingsService(store);
            Assert.AreEqual(0.35f, settings.BgmVolume, 0.001f);
            Assert.AreEqual(0.35f, settings.SfxVolume, 0.001f);
            Assert.AreEqual(4, settings.Current.schemaVersion);
            Assert.IsFalse(settings.ShowChoicePreview);
        }

        [Test]
        public void AppSettings_MigratesSchema3_DisablesChoicePreviewByDefault()
        {
            var store = new MemoryAppSettingsStore();
            store.Save(new AppSettingsData
            {
                schemaVersion = 3,
                soundEnabled = true,
                bgmVolume = 1f,
                sfxVolume = 1f,
                showChoicePreview = true
            });

            var settings = new AppSettingsService(store);
            Assert.IsFalse(settings.ShowChoicePreview);
            Assert.AreEqual(4, settings.Current.schemaVersion);
        }

        [Test]
        public void AppSettings_CompleteConsent_MarksFlowDone()
        {
            var settings = new AppSettingsService(new MemoryAppSettingsStore());
            Assert.IsFalse(settings.ConsentFlowCompleted);

            settings.CompleteConsent(privacyAccepted: true, adsConsentGranted: true);

            Assert.IsTrue(settings.ConsentFlowCompleted);
            Assert.IsTrue(settings.AdsConsentGranted);

            var consent = new LocalAdsConsentService(settings);
            Assert.IsTrue(consent.HasCompletedFlow);
            Assert.IsTrue(consent.CanRequestAds);
        }

        [Test]
        public void ConsentPanel_Show_DoesNotHideItselfOnFirstActivate()
        {
            var root = new GameObject("ConsentPanel");
            root.SetActive(false);
            var view = root.AddComponent<SurviveUntilPayday.UI.ConsentPanelView>();
            view.Bind(root, null, null, null, null);

            view.Show(() => { });

            Assert.IsTrue(root.activeSelf, "Show 직후 Awake가 Hide를 호출하면 스플래시에 멈춘다.");
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PrivacyPolicyConfig_ExposesUrl()
        {
            var config = ScriptableObject.CreateInstance<PrivacyPolicyConfig>();
            config.EditorSet(PrivacyPolicyUrls.Canonical, "요약");
            Assert.AreEqual(PrivacyPolicyUrls.Canonical, config.PolicyUrl);
            Assert.AreEqual("요약", config.SummaryText);
            Assert.IsFalse(config.HasPlaceholderUrl);
        }

        [Test]
        public void PrivacyPolicyUrls_ExampleCom_IsPlaceholder()
        {
            Assert.IsTrue(PrivacyPolicyUrls.IsPlaceholder("https://example.com/privacy"));
            Assert.IsFalse(PrivacyPolicyUrls.IsHttpsPublicUrl("https://example.com/privacy"));
            Assert.IsTrue(PrivacyPolicyUrls.IsHttpsPublicUrl(PrivacyPolicyUrls.Canonical));
        }

        private sealed class MemoryAppSettingsStore : IAppSettingsStore
        {
            private AppSettingsData data = new AppSettingsData();

            public AppSettingsData Load()
            {
                return JsonUtility.FromJson<AppSettingsData>(JsonUtility.ToJson(data));
            }

            public void Save(AppSettingsData value)
            {
                data = JsonUtility.FromJson<AppSettingsData>(JsonUtility.ToJson(value));
            }
        }
    }
}
