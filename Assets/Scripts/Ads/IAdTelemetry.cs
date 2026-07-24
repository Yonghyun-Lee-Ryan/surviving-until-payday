namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 광고 게이트웨이가 분석 파사드에 위임할 때 사용. Ads는 구체 Analytics SDK를 모른다.
    /// </summary>
    public interface IAdTelemetry
    {
        void OnRewardedOffered(RewardedAdPlacement placement);
        void OnRewardedStarted(RewardedAdPlacement placement);
        void OnRewardedCompleted(RewardedAdPlacement placement);
    }
}
