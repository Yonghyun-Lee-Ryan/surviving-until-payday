namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 보상형 광고 배치. 게임 로직은 이 enum으로만 광고 종류를 구분한다.
    /// </summary>
    public enum RewardedAdPlacement
    {
        /// <summary>사건 선택 전 선택지 새로고침. 회차당 2회.</summary>
        ChoiceReroll = 0,

        /// <summary>위험 선택 실패 후 결과 재시도. 회차당 1회.</summary>
        RetryOutcome = 1,

        /// <summary>파산 직전 긴급 대출. 회차당 1회.</summary>
        EmergencyLoan = 2,

        /// <summary>하루 종료 부업 수익. 하루 1회.</summary>
        DailySideJob = 3,

        /// <summary>회차 종료 인생 경험치 2배. 회차당 1회.</summary>
        DoubleExperience = 4,

        /// <summary>무료 상점 특성 조각. 하루 3회.</summary>
        TraitFragment = 5
    }
}
