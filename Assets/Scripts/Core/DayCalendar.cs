using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// Day 1 기준 요일 매핑과 주말 판정.
    /// 기본 규칙: Day 1 = Monday.
    /// </summary>
    public static class DayCalendar
    {
        public static readonly DayOfWeek DefaultDayOneWeekday = DayOfWeek.Monday;

        public static DayOfWeek GetDayOfWeek(int day)
        {
            return GetDayOfWeek(day, DefaultDayOneWeekday);
        }

        public static DayOfWeek GetDayOfWeek(int day, DayOfWeek dayOneWeekday)
        {
            if (day < GameState.MinDay)
            {
                throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be >= 1.");
            }

            var offset = (day - 1) % 7;
            var value = ((int)dayOneWeekday + offset) % 7;
            return (DayOfWeek)value;
        }

        public static bool IsWeekend(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
        }

        public static bool IsWeekend(int day)
        {
            return IsWeekend(GetDayOfWeek(day));
        }

        public static bool IsWeekend(int day, DayOfWeek dayOneWeekday)
        {
            return IsWeekend(GetDayOfWeek(day, dayOneWeekday));
        }

        public static bool IsWeekday(int day)
        {
            return !IsWeekend(day);
        }
    }
}
