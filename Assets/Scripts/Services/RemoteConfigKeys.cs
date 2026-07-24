namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Remote Config 키. Firebase Remote Config와 로컬 기본값이 동일한 키를 쓴다.
    /// </summary>
    public static class RemoteConfigKeys
    {
        /// <summary>스킵 구간 이후 전면 광고를 N회차마다 1회 노출.</summary>
        public const string InterstitialEveryNRuns = "interstitial_every_n_runs";

        /// <summary>보상형 광고 글로벌 쿨다운(초).</summary>
        public const string RewardedCooldownSeconds = "rewarded_cooldown_seconds";

        /// <summary>실제 SDK 사용 여부(원격 킬스위치).</summary>
        public const string UseRealAds = "use_real_ads";
    }
}
