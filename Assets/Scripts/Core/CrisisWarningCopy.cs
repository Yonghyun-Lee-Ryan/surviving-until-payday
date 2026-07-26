using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// HUD 위기 배너용 경고 문구 (Unit 26).
    /// </summary>
    public static class CrisisWarningCopy
    {
        public const long LowCashThreshold = 200_000L;
        public const long CriticalCashThreshold = 50_000L;

        public static string HealthWarning => "건강 위험 · 0이 되면 입원";
        public static string StressWarning => "스트레스 경고 · 100이면 번아웃";
        public static string CompanyWarning => "해고 위기 · 평가 0이면 종료";
        public static string LateCrisis => "월급날 직전 · 선택 신중히";
        public static string LowCash => "현금 부족 · 큰 지출을 피하세요";
        public static string CriticalCash => "파산 직전 · 현금이 거의 없습니다";
    }
}
