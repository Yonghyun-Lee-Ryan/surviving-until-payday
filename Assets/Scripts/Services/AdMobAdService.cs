using System;
using SurviveUntilPayday.Ads;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// AdMob 연동. GOOGLE_MOBILE_ADS가 없으면 Test/Mock으로 폴백한다.
    /// 로드·표시 실패 시 예외를 밖으로 던지지 않고 Failed를 돌려 게임을 진행한다.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private readonly IAdService fallback;
        private readonly SdkIntegrationConfig config;
        private bool loggedMissingSdk;
        private static bool initializeStarted;

#if GOOGLE_MOBILE_ADS
        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;
#endif

        public AdMobAdService(IAdService fallback, SdkIntegrationConfig config = null)
        {
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            this.config = config;
#if GOOGLE_MOBILE_ADS
            EnsureInitialized();
            Debug.Log("[AdMobAdService] GOOGLE_MOBILE_ADS enabled.");
#else
            LogMissingOnce();
#endif
        }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
#if GOOGLE_MOBILE_ADS
            try
            {
                return rewardedAd != null && rewardedAd.CanShowAd();
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
                return interstitialAd != null && interstitialAd.CanShowAd();
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
                ShowRewardedInternal(onFinished);
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
                ShowInterstitialInternal(onFinished);
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

#if GOOGLE_MOBILE_ADS
        private void EnsureInitialized()
        {
            if (initializeStarted)
            {
                return;
            }

            initializeStarted = true;
            try
            {
                MobileAds.Initialize(_ =>
                {
                    PreloadRewarded();
                    PreloadInterstitial();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdMobAdService] Initialize failed: {ex.Message}");
            }
        }

        private string RewardedUnitId =>
            config != null ? config.RewardedAdUnitId : SdkIntegrationConfig.GoogleTestRewardedUnitId;

        private string InterstitialUnitId =>
            config != null ? config.InterstitialAdUnitId : SdkIntegrationConfig.GoogleTestInterstitialUnitId;

        private void PreloadRewarded()
        {
            RewardedAd.Load(RewardedUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdMobAdService] Rewarded load failed: {error?.GetMessage()}");
                    return;
                }

                rewardedAd = ad;
            });
        }

        private void PreloadInterstitial()
        {
            InterstitialAd.Load(InterstitialUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdMobAdService] Interstitial load failed: {error?.GetMessage()}");
                    return;
                }

                interstitialAd = ad;
            });
        }

        private void ShowRewardedInternal(Action<AdShowResult> onFinished)
        {
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                RewardedAd.Load(RewardedUnitId, new AdRequest(), (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        onFinished(AdShowResult.Failed(error != null ? error.GetMessage() : "rewarded not ready"));
                        return;
                    }

                    rewardedAd = ad;
                    PresentRewarded(onFinished);
                });
                return;
            }

            PresentRewarded(onFinished);
        }

        private void PresentRewarded(Action<AdShowResult> onFinished)
        {
            var granted = false;
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                PreloadRewarded();
                onFinished(granted ? AdShowResult.Completed() : AdShowResult.Cancelled());
            };
            rewardedAd.OnAdFullScreenContentFailed += adError =>
            {
                PreloadRewarded();
                onFinished(AdShowResult.Failed(adError != null ? adError.GetMessage() : "rewarded show failed"));
            };

            rewardedAd.Show(_ => granted = true);
        }

        private void ShowInterstitialInternal(Action<AdShowResult> onFinished)
        {
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                InterstitialAd.Load(InterstitialUnitId, new AdRequest(), (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        onFinished(AdShowResult.Failed(error != null ? error.GetMessage() : "interstitial not ready"));
                        return;
                    }

                    interstitialAd = ad;
                    PresentInterstitial(onFinished);
                });
                return;
            }

            PresentInterstitial(onFinished);
        }

        private void PresentInterstitial(Action<AdShowResult> onFinished)
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                PreloadInterstitial();
                onFinished(AdShowResult.Completed());
            };
            interstitialAd.OnAdFullScreenContentFailed += adError =>
            {
                PreloadInterstitial();
                onFinished(AdShowResult.Failed(adError != null ? adError.GetMessage() : "interstitial show failed"));
            };

            interstitialAd.Show();
        }
#endif

        private void LogMissingOnce()
        {
            if (loggedMissingSdk)
            {
                return;
            }

            loggedMissingSdk = true;
            Debug.Log(
                "[AdMobAdService] GOOGLE_MOBILE_ADS 미정의. Test/Mock 광고로 폴백합니다. " +
                "com.google.ads.mobile 설치 또는 Scripting Define에 GOOGLE_MOBILE_ADS를 추가하세요. " +
                $"테스트 유닛: {SdkIntegrationConfig.GoogleTestRewardedUnitId}");
        }
    }
}
