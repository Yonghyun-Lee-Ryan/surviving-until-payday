using System.Text;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 엔딩 결과 공유 텍스트 (외부 SDK 없이 클립보드).
    /// </summary>
    public static class EndingShareCopy
    {
        public static string Build(ResultData result)
        {
            if (result == null)
            {
                return "월급날까지 살아남기";
            }

            var builder = new StringBuilder();
            builder.Append("월급날까지 살아남기");
            builder.Append('\n');
            if (result.Ending != null && !string.IsNullOrWhiteSpace(result.Ending.Title))
            {
                builder.Append('「');
                builder.Append(result.Ending.Title);
                builder.Append('」');
            }
            else
            {
                builder.Append(result.IsSuccess ? "월급날 생존" : "회차 종료");
            }

            builder.Append('\n');
            builder.Append(result.DaysSurvived);
            builder.Append("일 · ");
            if (result.FinalStats != null)
            {
                builder.Append(result.FinalStats.Cash.ToString("N0"));
                builder.Append("원");
            }

            if (result.IsSuccess)
            {
                builder.Append(" · 생존");
            }
            else
            {
                builder.Append(" · ");
                builder.Append(FailureReasonLabel(result.FailureReason));
            }

            builder.Append("\n#월급날까지살아남기");
            return builder.ToString();
        }

        public static string FailureReasonLabel(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.Bankruptcy:
                    return "파산";
                case FailureReason.Hospitalization:
                    return "입원";
                case FailureReason.Burnout:
                    return "번아웃";
                case FailureReason.Fired:
                    return "해고";
                default:
                    return "종료";
            }
        }
    }
}
