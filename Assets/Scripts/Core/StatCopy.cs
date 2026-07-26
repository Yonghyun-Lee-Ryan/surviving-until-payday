using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 능력치 표시 이름·짧은 설명 (Unit 26).
    /// </summary>
    public static class StatCopy
    {
        public static string GetDisplayName(StatType type)
        {
            switch (type)
            {
                case StatType.Cash:
                    return "현금";
                case StatType.Health:
                    return "건강";
                case StatType.Stress:
                    return "스트레스";
                case StatType.Happiness:
                    return "행복도";
                case StatType.CompanyScore:
                    return "회사 평가";
                default:
                    return type.ToString();
            }
        }

        public static string GetDescription(StatType type)
        {
            switch (type)
            {
                case StatType.Cash:
                    return "생활비·선택에 쓰는 돈입니다. 0원 미만이면 파산합니다.";
                case StatType.Health:
                    return "몸 상태입니다. 0이 되면 병원에 입원해 회차가 끝납니다.";
                case StatType.Stress:
                    return "정신 부담입니다. 100에 도달하면 번아웃으로 실패합니다.";
                case StatType.Happiness:
                    return "삶의 만족도입니다. 선택과 엔딩 조건에 영향을 줍니다.";
                case StatType.CompanyScore:
                    return "직장에서의 평가입니다. 0이 되면 해고됩니다.";
                default:
                    return string.Empty;
            }
        }
    }
}
