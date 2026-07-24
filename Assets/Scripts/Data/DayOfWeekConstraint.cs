namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 요일 필터. 사건 선택 엔진(개발 단위 5)에서 사용한다.
    /// </summary>
    public enum DayOfWeekConstraint
    {
        Any = 0,
        WeekdayOnly = 1,
        WeekendOnly = 2
    }
}
