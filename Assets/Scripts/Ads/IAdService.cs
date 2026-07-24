using System;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 광고 SDK 추상화. 게임 로직은 이 인터페이스만 사용한다.
    /// </summary>
    public interface IAdService
    {
        bool IsRewardedReady(RewardedAdPlacement placement);

        bool IsInterstitialReady();

        /// <summary>
        /// 보상형 광고를 표시한다. 콜백은 완료/실패/취소 모두에서 호출된다.
        /// </summary>
        void ShowRewardedAd(RewardedAdPlacement placement, Action<AdShowResult> onFinished);

        /// <summary>
        /// 전면 광고를 표시한다. 로딩 실패 시 Failed로 콜백하며 게임 진행을 막지 않는다.
        /// </summary>
        void ShowInterstitial(Action<AdShowResult> onFinished);
    }
}
