namespace SurviveUntilPayday.Events
{
    public enum ChoicePhase
    {
        /// <summary>사건이 아직 시작되지 않음.</summary>
        NoActiveEvent = 0,

        /// <summary>선택지 입력 대기. 한 번만 선택 가능.</summary>
        AwaitingChoice = 1,

        /// <summary>결과 확정. 추가 선택 불가, 다음 날 진행 가능.</summary>
        ResultReady = 2
    }
}
