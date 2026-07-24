namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 회차 실패 사유. 우선순위는 EvaluateFailure에서 정의한다.
    /// </summary>
    public enum FailureReason
    {
        None = 0,
        Bankruptcy = 1,
        Hospitalization = 2,
        Burnout = 3,
        Fired = 4
    }
}
