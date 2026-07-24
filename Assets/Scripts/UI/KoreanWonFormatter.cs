using System.Globalization;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 한국 원화 표시 포맷.
    /// </summary>
    public static class KoreanWonFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ko-KR");

        public static string Format(long amount)
        {
            return string.Format(Culture, "{0:N0}원", amount);
        }

        public static string FormatDelta(long delta)
        {
            if (delta > 0)
            {
                return string.Format(Culture, "+{0:N0}원", delta);
            }

            if (delta < 0)
            {
                return string.Format(Culture, "{0:N0}원", delta);
            }

            return "0원";
        }
    }
}
