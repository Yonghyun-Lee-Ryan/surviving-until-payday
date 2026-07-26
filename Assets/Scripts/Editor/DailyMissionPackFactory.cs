using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Unit 25: 일일 미션 풀(8개) 생성.
    /// </summary>
    public static class DailyMissionPackFactory
    {
        private const string MissionsFolder = "Assets/Resources/Missions";

        [MenuItem("Tools/Surviving Until Payday/Create Daily Mission Pack (Unit 25)")]
        public static void CreatePack()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(MissionsFolder);

            Create(
                "Mission_NoDelivery_10",
                "배달 없이 10일",
                "배달 음식을 주문하지 않고 10일 이상 버틴다.",
                DailyMissionGoalType.ForbiddenFlagThroughDays,
                0,
                10,
                RunFlags.OrderedDelivery,
                xp: 25,
                fragments: 1);

            Create(
                "Mission_Company_80",
                "회사 평가 80",
                "회차 종료 시 회사 평가 80 이상.",
                DailyMissionGoalType.MinCompanyScore,
                0,
                80,
                null,
                20,
                1);

            Create(
                "Mission_Cash_500k",
                "잔액 50만 생존",
                "현금 500,000원 이상 남기고 월급날까지 생존.",
                DailyMissionGoalType.MinCashOnEnd,
                500_000L,
                0,
                null,
                30,
                1);

            Create(
                "Mission_SideJob_3",
                "부업 세 번",
                "부업 광고 보상을 3회 수령한다.",
                DailyMissionGoalType.MinSideJobCount,
                0,
                3,
                null,
                20,
                1);

            Create(
                "Mission_Health_80_Payday",
                "건강 80 월급날",
                "건강 80 이상으로 월급날에 도달한다.",
                DailyMissionGoalType.MinHealthOnSuccess,
                0,
                80,
                null,
                25,
                1);

            Create(
                "Mission_Survive_15",
                "15일 생존",
                "15일 이상 버틴다.",
                DailyMissionGoalType.SurviveMinDays,
                0,
                15,
                null,
                20,
                1);

            Create(
                "Mission_Stress_40",
                "스트레스 관리",
                "종료 시 스트레스 40 이하.",
                DailyMissionGoalType.MaxStressOnEnd,
                0,
                40,
                null,
                15,
                1);

            Create(
                "Mission_Happiness_70",
                "행복도 70",
                "종료 시 행복도 70 이상.",
                DailyMissionGoalType.MinHappinessOnEnd,
                0,
                70,
                null,
                15,
                1);

            // 현금 미션은 생존도 요구 — 별도 SurviveSuccess와 조합은 평가기 단일 조건이므로
            // MinCash는 잔액만, 생존은 SurviveSuccess로 분리되어 있음. 잔액+생존은 완료 시 둘 다 배정될 수 있음.

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DailyMissionPackFactory] Missions created under {MissionsFolder}");
        }

        private static void Create(
            string fileName,
            string title,
            string description,
            DailyMissionGoalType type,
            long longThreshold,
            int intThreshold,
            string flagId,
            int xp,
            int fragments)
        {
            var path = $"{MissionsFolder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DailyMissionData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DailyMissionData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(
                fileName.ToLowerInvariant(),
                title,
                description,
                type,
                longThreshold,
                intThreshold,
                flagId,
                xp,
                fragments);
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
