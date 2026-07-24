using System;
using SurviveUntilPayday.Ads;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// AdMob 연동 지점. 심볼 GOOGLE_MOBILE_ADS가 없으면 테스트/Mock으로 폴백한다.
    /// 광고 로딩 실패 시에도 예외를 밖으로 던지지 않는다.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private readonly IAdService fallback;
        private bool loggedMissingSdk;

        public AdMobAdService(IAdService fallback)
        {
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
#if GOOGLE_MOBILE_ADS
            Debug.Log("[AdMobAdService] GOOGLE_MOBILE_ADS enabled.");
            // MobileAds.Initialize(_ => { });
#else
            LogMissingOnce();
#endif
        }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            try
            {
                // return rewardedAd != null && rewardedAd.CanShowAd();
                return fallback.IsRewardedReady(placement);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdMobAdService] IsRewardedReady failed: {ex.Message}");
                return false;
            }
#else
            return fallback.IsRewardedReady(placement);
#endif
        }

        public bool IsInterstitialReady()
        {
#if GOOGLE_MOBILE_ADS
            try
            {
                return fallback.IsInterstitialReady();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdMobAdService] IsInterstitialReady failed: {ex.Message}");
                return false;
            }
#else
            return fallback.IsInterstitialReady();
#endif
        }

        public void ShowRewardedAd(RewardedAdPlacement placement, Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            try
            {
#if GOOGLE_MOBILE_ADS
                // rewardedAd.Show(reward => onFinished(AdShowResult.Completed()));
                fallback.ShowRewardedAd(placement, onFinished);
#else
                LogMissingOnce();
                fallback.ShowRewardedAd(placement, onFinished);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdMobAdService] ShowRewardedAd failed: {ex.Message}");
                onFinished(AdShowResult.Failed(ex.Message));
            }
        }

        public void ShowInterstitial(Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            try
            {
#if GOOGLE_MOBILE_ADS
                fallback.ShowInterstitial(onFinished);
#else
                LogMissingOnce();
                fallback.ShowInterstitial(onFinished);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdMobAdService] ShowInterstitial failed: {ex.Message}");
                onFinished(AdShowResult.Failed(ex.Message));
            }
        }

        private void LogMissingOnce()
        {
            if (loggedMissingSdk)
            {
                return;
            }

            loggedMissingSdk = true;
            Debug.Log(
                "[AdMobAdService] GOOGLE_MOBILE_ADS 미정의. Test/Mock 광고로 폴백합니다. " +
                "Google Mobile Ads Unity 플러그인 설치 후 Scripting Define Symbols에 GOOGLE_MOBILE_ADS를 추가하세요.");
        }
    }
}
