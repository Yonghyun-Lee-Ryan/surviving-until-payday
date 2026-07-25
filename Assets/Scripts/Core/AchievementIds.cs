using System.Collections.Generic;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 업적 id 상수. Unit 24: 핵심 20개.
    /// </summary>
    public static class AchievementIds
    {
        public const string FirstEnding = "ach_first_ending";
        public const string Survive7Days = "ach_survive_7";
        public const string Survive15Days = "ach_survive_15";
        public const string Survive30Days = "ach_survive_30";
        public const string CashHalfMillion = "ach_cash_500k";
        public const string CashOneMillion = "ach_cash_1m";
        public const string UnlockThreeTraits = "ach_traits_3";
        public const string HealthNinety = "ach_health_90";
        public const string StressTenOrLess = "ach_stress_10";
        public const string HappinessNinety = "ach_happiness_90";
        public const string CompanyNinety = "ach_company_90";
        public const string EventsTen = "ach_events_10";
        public const string EventsThirty = "ach_events_30";
        public const string EndingsFive = "ach_endings_5";
        public const string JobsTwo = "ach_jobs_2";
        public const string JobsThree = "ach_jobs_3";
        public const string CardJuggleEnding = "ach_ending_card_juggle";
        public const string OneBigShotEnding = "ach_ending_one_big_shot";
        public const string ResignReadyEnding = "ach_ending_resign_ready";
        public const string PaydaySuccess = "ach_payday_success";

        public const int CatalogCount = 20;

        public static IReadOnlyList<AchievementDefinition> Catalog { get; } = new[]
        {
            new AchievementDefinition(FirstEnding, "첫 엔딩", "엔딩을 한 번이라도 해금한다."),
            new AchievementDefinition(Survive7Days, "일주일 생존", "7일 이상 생존한다."),
            new AchievementDefinition(Survive15Days, "보름 생존", "15일 이상 생존한다."),
            new AchievementDefinition(Survive30Days, "월급날 생존", "30일까지 성공 생존한다."),
            new AchievementDefinition(CashHalfMillion, "50만 원", "회차 종료 시 현금 50만 원 이상."),
            new AchievementDefinition(CashOneMillion, "백만장자", "회차 종료 시 현금 100만 원 이상."),
            new AchievementDefinition(UnlockThreeTraits, "특성 수집가", "특성을 3개 이상 해금한다."),
            new AchievementDefinition(HealthNinety, "철인", "종료 시 건강 90 이상."),
            new AchievementDefinition(StressTenOrLess, "평온", "종료 시 스트레스 10 이하."),
            new AchievementDefinition(HappinessNinety, "행복회로", "종료 시 행복도 90 이상."),
            new AchievementDefinition(CompanyNinety, "핵심 인재", "종료 시 회사 평가 90 이상."),
            new AchievementDefinition(EventsTen, "사건 입문", "사건 도감 10개 해금."),
            new AchievementDefinition(EventsThirty, "사건 탐험가", "사건 도감 30개 해금."),
            new AchievementDefinition(EndingsFive, "엔딩 수집", "엔딩 5개 해금."),
            new AchievementDefinition(JobsTwo, "이직 준비", "직업 2개 해금."),
            new AchievementDefinition(JobsThree, "커리어 확장", "직업 3개 해금."),
            new AchievementDefinition(CardJuggleEnding, "돌려막기 달인", "카드 돌려막기 엔딩 달성."),
            new AchievementDefinition(OneBigShotEnding, "한방", "인생은 한방 엔딩 달성."),
            new AchievementDefinition(ResignReadyEnding, "퇴사 준비생", "퇴사 준비 완료 엔딩 달성."),
            new AchievementDefinition(PaydaySuccess, "월급날!", "월급날까지 성공 생존."),
        };

        public static string GetDisplayName(string id)
        {
            for (var i = 0; i < Catalog.Count; i++)
            {
                if (Catalog[i].Id == id)
                {
                    return Catalog[i].Title;
                }
            }

            return id ?? string.Empty;
        }
    }

    public readonly struct AchievementDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public AchievementDefinition(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }
}
