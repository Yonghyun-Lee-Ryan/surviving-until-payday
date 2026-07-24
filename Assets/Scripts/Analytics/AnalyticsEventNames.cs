namespace SurviveUntilPayday.Analytics
{
    /// <summary>
    /// snake_case 분석 이벤트 이름. 개인 식별 정보는 포함하지 않는다.
    /// </summary>
    public static class AnalyticsEventNames
    {
        public const string SessionStarted = "session_started";
        public const string SessionEnded = "session_ended";
        public const string RunStarted = "run_started";
        public const string EventShown = "event_shown";
        public const string ChoiceSelected = "choice_selected";
        public const string RunFailed = "run_failed";
        public const string RunCompleted = "run_completed";
        public const string RewardedAdOffered = "rewarded_ad_offered";
        public const string RewardedAdStarted = "rewarded_ad_started";
        public const string RewardedAdCompleted = "rewarded_ad_completed";
    }

    public static class AnalyticsParams
    {
        public const string Day = "day";
        public const string EventId = "event_id";
        public const string ChoiceIndex = "choice_index";
        public const string JobId = "job_id";
        public const string TraitId = "trait_id";
        public const string Seed = "seed";
        public const string FailureReason = "failure_reason";
        public const string DaysSurvived = "days_survived";
        public const string IsSuccess = "is_success";
        public const string Cash = "cash";
        public const string Placement = "placement";
        public const string DurationSeconds = "duration_seconds";
        public const string Continued = "continued";
    }
}
