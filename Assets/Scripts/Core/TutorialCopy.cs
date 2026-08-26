using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 첫 실행 튜토리얼 카피 (R-QA-06: 실패해도 OK · 안전만 고르기의 함정).
    /// </summary>
    public static class TutorialCopy
    {
        public static readonly string[] Titles =
        {
            "월급날까지 살아남기",
            "능력치를 챙기세요",
            "선택은 트레이드오프",
            "실패해도 됩니다",
            "준비됐다면 시작"
        };

        public static readonly string[] Bodies =
        {
            "30일 동안 현금·건강·스트레스·행복·회사 평가를 관리하며 월급날까지 버티는 게임입니다. 한 번에 완벽한 삶은 없습니다.",
            $"{StatCopy.GetDisplayName(StatType.Cash)}: {StatCopy.GetDescription(StatType.Cash)}\n" +
            $"{StatCopy.GetDisplayName(StatType.Health)}: {StatCopy.GetDescription(StatType.Health)}\n" +
            $"{StatCopy.GetDisplayName(StatType.Stress)}: {StatCopy.GetDescription(StatType.Stress)}\n" +
            $"{StatCopy.GetDisplayName(StatType.Happiness)}: {StatCopy.GetDescription(StatType.Happiness)}\n" +
            $"{StatCopy.GetDisplayName(StatType.CompanyScore)}: {StatCopy.GetDescription(StatType.CompanyScore)}",
            "맨 위(안전한) 선택만 고르면 월급날은 오기 쉽지만, 엔딩이 비슷해지고 위기를 배우지 못합니다. 돈을 벌면 건강·스트레스가 나빠질 수 있으니 당장의 이득과 다음 날을 함께 보세요.",
            "실패해도 됩니다. 파산·입원·번아웃·해고로 끝나도 인생 경험치와 도감은 남습니다. 상단 경고가 뜨면 위험 구간입니다. 설정에서 「선택 미리보기」를 켜면 선택지의 경향(현금↓ 등)을 볼 수 있습니다.",
            "새 게임으로 직업을 고르거나, 오늘의 직장인으로 같은 시드에 도전해 보세요. 이 안내는 다시 보지 않습니다."
        };

        public static bool TeachesFailureIsOk()
        {
            for (var i = 0; i < Bodies.Length; i++)
            {
                if (Bodies[i] != null && Bodies[i].IndexOf("실패해도") >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool WarnsSafeOnlyPath()
        {
            for (var i = 0; i < Bodies.Length; i++)
            {
                if (Bodies[i] != null && Bodies[i].IndexOf("안전한") >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
