using System;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// Editor/테스트용 Mock. 기본은 즉시 성공, 실패 모드를 설정할 수 있다.
    /// </summary>
    public sealed class MockAdService : IAdService
    {
        private bool rewardedReady = true;
        private bool interstitialReady = true;
        private bool forceRewardedFailure;
        private bool forceInterstitialFailure;
        private string failureMessage = "mock failure";

        public int RewardedShowCount { get; private set; }
        public int InterstitialShowCount { get; private set; }
        public RewardedAdPlacement? LastRewardedPlacement { get; private set; }

        public void SetRewardedReady(bool ready) => rewardedReady = ready;

        public void SetInterstitialReady(bool ready) => interstitialReady = ready;

        public void SetForceRewardedFailure(bool force, string message = null)
        {
            forceRewardedFailure = force;
            if (!string.IsNullOrEmpty(message))
            {
                failureMessage = message;
            }
        }

        public void SetForceInterstitialFailure(bool force, string message = null)
        {
            forceInterstitialFailure = force;
            if (!string.IsNullOrEmpty(message))
            {
                failureMessage = message;
            }
        }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
            return rewardedReady;
        }

        public bool IsInterstitialReady()
        {
            return interstitialReady;
        }

        public void ShowRewardedAd(RewardedAdPlacement placement, Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            LastRewardedPlacement = placement;
            RewardedShowCount++;

            if (!rewardedReady)
            {
                onFinished(AdShowResult.NotReady("Mock rewarded ad not ready."));
                return;
            }

            if (forceRewardedFailure)
            {
                onFinished(AdShowResult.Failed(failureMessage));
                return;
            }

            onFinished(AdShowResult.Completed());
        }

        public void ShowInterstitial(Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            InterstitialShowCount++;

            if (!interstitialReady)
            {
                onFinished(AdShowResult.NotReady("Mock interstitial not ready."));
                return;
            }

            if (forceInterstitialFailure)
            {
                onFinished(AdShowResult.Failed(failureMessage));
                return;
            }

            onFinished(AdShowResult.Completed());
        }
    }
}
