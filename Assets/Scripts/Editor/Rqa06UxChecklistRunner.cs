using System;
using System.IO;
using System.Text;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-06: 튜토리얼·모달 레이어·공유 카피·DebugPanel Release 가드 체크리스트.
    /// batch: -executeMethod SurviveUntilPayday.EditorTools.Rqa06UxChecklistRunner.RunFromBatch
    /// </summary>
    public static class Rqa06UxChecklistRunner
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DebugPanelSource = "Assets/Scripts/Debug/DebugPanel.cs";

        [MenuItem("Tools/Surviving Until Payday/Run UX Checklist (R-QA-06)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[R-QA-06] UX 체크리스트 완료. {path}");
        }

        public static void RunFromBatch()
        {
            try
            {
                var path = RunAndSaveReport();
                Debug.Log($"[R-QA-06] batch OK. {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-06] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static string RunAndSaveReport()
        {
            var report = new StringBuilder();
            report.AppendLine("R-QA-06 UX Checklist");
            report.AppendLine($"Unity {Application.unityVersion}");
            report.AppendLine($"When {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("Shop/IAP: not used");
            report.AppendLine();

            var failures = new System.Collections.Generic.List<string>();

            CheckCopy(report, failures);
            CheckRuntimeLayer(report, failures);
            CheckScenes(report, failures);
            CheckDebugPanelReleaseGuard(report, failures);

            report.AppendLine();
            if (failures.Count == 0)
            {
                report.AppendLine("RESULT: PASS");
            }
            else
            {
                report.AppendLine("RESULT: FAIL");
                for (var i = 0; i < failures.Count; i++)
                {
                    report.AppendLine($"  - {failures[i]}");
                }
            }

            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(logs);
            var file = Path.Combine(logs, $"rqa06_ux_checklist_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(file, report.ToString(), Encoding.UTF8);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException("[R-QA-06] 체크리스트 실패. " + file);
            }

            return file;
        }

        private static void CheckCopy(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 카피 (G6/G9/G10) ---");
            Expect(failures, TutorialCopy.TeachesFailureIsOk(), "튜토리얼에 '실패해도'가 없습니다.");
            Expect(failures, TutorialCopy.WarnsSafeOnlyPath(), "튜토리얼에 안전-only 경고가 없습니다.");
            report.AppendLine($"  Tutorial steps: {TutorialCopy.Titles.Length}");
            report.AppendLine($"  Failure-is-ok: {TutorialCopy.TeachesFailureIsOk()}");
            report.AppendLine($"  Safe-only trap: {TutorialCopy.WarnsSafeOnlyPath()}");

            var share = EndingShareCopy.Build(null);
            Expect(failures, share.IndexOf("월급날까지", StringComparison.Ordinal) >= 0, "공유 폴백 카피가 비어 있습니다.");
            report.AppendLine("  EndingShareCopy fallback OK");

            var quota = AdBlockReasonCopy.QuotaExhausted(RewardedAdPlacement.ChoiceReroll);
            Expect(failures, quota.IndexOf("한도", StringComparison.Ordinal) >= 0, "광고 한도 사유가 한글이 아닙니다.");
            report.AppendLine($"  Ad block: {quota}");

            var tip = FailureTipCatalog.GetTip(FailureReason.None, true, "ending_cash_king");
            Expect(failures, tip.IndexOf("안전만", StringComparison.Ordinal) >= 0, "cash_king 팁에 안전만 경고가 없습니다.");
            report.AppendLine("  cash_king tip warns safe-only");
            report.AppendLine();
        }

        private static void CheckRuntimeLayer(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 런타임 레이어 (HUD vs Result/Weekly) ---");
            var canvas = new GameObject("Rqa06ChecklistCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hudGo = new GameObject("HUD", typeof(RectTransform));
                hudGo.transform.SetParent(canvas.transform, false);
                var hud = hudGo.AddComponent<GameHudView>();
                var resultGo = new GameObject("ResultPopup", typeof(RectTransform));
                resultGo.transform.SetParent(canvas.transform, false);
                var result = resultGo.AddComponent<ResultPopupView>();
                var weeklyGo = new GameObject("WeeklySummaryPopup", typeof(RectTransform));
                weeklyGo.transform.SetParent(canvas.transform, false);
                var weekly = weeklyGo.AddComponent<WeeklySummaryPopupView>();

                hudGo.transform.SetAsLastSibling();
                resultGo.SetActive(true);
                weeklyGo.SetActive(false);
                GameplayLayoutApplier.Apply(hud, null, null);
                Expect(
                    failures,
                    UiModalLayer.IsInFrontOf(resultGo.transform, hudGo.transform),
                    "GameplayLayoutApplier 후 ResultPopup이 HUD 뒤에 있습니다.");

                result.Show("결과", "메시지", "변화", "다음 날");
                weekly.Show("1주차 결산", "본문", "경고");
                UiModalLayer.RestackModalsAboveHud(hudGo.transform, result, weekly);
                Expect(
                    failures,
                    UiModalLayer.IsInFrontOf(weeklyGo.transform, hudGo.transform),
                    "주간결산 Show 후 Weekly가 HUD 뒤에 있습니다.");
                Expect(
                    failures,
                    UiModalLayer.IsInFrontOf(weeklyGo.transform, resultGo.transform)
                    || UiModalLayer.IsInFrontOf(resultGo.transform, hudGo.transform),
                    "활성 모달이 HUD보다 앞에 있어야 합니다.");

                report.AppendLine(
                    $"  HUD sibling={hudGo.transform.GetSiblingIndex()} " +
                    $"Result={resultGo.transform.GetSiblingIndex()} " +
                    $"Weekly={weeklyGo.transform.GetSiblingIndex()}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }

            report.AppendLine();
        }

        private static void CheckScenes(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 씬 바인딩 ---");
            InspectGameScene(report, failures);
            InspectScene(
                report,
                failures,
                "Assets/Scenes/Bootstrap.unity",
                "Bootstrap",
                typeof(SurviveUntilPayday.UI.ConsentPanelView),
                typeof(SurviveUntilPayday.UI.SplashController));
            InspectScene(
                report,
                failures,
                MainMenuScenePath,
                "MainMenu",
                typeof(SurviveUntilPayday.UI.MainMenuController));
            InspectScene(
                report,
                failures,
                ResultScenePath,
                "Result",
                typeof(SurviveUntilPayday.UI.ResultPresenter));
            report.AppendLine();
        }

        private static void InspectGameScene(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            if (!File.Exists(GameScenePath))
            {
                failures.Add("Game.unity가 없습니다.");
                return;
            }

            var opened = OpenSceneAdditive(GameScenePath, out var shouldClose);
            try
            {
                var hud = FindInScene<GameHudView>(opened);
                var result = FindInScene<ResultPopupView>(opened);
                var weekly = FindInScene<WeeklySummaryPopupView>(opened);
                var debug = FindInScene<DebugPanel>(opened);
                Expect(failures, hud != null, "Game 씬에 GameHudView가 없습니다.");
                Expect(failures, result != null, "Game 씬에 ResultPopupView가 없습니다.");
                Expect(failures, weekly != null, "Game 씬에 WeeklySummaryPopupView가 없습니다.");
                Expect(failures, debug != null, "Game 씬에 DebugPanel이 없습니다(에디터에서만 유지).");
                report.AppendLine(
                    $"  Game.unity HUD={hud != null} Result={result != null} Weekly={weekly != null} DebugPanel={debug != null}");
                if (hud != null && result != null)
                {
                    var sameParent = hud.transform.parent == result.transform.parent;
                    report.AppendLine($"  HUD/Result same parent: {sameParent}");
                    Expect(failures, sameParent, "Game 씬에서 HUD와 ResultPopup 부모가 다릅니다.");
                }
            }
            finally
            {
                if (shouldClose && opened.IsValid())
                {
                    EditorSceneManager.CloseScene(opened, true);
                }
            }
        }

        private static void InspectScene(
            StringBuilder report,
            System.Collections.Generic.List<string> failures,
            string path,
            string label,
            params Type[] required)
        {
            if (!File.Exists(path))
            {
                failures.Add($"{label} 씬이 없습니다: {path}");
                return;
            }

            var opened = OpenSceneAdditive(path, out var shouldClose);
            try
            {
                for (var i = 0; i < required.Length; i++)
                {
                    var found = false;
                    var roots = opened.GetRootGameObjects();
                    for (var r = 0; r < roots.Length && !found; r++)
                    {
                        found = roots[r].GetComponentInChildren(required[i], true) != null;
                    }

                    Expect(failures, found, $"{label} 씬에 {required[i].Name}가 없습니다.");
                    report.AppendLine($"  {label}: {required[i].Name}={found}");
                }
            }
            finally
            {
                if (shouldClose && opened.IsValid())
                {
                    EditorSceneManager.CloseScene(opened, true);
                }
            }
        }

        private static Scene OpenSceneAdditive(string path, out bool shouldClose)
        {
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
            {
                shouldClose = false;
                return existing;
            }

            shouldClose = true;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CheckDebugPanelReleaseGuard(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- DebugPanel Release 가드 ---");
            Expect(failures, DebugPanel.IsIncludedInThisBuild, "에디터에서 DebugPanel.IsIncludedInThisBuild가 false입니다.");
            report.AppendLine($"  Editor include: {DebugPanel.IsIncludedInThisBuild}");

            var source = File.Exists(DebugPanelSource)
                ? File.ReadAllText(DebugPanelSource)
                : string.Empty;
            Expect(
                failures,
                source.IndexOf("#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)", StringComparison.Ordinal) >= 0
                && source.IndexOf("Destroy(gameObject)", StringComparison.Ordinal) >= 0,
                "DebugPanel Release Destroy 가드가 소스에 없습니다.");
            report.AppendLine("  Release Destroy guard present in source");
            report.AppendLine("  Manual: Player Release 빌드에서 F1 Debug 버튼이 없어야 함 (Docs/Rqa06UxChecklist.md)");
        }

        private static void Expect(System.Collections.Generic.List<string> failures, bool ok, string message)
        {
            if (!ok)
            {
                failures.Add(message);
            }
        }
    }
}
