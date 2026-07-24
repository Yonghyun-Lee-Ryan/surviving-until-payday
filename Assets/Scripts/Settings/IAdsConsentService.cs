using System;
using UnityEngine;

namespace SurviveUntilPayday.Settings
{
    /// <summary>
    /// 광고 동의(UMP 대체 스텁). 실제 UMP SDK는 이후 심볼로 교체한다.
    /// </summary>
    public interface IAdsConsentService
    {
        bool HasCompletedFlow { get; }

        bool CanRequestAds { get; }

        void EnsureConsent(Action<bool> onCompleted);
    }

    public sealed class LocalAdsConsentService : IAdsConsentService
    {
        private readonly AppSettingsService settings;

        public LocalAdsConsentService(AppSettingsService settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool HasCompletedFlow => settings.ConsentFlowCompleted;

        public bool CanRequestAds => settings.AdsConsentGranted || Application.isEditor;

        public void EnsureConsent(Action<bool> onCompleted)
        {
            if (HasCompletedFlow)
            {
                onCompleted?.Invoke(CanRequestAds);
                return;
            }

            // UI(ConsentPanel)가 CompleteConsent를 호출한 뒤 다시 EnsureConsent를 호출한다.
            onCompleted?.Invoke(false);
        }
    }
}
