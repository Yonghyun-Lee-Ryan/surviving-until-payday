using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Events
{
    /// <summary>
    /// 선택지 처리 결과. UI·이력·분석에서 공통으로 사용한다.
    /// </summary>
    public sealed class ChoiceResult
    {
        public int Day { get; }
        public string EventId { get; }
        public string EventTitle { get; }
        public int ChoiceIndex { get; }
        public string ChoiceId { get; }
        public string ChoiceText { get; }
        public string Message { get; }
        public string RandomOutcomeId { get; }
        public string RandomOutcomeMessage { get; }
        public PlayerStats StatsBefore { get; }
        public PlayerStats StatsAfter { get; }
        public IReadOnlyList<StatChangeResult> StatChanges { get; }
        public FailureReason FailureAfter { get; }

        public ChoiceResult(
            int day,
            string eventId,
            string eventTitle,
            int choiceIndex,
            string choiceId,
            string choiceText,
            string message,
            string randomOutcomeId,
            string randomOutcomeMessage,
            PlayerStats statsBefore,
            PlayerStats statsAfter,
            IReadOnlyList<StatChangeResult> statChanges,
            FailureReason failureAfter)
        {
            Day = day;
            EventId = eventId ?? string.Empty;
            EventTitle = eventTitle ?? string.Empty;
            ChoiceIndex = choiceIndex;
            ChoiceId = choiceId ?? string.Empty;
            ChoiceText = choiceText ?? string.Empty;
            Message = message ?? string.Empty;
            RandomOutcomeId = randomOutcomeId;
            RandomOutcomeMessage = randomOutcomeMessage;
            StatsBefore = statsBefore ?? new PlayerStats();
            StatsAfter = statsAfter ?? new PlayerStats();
            StatChanges = statChanges ?? System.Array.Empty<StatChangeResult>();
            FailureAfter = failureAfter;
        }
    }
}
