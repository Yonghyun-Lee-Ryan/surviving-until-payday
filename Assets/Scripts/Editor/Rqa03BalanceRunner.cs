using System;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-03: 3차 밸런스 패스 적용(MVP+Unit23 팩) 후 BalancePass·QA Campaign 재측정.
    /// batchmode: -executeMethod SurviveUntilPayday.EditorTools.Rqa03BalanceRunner.RunFromBatch
    /// </summary>
    public static class Rqa03BalanceRunner
    {
        [MenuItem("Tools/Surviving Until Payday/Apply Balance Pass 3 + Measure (R-QA-03)")]
        public static void RunFromMenu()
        {
            var balancePath = ApplyAndMeasure();
            Debug.Log($"[R-QA-03] 완료. BalancePass={balancePath}");
        }

        public static void RunFromBatch()
        {
            try
            {
                var balancePath = ApplyAndMeasure();
                Debug.Log($"[R-QA-03] batch OK. BalancePass={balancePath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-03] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static string ApplyAndMeasure()
        {
            MvpEventPackFactory.CreateMvpEventPack();
            ContentPackUnit23Factory.CreateContentPack();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var balancePath = BalancePassRunner.RunAndSaveReport();
            ReleaseQaCampaignRunner.RunAndSaveReport();
            return balancePath;
        }
    }
}
