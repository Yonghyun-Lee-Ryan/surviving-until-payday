using System;
using SurviveUntilPayday.Core;

namespace SurviveUntilPayday.UI
{
    public static class DayDisplayFormatter
    {
        public static string Format(int day, DayOfWeek dayOfWeek)
        {
            return $"{day}일 ({ToKorean(dayOfWeek)})";
        }

        public static string ToKorean(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return "월";
                case DayOfWeek.Tuesday:
                    return "화";
                case DayOfWeek.Wednesday:
                    return "수";
                case DayOfWeek.Thursday:
                    return "목";
                case DayOfWeek.Friday:
                    return "금";
                case DayOfWeek.Saturday:
                    return "토";
                case DayOfWeek.Sunday:
                    return "일";
                default:
                    return dayOfWeek.ToString();
            }
        }
    }
}
