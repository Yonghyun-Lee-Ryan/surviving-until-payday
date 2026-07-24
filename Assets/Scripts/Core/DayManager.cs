using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 날짜·요일·난도·주간 결산일 판정. Scene과 무관한 순수 로직.
    /// </summary>
    public sealed class DayManager
    {
        public static readonly int[] WeeklySummaryDays = { 7, 14, 21 };

        private readonly GameState state;
        private readonly DayOfWeek dayOneWeekday;

        public DayManager(GameState state)
            : this(state, DayCalendar.DefaultDayOneWeekday)
        {
        }

        public DayManager(GameState state, DayOfWeek dayOneWeekday)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.dayOneWeekday = dayOneWeekday;
        }

        public GameState State => state;

        public int CurrentDay => state.CurrentDay;

        public DayOfWeek DayOneWeekday => dayOneWeekday;

        public DayOfWeek CurrentDayOfWeek => DayCalendar.GetDayOfWeek(CurrentDay, dayOneWeekday);

        public bool IsWeekend => DayCalendar.IsWeekend(CurrentDayOfWeek);

        public bool IsWeekday => !IsWeekend;

        public bool IsFinalDay => CurrentDay >= GameState.MaxDay;

        public float DifficultyMultiplier => DifficultyScaler.GetMultiplier(CurrentDay);

        /// <summary>
        /// EffectResolver가 선택 결과를 확정하면 true가 된다.
        /// </summary>
        public bool ReadyForNextDay { get; private set; }

        public void MarkReadyForNextDay()
        {
            ReadyForNextDay = true;
        }

        public void ResetReadyForNextDay()
        {
            ReadyForNextDay = false;
        }

        public bool IsWeeklySummaryDay()
        {
            return IsWeeklySummaryDay(CurrentDay);
        }

        /// <summary>28~29일: 월급 직전 위기 연출 구간.</summary>
        public bool IsLateCrisisDay()
        {
            return IsLateCrisisDay(CurrentDay);
        }

        public static bool IsWeeklySummaryDay(int day)
        {
            return day == 7 || day == 14 || day == 21;
        }

        public static bool IsLateCrisisDay(int day)
        {
            return day == 28 || day == 29;
        }

        public static int GetWeekNumber(int day)
        {
            if (day < GameState.MinDay || day > GameState.MaxDay)
            {
                throw new ArgumentOutOfRangeException(nameof(day), day, "Invalid day.");
            }

            return ((day - 1) / 7) + 1;
        }

        public bool CanAdvance()
        {
            return CurrentDay < GameState.MaxDay;
        }

        public bool TryAdvanceDay()
        {
            if (!CanAdvance())
            {
                return false;
            }

            state.CurrentDay += 1;
            return true;
        }

        public void SetDay(int day)
        {
            if (day < GameState.MinDay || day > GameState.MaxDay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    $"Day must be between {GameState.MinDay} and {GameState.MaxDay}.");
            }

            state.CurrentDay = day;
        }
    }
}
