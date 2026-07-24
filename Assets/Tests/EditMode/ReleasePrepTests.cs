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
            settings.SoundVolume = 0.4f;
            settings.VibrationEnabled = false;

            var reloaded = new AppSettingsService(store);
            Assert.IsFalse(reloaded.SoundEnabled);
            Assert.AreEqual(0.4f, reloaded.SoundVolume, 0.001f);
            Assert.IsFalse(reloaded.VibrationEnabled);
            Assert.AreEqual(0f, AudioListener.volume, 0.001f);
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
            config.EditorSet("https://example.com/policy", "요약");
            Assert.AreEqual("https://example.com/policy", config.PolicyUrl);
            Assert.AreEqual("요약", config.SummaryText);
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
