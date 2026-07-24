using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// CompleteCurrentDay 결과. Scene 전환 정보는 포함하지 않는다.
    /// </summary>
    public sealed class DayAdvanceResult
    {
        public bool Accepted { get; }
        public string Message { get; }
        public int DayBefore { get; }
        public int DayAfter { get; }
        public bool WeeklySummaryTriggered { get; }
        public bool RunSucceeded { get; }
        public bool RunFailed { get; }
        public FailureReason FailureReason { get; }

        private DayAdvanceResult(
            bool accepted,
            string message,
            int dayBefore,
            int dayAfter,
            bool weeklySummaryTriggered,
            bool runSucceeded,
            bool runFailed,
            FailureReason failureReason)
        {
            Accepted = accepted;
            Message = message;
            DayBefore = dayBefore;
            DayAfter = dayAfter;
            WeeklySummaryTriggered = weeklySummaryTriggered;
            RunSucceeded = runSucceeded;
            RunFailed = runFailed;
            FailureReason = failureReason;
        }

        public static DayAdvanceResult Rejected(string message, int day)
        {
            return new DayAdvanceResult(false, message, day, day, false, false, false, FailureReason.None);
        }

        public static DayAdvanceResult Failed(int day, FailureReason reason)
        {
            return new DayAdvanceResult(
                true,
                $"Run failed: {reason}",
                day,
                day,
                false,
                false,
                true,
                reason);
        }

        public static DayAdvanceResult Succeeded(int day, bool weeklySummaryTriggered)
        {
            return new DayAdvanceResult(
                true,
                "Run succeeded",
                day,
                day,
                weeklySummaryTriggered,
                true,
                false,
                FailureReason.None);
        }

        public static DayAdvanceResult Advanced(int dayBefore, int dayAfter, bool weeklySummaryTriggered)
        {
            return new DayAdvanceResult(
                true,
                "Advanced to next day",
                dayBefore,
                dayAfter,
                weeklySummaryTriggered,
                false,
                false,
                FailureReason.None);
        }
    }
}
