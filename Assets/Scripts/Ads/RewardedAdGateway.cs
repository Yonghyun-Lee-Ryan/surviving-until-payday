using System;
using UnityEngine;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 쿼터·쿨다운 검사 후 IAdService를 호출하고, Completed일 때만 보상을 확정한다.
    /// </summary>
    public sealed class RewardedAdGateway
    {
        private readonly IAdService adService;
        private readonly AdQuotaTracker quota;
        private readonly IAdTelemetry telemetry;
        private bool requestInFlight;

        public RewardedAdGateway(
            IAdService adService,
            AdQuotaTracker quota,
            IAdTelemetry telemetry = null)
        {
            this.adService = adService ?? throw new ArgumentNullException(nameof(adService));
            this.quota = quota ?? throw new ArgumentNullException(nameof(quota));
            this.telemetry = telemetry;
        }

        public AdQuotaTracker Quota => quota;

        public bool CanRequest(RewardedAdPlacement placement, out string reason)
        {
            if (requestInFlight)
            {
                reason = "Ad request already in progress.";
                return false;
            }

            if (!quota.CanConsume(placement, out reason))
            {
                return false;
            }

            if (!adService.IsRewardedReady(placement))
            {
                reason = Application.internetReachability == NetworkReachability.NotReachable
                    ? "offline"
                    : "Rewarded ad is not ready.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// 광고를 요청한다. 실패/취소/미준비 시 보상을 지급하지 않으며 게임 진행을 막지 않는다.
        /// </summary>
        public void Request(
            RewardedAdPlacement placement,
            Action<AdRewardRequestResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!CanRequest(placement, out var reason))
            {
                var status = ResolveBlockedStatus(reason);
                onFinished(new AdRewardRequestResult(
                    new AdShowResult(status, reason),
                    reward: null));
                return;
            }

            telemetry?.OnRewardedOffered(placement);
            requestInFlight = true;
            telemetry?.OnRewardedStarted(placement);

            try
            {
                adService.ShowRewardedAd(placement, showResult =>
                {
                    requestInFlight = false;
                    AdRewardGrant? reward = null;
                    if (showResult.IsSuccess)
                    {
                        quota.ConsumeOnSuccess(placement);
                        reward = AdRewardGrant.ForPlacement(placement);
                        telemetry?.OnRewardedCompleted(placement);
                    }
                    else
                    {
                        Debug.Log(
                            $"[RewardedAdGateway] No reward for {placement}: {showResult.Status} ({showResult.Message})");
                    }

                    onFinished(new AdRewardRequestResult(showResult, reward));
                });
            }
            catch (Exception ex)
            {
                requestInFlight = false;
                Debug.LogWarning($"[RewardedAdGateway] ShowRewardedAd threw: {ex.Message}");
                onFinished(new AdRewardRequestResult(
                    AdShowResult.Failed(ex.Message),
                    reward: null));
            }
        }

        private static AdShowStatus ResolveBlockedStatus(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return AdShowStatus.Failed;
            }

            if (reason.IndexOf("cooldown", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AdShowStatus.OnCooldown;
            }

            if (reason.IndexOf("Quota", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AdShowStatus.QuotaExceeded;
            }

            if (reason.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AdShowStatus.NotReady;
            }

            return AdShowStatus.Failed;
        }
    }
}
