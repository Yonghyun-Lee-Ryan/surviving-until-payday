using System;
using UnityEngine;

namespace SurviveUntilPayday.Settings
{
    /// <summary>
    /// 광고 동의. 실기기는 UMP(`GOOGLE_MOBILE_ADS`), Editor/미설치는 Local 스텁.
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
            // 1차 동의 UI를 이미 통과한 뒤 호출된다. Mock 경로에서는 광고 요청을 허용한다.
            onCompleted?.Invoke(true);
        }
    }
}
