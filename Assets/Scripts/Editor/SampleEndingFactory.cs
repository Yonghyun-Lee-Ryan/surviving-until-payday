using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 8: MVP 엔딩 샘플 생성.
    /// </summary>
    public static class SampleEndingFactory
    {
        private const string Folder = "Assets/Data/Endings";

        [MenuItem("Tools/Surviving Until Payday/Create Sample Endings (Unit 8/20)")]
        public static void CreateSampleEndings()
        {
            EnsureFolder(Folder);

            var catalog = new List<EndingData>
            {
                CreateSuccess(
                    "Ending_CashKing.asset",
                    "ending_cash_king",
                    "통장 잔고의 제왕",
                    "월급날까지 현금을 든든히 남겼다.",
                    priority: 73,
                    cashMin: 1_400_000L),
                CreateHealthyWorker(),
                CreatePromotionCandidate(),
                CreateHappyConsumer(),
                CreateOneBigShot(),
                CreateResignReady(),
                CreateCardJuggle(),
                CreateBarelySurvived(),
                CreateFailure(
                    "Ending_Bankruptcy.asset",
                    "ending_bankruptcy",
                    "파산",
                    "통장이 바닥나고 더 이상 버틸 수 없었다.",
                    FailureReason.Bankruptcy,
                    priority: 200),
                CreateFailure(
                    "Ending_Hospital.asset",
                    "ending_hospital",
                    "병원 입원",
                    "몸이 먼저 한계를 맞았다.",
                    FailureReason.Hospitalization,
                    priority: 200),
                CreateFailure(
                    "Ending_Burnout.asset",
                    "ending_burnout",
                    "번아웃",
                    "스트레스가 폭발했다.",
                    FailureReason.Burnout,
                    priority: 200),
                CreateFailure(
                    "Ending_Fired.asset",
                    "ending_fired",
                    "해고",
                    "회사 평가가 바닥을 쳤다.",
                    FailureReason.Fired,
                    priority: 200)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog[5];
            Debug.Log($"[SampleEndingFactory] Created/updated {catalog.Count} endings in {Folder}");
        }

        private static EndingData CreateHealthyWorker()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_HealthyWorker.asset");
            var condition = new EndingCondition();
            condition.EditorSetHealth(true, 70, false, 0);
            condition.EditorSetStress(false, 0, true, 40);
            ending.EditorSet(
                "ending_healthy_worker",
                "건강한 직장인",
                "몸과 마음을 지키며 월급날을 맞았다.",
                90,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreatePromotionCandidate()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_Promotion.asset");
            var condition = new EndingCondition();
            condition.EditorSetCompanyScore(true, 60, false, 0);
            condition.EditorSetFlags(new[] { RunFlags.PromotionTrack });
            ending.EditorSet(
                "ending_promotion",
                "승진 후보",
                "회사에서의 평판이 탄탄하다.",
                95,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateHappyConsumer()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_HappyConsumer.asset");
            var condition = new EndingCondition();
            condition.EditorSetHappiness(true, 80, false, 0);
            ending.EditorSet(
                "ending_happy_consumer",
                "행복한 소비왕",
                "힘들 때도 행복만큼은 챙겼다.",
                80,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateOneBigShot()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_OneBigShot.asset");
            var condition = new EndingCondition();
            condition.EditorSetCash(true, 500_000L, false, 0);
            condition.EditorSetHappiness(true, 70, false, 0);
            condition.EditorSetFlags(new[] { RunFlags.StockBigWin });
            ending.EditorSet(
                "ending_one_big_shot",
                "인생은 한방",
                "큰 승부에서 이겼다. 이번 달은 운이 따랐다.",
                75,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateResignReady()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_ResignReady.asset");
            var condition = new EndingCondition();
            condition.EditorSetCash(true, 800_000L, false, 0);
            condition.EditorSetCompanyScore(false, 0, true, 35);
            ending.EditorSet(
                "ending_resign_ready",
                "퇴사 준비 완료",
                "회사에서의 입지는 약해졌지만, 퇴사해도 될 만한 현금은 모아 두었다.",
                72,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateCardJuggle()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_CardJuggle.asset");
            var condition = new EndingCondition();
            condition.EditorSetHappiness(true, 40, false, 0);
            condition.EditorSetFlags(new[] { RunFlags.OwesDebt });
            ending.EditorSet(
                "ending_card_juggle",
                "카드 돌려막기",
                "비싼 대출을 카드로 돌려막으며 겨우 월급날까지 더더더 버텼다.",
                70,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateBarelySurvived()
        {
            var ending = LoadOrCreate($"{Folder}/Ending_BarelySurvived.asset");
            var condition = new EndingCondition();
            condition.EditorSetCash(false, 0, true, 900_000L);
            ending.EditorSet(
                "ending_barely_survived",
                "겨우 살아남았다",
                "아슬아슬했지만 월급날까지 버텼다.",
                8,
                false,
                FailureReason.None,
                condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateSuccess(
            string fileName,
            string id,
            string title,
            string description,
            int priority,
            long cashMin)
        {
            var ending = LoadOrCreate($"{Folder}/{fileName}");
            var condition = new EndingCondition();
            condition.EditorSetCash(true, cashMin, false, 0);
            ending.EditorSet(id, title, description, priority, false, FailureReason.None, condition);
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData CreateFailure(
            string fileName,
            string id,
            string title,
            string description,
            FailureReason reason,
            int priority)
        {
            var ending = LoadOrCreate($"{Folder}/{fileName}");
            ending.EditorSet(id, title, description, priority, true, reason, new EndingCondition());
            EditorUtility.SetDirty(ending);
            return ending;
        }

        private static EndingData LoadOrCreate(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EndingData>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<EndingData>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
