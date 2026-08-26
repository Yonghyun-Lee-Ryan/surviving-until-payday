using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 선택 결과 숫자 아래에 붙는 짧은 드라마 한 줄.
    /// </summary>
    public static class ChoiceFeedbackCopy
    {
        public static string BuildDramaLine(ChoiceResult result)
        {
            if (result?.StatChanges == null || result.StatChanges.Count == 0)
            {
                return string.Empty;
            }

            long cashDelta = 0;
            long healthDelta = 0;
            long stressDelta = 0;
            long companyDelta = 0;
            for (var i = 0; i < result.StatChanges.Count; i++)
            {
                var change = result.StatChanges[i];
                if (!change.Changed)
                {
                    continue;
                }

                switch (change.StatType)
                {
                    case StatType.Cash:
                        cashDelta = change.ActualDelta;
                        break;
                    case StatType.Health:
                        healthDelta = change.ActualDelta;
                        break;
                    case StatType.Stress:
                        stressDelta = change.ActualDelta;
                        break;
                    case StatType.CompanyScore:
                        companyDelta = change.ActualDelta;
                        break;
                }
            }

            if (result.FailureAfter == FailureReason.Bankruptcy)
            {
                return "지갑이 바닥났습니다. 다음엔 큰 지출을 피하세요. 실패해도 경험치는 남습니다.";
            }

            if (cashDelta <= -80_000L)
            {
                return "통장이 한 꺼풀 얇아졌습니다. 다음 고정비를 남겨 두세요.";
            }

            if (healthDelta <= -8)
            {
                return "몸이 먼저 신호를 냅니다. 회복 선택을 잊지 마세요.";
            }

            if (stressDelta >= 10)
            {
                return "마음이 바빠졌습니다. 휴식 쪽을 한 번 섞어 보세요.";
            }

            if (companyDelta <= -8)
            {
                return "평가가 흔들립니다. 업무 태도를 챙길 타이밍입니다.";
            }

            if (cashDelta >= 50_000L)
            {
                return "당장은 이득입니다. 대신 다른 능력치가 깎였는지도 보세요.";
            }

            return string.Empty;
        }
    }
}
