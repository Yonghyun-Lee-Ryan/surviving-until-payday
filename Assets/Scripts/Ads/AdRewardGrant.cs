namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 광고 완료 시 지급되는 보상 설명. UI/게임 로직이 해석한다.
    /// </summary>
    public readonly struct AdRewardGrant
    {
        public RewardedAdPlacement Placement { get; }
        public long CashDelta { get; }
        public bool DoubleExperience { get; }
        public bool ChoiceReroll { get; }
        public bool RetryOutcome { get; }
        public int TraitFragments { get; }
        public string Description { get; }

        public AdRewardGrant(
            RewardedAdPlacement placement,
            long cashDelta = 0,
            bool doubleExperience = false,
            bool choiceReroll = false,
            bool retryOutcome = false,
            int traitFragments = 0,
            string description = null)
        {
            Placement = placement;
            CashDelta = cashDelta;
            DoubleExperience = doubleExperience;
            ChoiceReroll = choiceReroll;
            RetryOutcome = retryOutcome;
            TraitFragments = traitFragments;
            Description = description ?? string.Empty;
        }

        public static AdRewardGrant ForPlacement(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.ChoiceReroll:
                    return new AdRewardGrant(
                        placement,
                        choiceReroll: true,
                        description: "선택지 새로고침");
                case RewardedAdPlacement.RetryOutcome:
                    return new AdRewardGrant(
                        placement,
                        retryOutcome: true,
                        description: "결과 재시도");
                case RewardedAdPlacement.EmergencyLoan:
                    return new AdRewardGrant(
                        placement,
                        cashDelta: 100_000L,
                        description: "긴급 대출 100,000원");
                case RewardedAdPlacement.DailySideJob:
                    return new AdRewardGrant(
                        placement,
                        cashDelta: 30_000L,
                        description: "부업 수익 30,000원");
                case RewardedAdPlacement.DoubleExperience:
                    return new AdRewardGrant(
                        placement,
                        doubleExperience: true,
                        description: "인생 경험치 2배");
                case RewardedAdPlacement.TraitFragment:
                    return new AdRewardGrant(
                        placement,
                        traitFragments: 1,
                        description: "특성 조각 1개");
                default:
                    return new AdRewardGrant(placement, description: "unknown");
            }
        }
    }

    public readonly struct AdRewardRequestResult
    {
        public AdShowResult ShowResult { get; }
        public AdRewardGrant? Reward { get; }
        public bool RewardGranted => Reward.HasValue && ShowResult.IsSuccess;

        public AdRewardRequestResult(AdShowResult showResult, AdRewardGrant? reward)
        {
            ShowResult = showResult;
            Reward = reward;
        }
    }
}
