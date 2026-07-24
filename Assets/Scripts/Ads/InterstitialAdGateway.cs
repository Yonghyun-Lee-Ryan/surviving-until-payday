using System;
using UnityEngine;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 결과→메인 이동 시 전면 광고 정책. 로딩 실패해도 콜백만 Failed로 끝난다.
    /// </summary>
    public sealed class InterstitialAdGateway
    {
        public const int SkipFirstCompletedRuns = 3;
        public const int DefaultShowEveryNRuns = 3;

        private readonly IAdService adService;
        private readonly AdQuotaTracker quota;
        private int showEveryNRuns;

        private int completedRunCount;
        private bool removeInterstitials;

        public InterstitialAdGateway(
            IAdService adService,
            AdQuotaTracker quota,
            int showEveryNRuns = DefaultShowEveryNRuns)
        {
            this.adService = adService ?? throw new ArgumentNullException(nameof(adService));
            this.quota = quota ?? throw new ArgumentNullException(nameof(quota));
            SetShowEveryNRuns(showEveryNRuns);
        }

        public int CompletedRunCount => completedRunCount;

        public int ShowEveryNRuns => showEveryNRuns;

        public void SetShowEveryNRuns(int value)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            showEveryNRuns = value;
        }

        public void SetRemoveInterstitials(bool remove)
        {
            removeInterstitials = remove;
        }

        public void NotifyRunCompleted()
        {
            completedRunCount++;
        }

        public bool ShouldShowOnReturnToMenu(out string reason)
        {
            if (removeInterstitials)
            {
                reason = "Interstitials removed by purchase.";
                return false;
            }

            if (completedRunCount <= SkipFirstCompletedRuns)
            {
                reason = $"Skipping interstitial for first {SkipFirstCompletedRuns} runs.";
                return false;
            }

            if (quota.HasRewardedRecentlyForInterstitial)
            {
                reason = "Skipped because rewarded ad was just watched.";
                return false;
            }

            var indexAfterSkip = completedRunCount - SkipFirstCompletedRuns;
            if (indexAfterSkip < 1)
            {
                reason = "Not yet eligible.";
                return false;
            }

            // 스킵 이후 1, 1+N, 1+2N ... 번째 완료 회차에 노출
            if ((indexAfterSkip - 1) % showEveryNRuns != 0)
            {
                reason = $"Interstitial every {showEveryNRuns} runs after skip window.";
                return false;
            }

            if (!adService.IsInterstitialReady())
            {
                reason = "Interstitial not ready.";
                return false;
            }

            reason = null;
            return true;
        }

        public void TryShowOnReturnToMenu(Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!ShouldShowOnReturnToMenu(out var reason))
            {
                var status = reason != null
                             && reason.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0
                    ? AdShowStatus.NotReady
                    : AdShowStatus.Cancelled;
                onFinished(new AdShowResult(status, reason));
                return;
            }

            try
            {
                adService.ShowInterstitial(result =>
                {
                    if (result.IsSuccess)
                    {
                        quota.ClearInterstitialSkipFlag();
                    }
                    else
                    {
                        Debug.Log($"[InterstitialAdGateway] Show failed/skipped: {result.Message}");
                    }

                    onFinished(result);
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InterstitialAdGateway] ShowInterstitial threw: {ex.Message}");
                onFinished(AdShowResult.Failed(ex.Message));
            }
        }
    }
}
