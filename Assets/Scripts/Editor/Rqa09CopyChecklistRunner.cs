using System;
using System.IO;
using System.Text;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-09: 빈 상태 카피·크레딧·접근성 + R-QA-06 PlayMode 레이어 체크리스트.
    /// batch: -executeMethod SurviveUntilPayday.EditorTools.Rqa09CopyChecklistRunner.RunFromBatch
    /// </summary>
    public static class Rqa09CopyChecklistRunner
    {
        private const string AchievementFolder = "Assets/Resources/Achievements";

        [MenuItem("Tools/Surviving Until Payday/Run Copy Checklist (R-QA-09)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[R-QA-09] 카피·접근성 체크리스트 완료. {path}");
        }

        public static void RunFromBatch()
        {
            try
            {
                var path = RunAndSaveReport();
                Debug.Log($"[R-QA-09] batch OK. {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-09] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static string RunAndSaveReport()
        {
            var report = new StringBuilder();
            report.AppendLine("R-QA-09 Copy / Accessibility Checklist");
            report.AppendLine($"Unity {Application.unityVersion}");
            report.AppendLine($"When {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("Shop/IAP: not restored");
            report.AppendLine();

            var failures = new System.Collections.Generic.List<string>();

            CheckCopy(report, failures);
            CheckCreditsAndAssets(report, failures);
            CheckSettingsRuntime(report, failures);

            if (failures.Count > 0)
            {
                WriteReport(report, failures);
                throw new InvalidOperationException("[R-QA-09] 체크리스트 실패. 카피·크레딧 항목을 확인하세요.");
            }

            var uxPath = Rqa06UxChecklistRunner.RunAndSaveReport();
            report.AppendLine("--- R-QA-06 UX 체크리스트 ---");
            report.AppendLine($"  reused: {uxPath}");
            report.AppendLine();
            report.AppendLine("RESULT: PASS");

            return WriteReport(report, failures);
        }

        private static string WriteReport(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            if (failures.Count > 0)
            {
                report.AppendLine("RESULT: FAIL");
                for (var i = 0; i < failures.Count; i++)
                {
                    report.AppendLine($"  - {failures[i]}");
                }
            }

            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(logs);
            var file = Path.Combine(logs, $"rqa09_copy_checklist_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(file, report.ToString(), Encoding.UTF8);
            return file;
        }

        private static void CheckCopy(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 빈 상태·접근성 카피 ---");
            ExpectKorean(failures, EmptyStateCopy.ContinueUnavailable, "이어하기 빈 상태");
            ExpectKorean(failures, EmptyStateCopy.NoDailyMissions, "일일 미션 빈 상태");
            ExpectKorean(failures, EmptyStateCopy.NoStatChanges, "능력치 변화 빈 상태");
            ExpectKorean(failures, EmptyStateCopy.NoResultData, "결과 빈 상태");
            ExpectKorean(failures, EmptyStateCopy.CodexEmptyList, "도감 빈 상태");
            Expect(
                failures,
                EmptyStateCopy.NoResultBody.IndexOf("Game Scene", StringComparison.OrdinalIgnoreCase) < 0,
                "결과 폴백에 영문 씬 이름이 남아 있습니다.");

            Expect(
                failures,
                AccessibilityCopy.OfflineNote.IndexOf("오프라인", StringComparison.Ordinal) >= 0,
                "오프라인 안내가 없습니다.");
            Expect(
                failures,
                AccessibilityCopy.MinBodyFontSize >= 20,
                "본문 최소 글자 크기가 20 미만입니다.");
            Expect(
                failures,
                AccessibilityCopy.MinTapHeight >= 48f,
                "최소 터치 높이가 48 미만입니다.");

            var offlineAd = AdBlockReasonCopy.FromGatewayReason("offline", RewardedAdPlacement.ChoiceReroll);
            Expect(
                failures,
                offlineAd.IndexOf("오프라인", StringComparison.Ordinal) >= 0,
                "광고 오프라인 사유가 한글이 아닙니다.");
            Expect(
                failures,
                AdBlockReasonCopy.ServiceUnavailable.IndexOf("광고", StringComparison.Ordinal) >= 0,
                "광고 서비스 불가 문구가 비어 있습니다.");

            report.AppendLine($"  Continue empty: {EmptyStateCopy.ContinueUnavailable}");
            report.AppendLine($"  Daily empty: {EmptyStateCopy.NoDailyMissions}");
            report.AppendLine($"  Offline note: {AccessibilityCopy.OfflineNote}");
            report.AppendLine($"  Ad offline: {offlineAd}");
            report.AppendLine();
        }

        private static void CheckCreditsAndAssets(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 크레딧·라이선스·업적 ---");
            Expect(
                failures,
                CreditsCopy.Body.IndexOf("Noto", StringComparison.Ordinal) >= 0,
                "크레딧에 폰트 출처가 없습니다.");
            Expect(
                failures,
                CreditsCopy.Body.IndexOf("Kenney", StringComparison.Ordinal) >= 0,
                "크레딧에 효과음 출처가 없습니다.");
            Expect(
                failures,
                CreditsCopy.Body.IndexOf("example.com", StringComparison.OrdinalIgnoreCase) < 0,
                "크레딧에 placeholder URL이 있습니다.");

            var root = Directory.GetParent(Application.dataPath).FullName;
            var creditsPath = Path.Combine(root, "Docs", "AssetCredits.md");
            Expect(failures, File.Exists(creditsPath), "Docs/AssetCredits.md가 없습니다.");
            if (File.Exists(creditsPath))
            {
                var creditsMd = File.ReadAllText(creditsPath);
                Expect(
                    failures,
                    creditsMd.IndexOf("Adaptive Icon", StringComparison.OrdinalIgnoreCase) >= 0
                    || creditsMd.IndexOf("Art/Icons", StringComparison.OrdinalIgnoreCase) >= 0,
                    "AssetCredits.md에 Adaptive Icon 경로가 없습니다.");
                report.AppendLine("  AssetCredits.md Adaptive Icon OK");
            }

            var achievementGuids = AssetDatabase.IsValidFolder(AchievementFolder)
                ? AssetDatabase.FindAssets("t:AchievementData", new[] { AchievementFolder })
                : Array.Empty<string>();
            Expect(
                failures,
                achievementGuids.Length >= 10,
                $"업적 SO가 부족합니다 ({achievementGuids.Length}). Resources/Achievements를 확인하세요.");
            report.AppendLine($"  AchievementData count: {achievementGuids.Length}");
            report.AppendLine($"  Credits title: {CreditsCopy.Title}");
            report.AppendLine();
        }

        private static void CheckSettingsRuntime(StringBuilder report, System.Collections.Generic.List<string> failures)
        {
            report.AppendLine("--- 설정 런타임 (크레딧·오프라인) ---");
            var canvas = new GameObject("Rqa09ChecklistCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var go = new GameObject("SettingsPanel", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                var view = go.AddComponent<SettingsPanelView>();
                view.Show();

                var texts = go.GetComponentsInChildren<Text>(true);
                var blob = JoinTexts(texts);
                Expect(
                    failures,
                    blob.IndexOf(AccessibilityCopy.CreditsButton, StringComparison.Ordinal) >= 0,
                    "설정에 크레딧 버튼이 없습니다.");
                Expect(
                    failures,
                    blob.IndexOf("오프라인", StringComparison.Ordinal) >= 0,
                    "설정에 오프라인 안내가 없습니다.");
                Expect(
                    failures,
                    blob.IndexOf(AccessibilityCopy.BgmLabel, StringComparison.Ordinal) >= 0,
                    "설정 배경음 라벨이 한글이 아닙니다.");

                report.AppendLine($"  Settings texts contain credits={blob.Contains(AccessibilityCopy.CreditsButton)}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }

            report.AppendLine();
        }

        private static string JoinTexts(Text[] texts)
        {
            if (texts == null || texts.Length == 0)
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

        private static void ExpectKorean(System.Collections.Generic.List<string> failures, string value, string label)
        {
            Expect(failures, !string.IsNullOrWhiteSpace(value), label + "가 비어 있습니다.");
            Expect(
                failures,
                !string.IsNullOrEmpty(value) && ContainsHangul(value),
                label + "에 한글이 없습니다: " + value);
        }

        private static bool ContainsHangul(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] >= 0xAC00 && value[i] <= 0xD7A3)
                {
                    return true;
                }
            }

            return false;
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
