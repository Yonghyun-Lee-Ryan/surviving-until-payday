using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 결과 화면 “다음엔 이렇게” 팁 (Unit 26).
    /// </summary>
    public static class FailureTipCatalog
    {
        public static string GetTip(FailureReason reason, bool isSuccess)
        {
            return GetTip(reason, isSuccess, endingId: null);
        }

        public static string GetTip(FailureReason reason, bool isSuccess, string endingId)
        {
            if (isSuccess)
            {
                if (endingId == "ending_cash_king" || endingId == "ending_barely_survived")
                {
                    return "다음엔 이렇게: 안전만 고르면 월급날은 오지만 엔딩이 비슷합니다. 야근·지출·도박을 한 번씩 시험해 보세요. 실패해도 경험치와 도감은 남습니다.";
                }

                return "다음엔 이렇게: 위기 구간(후반)에는 회복·절약 선택을 미리 남겨 두세요. 다른 직업·특성으로 엔딩을 바꿔 보는 것도 좋습니다.";
            }

            switch (reason)
            {
                case FailureReason.Bankruptcy:
                    return "다음엔 이렇게: 현금이 빠듯하면 지출이 큰 선택과 빚을 피하고, 월급·부업으로 완충을 만드세요. 실패해도 경험치와 도감은 남습니다.";
                case FailureReason.Hospitalization:
                    return "다음엔 이렇게: 건강이 낮을 때는 무리한 야근·과로 선택을 줄이고 회복 사건을 고르세요. 입원해도 다음 회차에서 다시 배울 수 있습니다.";
                case FailureReason.Burnout:
                    return "다음엔 이렇게: 스트레스가 높을 때는 휴식·취미 쪽 선택을 섞어 100에 닿기 전에 낮추세요. 번아웃은 끝이자 힌트입니다.";
                case FailureReason.Fired:
                    return "다음엔 이렇게: 회사 평가가 떨어지면 업무·태도 관련 선택을 우선하고 무단·태만을 피하세요. 해고 기록도 도감에 남습니다.";
                default:
                    return "다음엔 이렇게: 능력치 균형을 보며, 한 능력치만 극단으로 깎이는 선택은 피하세요. 실패해도 됩니다.";
            }
        }
    }
}
