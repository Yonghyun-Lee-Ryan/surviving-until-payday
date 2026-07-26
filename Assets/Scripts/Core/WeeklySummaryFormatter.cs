using System.Collections.Generic;
using System.Text;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 주간 결산 팝업용 카피 생성.
    /// </summary>
    public static class WeeklySummaryFormatter
    {
        public static string BuildTitle(WeeklySummaryInfo info)
        {
            var week = info != null ? info.WeekNumber : 0;
            return $"{week}주차 결산";
        }

        public static string BuildBody(WeeklySummaryInfo info)
        {
            if (info?.StateSnapshot?.Stats == null)
            {
                return string.Empty;
            }

            var stats = info.StateSnapshot.Stats;
            var builder = new StringBuilder();
            builder.Append(info.Day);
            builder.Append("일까지의 상태를 점검합니다.\n");
            builder.Append("현금 ");
            builder.Append(stats.Cash.ToString("N0"));
            builder.Append("원\n");
            builder.Append("건강 ");
            builder.Append(stats.Health);
            builder.Append(" · 스트레스 ");
            builder.Append(stats.Stress);
            builder.Append("\n행복 ");
            builder.Append(stats.Happiness);
            builder.Append(" · 회사 ");
            builder.Append(stats.CompanyScore);
            return builder.ToString();
        }

        public static string BuildWarnings(WeeklySummaryInfo info)
        {
            if (info?.StateSnapshot?.Stats == null)
            {
                return string.Empty;
            }

            var stats = info.StateSnapshot.Stats;
            var warnings = new List<string>();
            if (stats.Cash < 200_000L)
            {
                warnings.Add("잔고가 빠듯합니다.");
            }

            if (stats.Health <= 30)
            {
                warnings.Add("건강을 챙기세요.");
            }

            if (stats.Stress >= 70)
            {
                warnings.Add("스트레스가 높습니다.");
            }

            if (stats.CompanyScore <= 30)
            {
                warnings.Add("회사 평가가 위험합니다.");
            }

            if (stats.Happiness <= 25)
            {
                warnings.Add("행복도가 낮습니다.");
            }

            return warnings.Count == 0
                ? "큰 위험 신호는 없습니다. 다음 주도 버텨 봅시다."
                : string.Join("\n", warnings);
        }
    }
}
