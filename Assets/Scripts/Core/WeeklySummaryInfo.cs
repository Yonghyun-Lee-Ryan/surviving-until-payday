using System;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    public sealed class WeeklySummaryInfo
    {
        public int WeekNumber { get; }
        public int Day { get; }
        public GameState StateSnapshot { get; }

        public WeeklySummaryInfo(int weekNumber, int day, GameState stateSnapshot)
        {
            WeekNumber = weekNumber;
            Day = day;
            StateSnapshot = stateSnapshot ?? throw new ArgumentNullException(nameof(stateSnapshot));
        }
    }
}
