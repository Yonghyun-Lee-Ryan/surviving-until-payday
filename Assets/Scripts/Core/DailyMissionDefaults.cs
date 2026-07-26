using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 에셋이 없을 때 쓰는 런타임 일일 미션 풀.
    /// </summary>
    public static class DailyMissionDefaults
    {
        public static List<DailyMissionData> CreateRuntimePool()
        {
            return new List<DailyMissionData>
            {
                Create(
                    "mission_nodelivery_10",
                    "배달 없이 10일",
                    "배달 음식을 주문하지 않고 10일 이상 버틴다.",
                    DailyMissionGoalType.ForbiddenFlagThroughDays,
                    0,
                    10,
                    RunFlags.OrderedDelivery,
                    25,
                    1),
                Create(
                    "mission_company_80",
                    "회사 평가 80",
                    "회차 종료 시 회사 평가 80 이상.",
                    DailyMissionGoalType.MinCompanyScore,
                    0,
                    80,
                    null,
                    20,
                    1),
                Create(
                    "mission_cash_500k",
                    "잔액 50만",
                    "현금 500,000원 이상.",
                    DailyMissionGoalType.MinCashOnEnd,
                    500_000L,
                    0,
                    null,
                    30,
                    1),
                Create(
                    "mission_sidejob_3",
                    "부업 세 번",
                    "부업 보상을 3회 수령한다.",
                    DailyMissionGoalType.MinSideJobCount,
                    0,
                    3,
                    null,
                    20,
                    1),
                Create(
                    "mission_health_80_payday",
                    "건강 80 월급날",
                    "건강 80 이상으로 월급날 도달.",
                    DailyMissionGoalType.MinHealthOnSuccess,
                    0,
                    80,
                    null,
                    25,
                    1),
                Create(
                    "mission_survive_15",
                    "15일 생존",
                    "15일 이상 버틴다.",
                    DailyMissionGoalType.SurviveMinDays,
                    0,
                    15,
                    null,
                    20,
                    1),
                Create(
                    "mission_stress_40",
                    "스트레스 관리",
                    "종료 시 스트레스 40 이하.",
                    DailyMissionGoalType.MaxStressOnEnd,
                    0,
                    40,
                    null,
                    15,
                    1),
                Create(
                    "mission_happiness_70",
                    "행복도 70",
                    "종료 시 행복도 70 이상.",
                    DailyMissionGoalType.MinHappinessOnEnd,
                    0,
                    70,
                    null,
                    15,
                    1)
            };
        }

        private static DailyMissionData Create(
            string id,
            string title,
            string description,
            DailyMissionGoalType type,
            long longThreshold,
            int intThreshold,
            string flagId,
            int xp,
            int fragments)
        {
            var mission = ScriptableObject.CreateInstance<DailyMissionData>();
            mission.hideFlags = HideFlags.HideAndDontSave;
            mission.Configure(id, title, description, type, longThreshold, intThreshold, flagId, xp, fragments);
            return mission;
        }
    }
}
