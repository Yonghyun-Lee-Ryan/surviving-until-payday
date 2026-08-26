namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 빈 상태·폴백 문구 (R-QA-09). 버튼 라벨과 본문을 한곳에서 맞춘다.
    /// </summary>
    public static class EmptyStateCopy
    {
        public const string ContinueAvailable = "이어하기";
        public const string ContinueUnavailable = "이어갈 회차가 없습니다";

        public const string NoDailyMissions = "오늘은 등록된 미션이 없습니다";
        public const string NoDailyBest = "오늘의 베스트: 아직 기록이 없습니다";

        public const string NoTraitsHint =
            "해금된 특성이 없습니다. 「특성 없음」으로 시작할 수 있습니다.";

        public const string NoStatChanges = "능력치 변화가 없습니다";
        public const string NoResultData = "결과 데이터가 없습니다";
        public const string NoEndingData = "엔딩 데이터가 없습니다.";
        public const string NoResultBody = "게임 화면에서 회차를 마치면 결과가 표시됩니다.";

        public const string CodexEmptyList = "아직 표시할 항목이 없습니다.\n플레이로 도감을 채워 보세요.";
        public const string NoDescription = "설명이 없습니다.";
        public const string CodexLocked = "아직 해금되지 않은 항목입니다.";
    }
}
