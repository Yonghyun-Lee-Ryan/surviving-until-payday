using SurviveUntilPayday.Ads;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 광고 버튼 비활성 사유 (한국어). 상점이 아닌 쿼터·쿨다운·미준비.
    /// </summary>
    public static class AdBlockReasonCopy
    {
        public static string QuotaExhausted(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.ChoiceReroll:
                    return "이번 회차 다른 사건 보기 한도 소진";
                case RewardedAdPlacement.RetryOutcome:
                    return "이번 회차 결과 재시도 한도 소진";
                case RewardedAdPlacement.EmergencyLoan:
                    return "이번 회차 긴급 대출 한도 소진";
                case RewardedAdPlacement.DailySideJob:
                    return "오늘 부업 광고 한도 소진";
                case RewardedAdPlacement.DoubleExperience:
                    return "이번 회차 경험치 2배 한도 소진";
                default:
                    return "광고 한도 소진";
            }
        }

        public static string Cooldown(double remainingSeconds)
        {
            var seconds = remainingSeconds < 1d ? 1 : (int)remainingSeconds + 1;
            return $"광고 쿨다운 {seconds}초 · 잠시 후 다시";
        }

        public static string NotReady => "광고가 아직 준비되지 않았습니다";

        public static string InFlight => "광고를 불러오는 중입니다";

        public static string AlreadyClaimed => "이미 경험치 2배를 받았습니다";

        public static string ServiceUnavailable => "광고 서비스를 사용할 수 없습니다";

        public static string Offline => "오프라인입니다. 본편은 계속할 수 있습니다";

        public static string FromGatewayReason(string reason, RewardedAdPlacement placement)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "지금은 광고를 볼 수 없습니다";
            }

            if (reason.IndexOf("cooldown", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "광고 쿨다운 중입니다. 잠시 후 다시 시도하세요.";
            }

            if (reason.IndexOf("Quota", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return QuotaExhausted(placement);
            }

            if (reason.IndexOf("not ready", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return NotReady;
            }

            if (reason.IndexOf("already in progress", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return InFlight;
            }

            if (reason.IndexOf("offline", System.StringComparison.OrdinalIgnoreCase) >= 0
                || reason.IndexOf("network", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Offline;
            }

            return "지금은 광고를 볼 수 없습니다";
        }

        public static string ButtonLabel(string readyLabel, bool interactable, string blockedReason)
        {
            if (interactable || string.IsNullOrWhiteSpace(blockedReason))
            {
                return readyLabel;
            }

            return blockedReason;
        }
    }
}
