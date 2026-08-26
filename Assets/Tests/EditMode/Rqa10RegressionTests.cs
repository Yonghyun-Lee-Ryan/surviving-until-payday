using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Save;
using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa10RegressionTests
    {
        [Test]
        public void SettingsReset_KeepsConsentAndRestoresSound()
        {
            var store = new MemoryStore();
            var settings = new AppSettingsService(store);
            settings.CompleteConsent(true, true);
            settings.SoundEnabled = false;
            settings.ShowChoicePreview = true;

            settings.ResetToDefaultsKeepingConsent(keepConsent: true);

            Assert.IsTrue(settings.ConsentFlowCompleted);
            Assert.IsTrue(settings.AdsConsentGranted);
            Assert.IsTrue(settings.SoundEnabled);
            Assert.IsFalse(settings.ShowChoicePreview);
        }

        [Test]
        public void AdFailure_DoesNotThrow()
        {
            var mock = new MockAdService();
            mock.SetForceRewardedFailure(true, "rqa10");
            AdShowResult? result = null;
            Assert.DoesNotThrow(() => mock.ShowRewardedAd(RewardedAdPlacement.RetryOutcome, r => result = r));
            Assert.IsTrue(result.HasValue);
            Assert.IsFalse(result.Value.IsSuccess);
        }

        [Test]
        public void ContinueSave_RoundTripThenClear()
        {
            var repo = new SaveRepository(new InMemorySaveService());
            var save = SaveRepository.CreateDefault();
            save.run.hasActiveRun = true;
            save.run.currentDay = 11;
            repo.Save(save);

            var loaded = repo.LoadOrCreate();
            Assert.IsTrue(loaded.run.hasActiveRun);
            Assert.AreEqual(11, loaded.run.currentDay);

            repo.ClearRunAndSave(loaded);
            var cleared = repo.LoadOrCreate();
            Assert.IsFalse(cleared.run.hasActiveRun);
        }

        [Test]
        public void EmptyStateCopy_HasNoEnglishSceneName()
        {
            Assert.IsFalse(EmptyStateCopy.NoResultBody.ToLowerInvariant().Contains("game scene"));
            Assert.IsTrue(EmptyStateCopy.ContinueUnavailable.Contains("이어갈"));
        }

        private sealed class MemoryStore : IAppSettingsStore
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
