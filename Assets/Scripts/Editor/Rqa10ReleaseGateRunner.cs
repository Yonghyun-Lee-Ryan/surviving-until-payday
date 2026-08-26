using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Save;
using SurviveUntilPayday.Settings;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-10: 전체 회귀 게이트. versionCode를 올리지 않고, 상점/IAP를 복구하지 않는다.
    /// batch: -executeMethod SurviveUntilPayday.EditorTools.Rqa10ReleaseGateRunner.RunFromBatch
    /// </summary>
    public static class Rqa10ReleaseGateRunner
    {
        /// <summary>
        /// R-QA-03이 도달한 Day30 유지 밴드. 기획 15~35%는 startingCash=월급 구조상 이벤트만으로 한계.
        /// </summary>
        public const double MaintainDay30Low = 0.50;
        public const double MaintainDay30High = 0.85;
        public const double MaxDay1Fail = 0.05;
        public const int ExpectedCampaignRuns = 2000;

        [MenuItem("Tools/Surviving Until Payday/Run Release Gate (R-QA-10)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[R-QA-10] 게이트 리포트: {path}");
        }

        public static void RunFromBatch()
        {
            try
            {
                var path = RunAndSaveReport();
                Debug.Log($"[R-QA-10] batch OK: {path}");
                EditorApplication.Exit(LastFailed ? 1 : 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-10] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static bool LastFailed { get; private set; }
        public static string LastReportPath { get; private set; }

        public static string RunAndSaveReport()
        {
            LastFailed = false;
            var failures = new List<string>();
            var report = new StringBuilder();
            report.AppendLine("=== R-QA-10 Release Gate ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Unity: {Application.unityVersion}");
            report.AppendLine("Shop/IAP: not restored");
            report.AppendLine("versionCode: not bumped");
            report.AppendLine();

            RunSweep(report, failures);
            RunBalance(report, failures);
            RunCampaign(report, failures);
            RunPlayFlow(report, failures);
            RunNestedChecklists(report, failures);
            CheckAabModule(report, failures);

            report.AppendLine();
            report.AppendLine("## Known issues (non-blocking)");
            report.AppendLine("- T-P2-01 freelancer+Risky 극단: Docs/Rqa09Polish.md. 캠페인 실패 조건 아님.");
            report.AppendLine("- T-P2-03 상점 제거 공백: 복구 금지.");
            report.AppendLine("- 기획 Random Day30 15~35%는 R-QA-03에서 구조적 한계. 게이트는 유지 밴드 50~85%.");
            report.AppendLine("- 실기기 AAB 설치·백그라운드 복귀는 Docs/AndroidBuild.md (서명 후).");
            report.AppendLine();

            if (failures.Count == 0)
            {
                report.AppendLine("RESULT: PASS");
            }
            else
            {
                LastFailed = true;
                report.AppendLine("RESULT: FAIL");
                for (var i = 0; i < failures.Count; i++)
                {
                    report.AppendLine($"  - {failures[i]}");
                }
            }

            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(logs);
            var file = Path.Combine(logs, $"release_gate_{DateTime.Now:yyyyMMdd}.txt");
            File.WriteAllText(file, report.ToString(), Encoding.UTF8);
            LastReportPath = file;
            if (failures.Count > 0)
            {
                throw new InvalidOperationException("[R-QA-10] 게이트 실패. " + file);
            }

            return file;
        }

        private static void RunSweep(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 1. ExhaustiveChoiceSweep ---");
            var path = ExhaustiveChoiceSweepRunner.RunAndSaveReport();
            report.AppendLine($"  report: {path}");
            report.AppendLine($"  Exceptions={ExhaustiveChoiceSweepRunner.LastExceptionCount}");
            Expect(failures, ExhaustiveChoiceSweepRunner.LastExceptionCount == 0, "선택지 스윕 예외 > 0");
            report.AppendLine();
        }

        private static void RunBalance(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 2. Balance Pass (Random KPI 유지) ---");
            var path = BalancePassRunner.RunAndSaveReport();
            var summary = BalancePassRunner.LastRandomSummary;
            report.AppendLine($"  report: {path}");
            if (summary == null)
            {
                Expect(failures, false, "BalancePass Random 요약이 없습니다.");
                report.AppendLine();
                return;
            }

            var day7 = summary.ReachRate(7);
            var day15 = summary.ReachRate(15);
            var day30 = summary.SuccessRate;
            var day1 = summary.Day1FailureRate;
            report.AppendLine($"  Day7={day7:P1} Day15={day15:P1} Day30Success={day30:P1} Day1Fail={day1:P1}");
            report.AppendLine($"  maintain Day30 {MaintainDay30Low:P0}~{MaintainDay30High:P0}, Day1Fail≤{MaxDay1Fail:P0}");
            Expect(failures, day1 <= MaxDay1Fail, $"Day1Fail {day1:P1} > {MaxDay1Fail:P0}");
            Expect(
                failures,
                day30 >= MaintainDay30Low && day30 <= MaintainDay30High,
                $"Day30Success {day30:P1}가 R-QA-03 유지 밴드({MaintainDay30Low:P0}~{MaintainDay30High:P0})를 이탈. 이벤트만 재조정 필요.");
            report.AppendLine();
        }

        private static void RunCampaign(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 3. Release QA Campaign 10×5 ---");
            var path = ReleaseQaCampaignRunner.RunAndSaveReport();
            report.AppendLine($"  report: {path}");
            report.AppendLine(
                $"  TotalRuns={ReleaseQaCampaignRunner.LastTotalRuns}, SuccessRate={ReleaseQaCampaignRunner.LastSuccessRate:P1}");
            Expect(
                failures,
                ReleaseQaCampaignRunner.LastTotalRuns == ExpectedCampaignRuns,
                $"캠페인 런 수 {ReleaseQaCampaignRunner.LastTotalRuns} ≠ {ExpectedCampaignRuns}");
            report.AppendLine();
        }

        private static void RunPlayFlow(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 4. Play flow (동의·일일·이어하기·리셋·광고 실패) ---");
            var canvas = new GameObject("Rqa10FlowCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var consentGo = new GameObject("ConsentPanel", typeof(RectTransform));
                consentGo.transform.SetParent(canvas.transform, false);
                consentGo.SetActive(false);
                var consent = consentGo.AddComponent<ConsentPanelView>();
                consent.Bind(consentGo, null, null, null, null);
                consent.Show(() => { });
                Expect(failures, consentGo.activeSelf, "동의 패널 Show 직후 꺼집니다.");

                var dailyGo = new GameObject("DailyPanel", typeof(RectTransform));
                dailyGo.transform.SetParent(canvas.transform, false);
                var dailyView = dailyGo.AddComponent<DailyPanelView>();
                var daily = new DailyContentState();
                daily.Load("2026-08-25", 0, false, 999, 0, 0, false, null);
                dailyView.Show(daily, () => { });
                var dailyTexts = JoinTexts(dailyGo.GetComponentsInChildren<Text>(true));
                Expect(
                    failures,
                    dailyTexts.IndexOf(EmptyStateCopy.NoDailyMissions, StringComparison.Ordinal) >= 0,
                    "일일 패널 빈 미션 문구가 없습니다.");
                Expect(
                    failures,
                    dailyTexts.IndexOf(EmptyStateCopy.NoDailyBest, StringComparison.Ordinal) >= 0,
                    "일일 패널 빈 베스트 문구가 없습니다.");

                var settingsGo = new GameObject("SettingsPanel", typeof(RectTransform));
                settingsGo.transform.SetParent(canvas.transform, false);
                var settings = settingsGo.AddComponent<SettingsPanelView>();
                settings.Show();
                Expect(failures, FindChild(settingsGo.transform, "ResetSaveButton"), "설정 리셋 버튼이 없습니다.");
                Expect(failures, FindChild(settingsGo.transform, "CreditsButton"), "설정 크레딧 버튼이 없습니다.");

                var mock = new MockAdService();
                mock.SetForceRewardedFailure(true, "rqa10");
                AdShowResult? adResult = null;
                mock.ShowRewardedAd(RewardedAdPlacement.ChoiceReroll, r => adResult = r);
                Expect(failures, adResult.HasValue && !adResult.Value.IsSuccess, "광고 실패 결과가 없습니다.");

                var store = new MemorySettingsStore();
                var appSettings = new AppSettingsService(store);
                appSettings.CompleteConsent(true, true);
                appSettings.SoundEnabled = false;
                appSettings.ResetToDefaultsKeepingConsent(keepConsent: true);
                Expect(failures, appSettings.ConsentFlowCompleted, "설정 리셋이 동의를 지웁니다.");
                Expect(failures, appSettings.SoundEnabled, "설정 리셋 후 사운드 기본값이 복구되지 않습니다.");

                var memory = new InMemorySaveService();
                var repo = new SaveRepository(memory);
                var save = SaveRepository.CreateDefault();
                save.run.hasActiveRun = true;
                save.run.currentDay = 9;
                repo.Save(save);
                var loaded = repo.LoadOrCreate();
                Expect(failures, loaded.run.hasActiveRun && loaded.run.currentDay == 9, "이어하기 세이브 라운드트립 실패.");
                repo.ClearRunAndSave(loaded);
                var cleared = repo.LoadOrCreate();
                Expect(failures, !cleared.run.hasActiveRun, "세이브 리셋 후에도 이어하기가 남아 있습니다.");

                report.AppendLine("  consent/daily/settings/ads/save OK");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }

            report.AppendLine();
        }

        private static void RunNestedChecklists(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 5. Nested checklists (05/06/08/09) ---");
            try
            {
                var meta = MetaUnlockScenarioRunner.RunAndSaveReport();
                report.AppendLine($"  meta: {meta}");
            }
            catch (Exception ex)
            {
                Expect(failures, false, "메타 해금 시나리오 실패: " + ex.Message);
            }

            try
            {
                var ux = Rqa06UxChecklistRunner.RunAndSaveReport();
                report.AppendLine($"  ux06: {ux}");
            }
            catch (Exception ex)
            {
                Expect(failures, false, "R-QA-06 UX 실패: " + ex.Message);
            }

            try
            {
                var copy = Rqa09CopyChecklistRunner.RunAndSaveReport();
                report.AppendLine($"  copy09: {copy}");
            }
            catch (Exception ex)
            {
                Expect(failures, false, "R-QA-09 카피 실패: " + ex.Message);
            }

            var sdk = Rqa08ReleaseGateRunner.RunAndSaveReport();
            report.AppendLine($"  sdk08: {sdk}");
            if (File.Exists(sdk))
            {
                var sdkText = File.ReadAllText(sdk);
                Expect(failures, sdkText.IndexOf("RESULT: PASS", StringComparison.Ordinal) >= 0, "R-QA-08 게이트 FAIL");
            }

            report.AppendLine();
        }

        private static void CheckAabModule(StringBuilder report, List<string> failures)
        {
            report.AppendLine("--- 6. AAB 모듈 (실빌드는 서명 후 AndroidBuild.md) ---");
            var supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            report.AppendLine($"  Android module: {supported}");
            report.AppendLine($"  buildAppBundle: {EditorUserBuildSettings.buildAppBundle}");
            report.AppendLine($"  versionCode: {PlayerSettings.Android.bundleVersionCode} (not bumped)");
            Expect(failures, supported, "Android 빌드 모듈이 없습니다.");
            report.AppendLine();
        }

        private static string JoinTexts(Text[] texts)
        {
            if (texts == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && !string.IsNullOrEmpty(texts[i].text))
                {
                    builder.AppendLine(texts[i].text);
                }
            }

            return builder.ToString();
        }

        private static bool FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == name)
            {
                return true;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                if (FindChild(root.GetChild(i), name))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Expect(List<string> failures, bool ok, string message)
        {
            if (!ok)
            {
                failures.Add(message);
            }
        }

        private sealed class MemorySettingsStore : IAppSettingsStore
        {
            private AppSettingsData data = new AppSettingsData();

            public AppSettingsData Load()
            {
                return JsonUtility.FromJson<AppSettingsData>(JsonUtility.ToJson(data));
            }

            public void Save(AppSettingsData value)
            {
                data = JsonUtility.FromJson<AppSettingsData>(JsonUtility.ToJson(value));
            }
        }
    }
}
