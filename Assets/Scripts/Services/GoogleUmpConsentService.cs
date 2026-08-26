using System;
using SurviveUntilPayday.Settings;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Ump.Api;
#endif

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Google UMP. GOOGLE_MOBILE_ADS가 없으면 Local 동의로 폴백하고 게임을 막지 않는다.
    /// </summary>
    public sealed class GoogleUmpConsentService : IAdsConsentService
    {
        private readonly AppSettingsService settings;
        private readonly SdkIntegrationConfig config;

        public GoogleUmpConsentService(AppSettingsService settings, SdkIntegrationConfig config = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.config = config;
        }

        public bool HasCompletedFlow => settings.ConsentFlowCompleted;

        public bool CanRequestAds
        {
            get
            {
#if GOOGLE_MOBILE_ADS
                try
                {
                    return ConsentInformation.CanRequestAds();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UMP] CanRequestAds 조회 실패: {ex.Message}");
                    return settings.AdsConsentGranted || Application.isEditor;
                }
#else
                return settings.AdsConsentGranted || Application.isEditor;
#endif
            }
        }

        public void EnsureConsent(Action<bool> onCompleted)
        {
            if (onCompleted == null)
            {
                return;
            }

#if GOOGLE_MOBILE_ADS
            try
            {
                RequestUmp(onCompleted);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UMP] 동의 양식 실패, 게임은 진행합니다: {ex.Message}");
                onCompleted(true);
            }
#else
            onCompleted(true);
#endif
        }

#if GOOGLE_MOBILE_ADS
        private void RequestUmp(Action<bool> onCompleted)
        {
            var debug = new ConsentDebugSettings();
            if (config != null && config.UmpDebugForceEea)
            {
                debug.DebugGeography = DebugGeography.EEA;
            }

            if (config != null)
            {
                var ids = config.TestDeviceHashedIds;
                if (ids != null && ids.Length > 0)
                {
                    debug.TestDeviceHashedIds = new System.Collections.Generic.List<string>(ids);
                }
            }

            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
                ConsentDebugSettings = debug
            };

            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[UMP] Update 실패: {updateError.Message}");
                    onCompleted(true);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"[UMP] Form 실패: {formError.Message}");
                        onCompleted(true);
                        return;
                    }

                    var canRequest = true;
                    try
                    {
                        canRequest = ConsentInformation.CanRequestAds();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UMP] CanRequestAds: {ex.Message}");
                    }

                    onCompleted(canRequest);
                });
            });
        }
#endif
    }

    public static class AdsConsentFactory
    {
        public static IAdsConsentService Create(AppSettingsService settings, SdkIntegrationConfig config)
        {
#if GOOGLE_MOBILE_ADS
            return new GoogleUmpConsentService(settings, config);
#else
            return new LocalAdsConsentService(settings);
#endif
        }
    }
}
