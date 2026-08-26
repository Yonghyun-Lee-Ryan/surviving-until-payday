using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Services;
using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa08SdkGateTests
    {
        [Test]
        public void AdsConsentFactory_WithoutMobileAdsDefine_UsesLocal()
        {
            var settings = new AppSettingsService(new MemoryAppSettingsStore());
            var consent = AdsConsentFactory.Create(settings, null);
            Assert.IsInstanceOf<LocalAdsConsentService>(consent);

            var granted = false;
            consent.EnsureConsent(ok => granted = ok);
            Assert.IsTrue(granted);
        }

        [Test]
        public void GoogleUmpConsentService_WithoutSdk_EnsureConsentSucceeds()
        {
            var settings = new AppSettingsService(new MemoryAppSettingsStore());
            var ump = new GoogleUmpConsentService(settings);
            var granted = false;
            ump.EnsureConsent(ok => granted = ok);
            Assert.IsTrue(granted);
        }

        [Test]
        public void AdMobAdService_ShowFailure_DoesNotThrow_AndDoesNotGrant()
        {
            var mock = new MockAdService();
            mock.SetForceRewardedFailure(true, "network");
            var admob = new AdMobAdService(mock);
            AdShowResult? result = null;
            Assert.DoesNotThrow(() =>
                admob.ShowRewardedAd(RewardedAdPlacement.ChoiceReroll, r => result = r));
            Assert.IsTrue(result.HasValue);
            Assert.IsFalse(result.Value.IsSuccess);
            Assert.AreEqual(AdShowStatus.Failed, result.Value.Status);
        }

        [Test]
        public void SdkDefines_ReportPackageSymbols()
        {
            Assert.AreEqual("GOOGLE_MOBILE_ADS", SdkDefines.GoogleMobileAds);
            Assert.AreEqual("FIREBASE_ANALYTICS", SdkDefines.FirebaseAnalytics);
            Assert.AreEqual("FIREBASE_CRASHLYTICS", SdkDefines.FirebaseCrashlytics);
        }

        [Test]
        public void PrivacyPolicyConfig_DefaultIsNotPlaceholder()
        {
            var config = ScriptableObject.CreateInstance<PrivacyPolicyConfig>();
            Assert.IsFalse(config.HasPlaceholderUrl);
            Assert.IsTrue(PrivacyPolicyUrls.IsHttpsPublicUrl(config.PolicyUrl));
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
