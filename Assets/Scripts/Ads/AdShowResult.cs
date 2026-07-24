namespace SurviveUntilPayday.Ads
{
    public enum AdShowStatus
    {
        Completed = 0,
        Failed = 1,
        Cancelled = 2,
        NotReady = 3,
        QuotaExceeded = 4,
        OnCooldown = 5
    }

    /// <summary>
    /// SDK/Mock이 광고 표시 후 반환하는 결과. 보상은 Completed일 때만 지급한다.
    /// </summary>
    public readonly struct AdShowResult
    {
        public AdShowStatus Status { get; }
        public string Message { get; }
        public bool IsSuccess => Status == AdShowStatus.Completed;

        public AdShowResult(AdShowStatus status, string message = null)
        {
            Status = status;
            Message = message ?? string.Empty;
        }

        public static AdShowResult Completed() => new AdShowResult(AdShowStatus.Completed, "completed");

        public static AdShowResult Failed(string message) =>
            new AdShowResult(AdShowStatus.Failed, message ?? "failed");

        public static AdShowResult Cancelled() => new AdShowResult(AdShowStatus.Cancelled, "cancelled");

        public static AdShowResult NotReady(string message = null) =>
            new AdShowResult(AdShowStatus.NotReady, message ?? "not ready");

        public static AdShowResult QuotaExceeded(string message = null) =>
            new AdShowResult(AdShowStatus.QuotaExceeded, message ?? "quota exceeded");

        public static AdShowResult OnCooldown(string message = null) =>
            new AdShowResult(AdShowStatus.OnCooldown, message ?? "on cooldown");
    }
}
