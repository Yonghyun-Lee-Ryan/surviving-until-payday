using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Unit 27: 4정책 × N회 시뮬레이션 밸런스 패스 리포트.
    /// </summary>
    public static class BalancePassRunner
    {
        public const int DefaultIterations = 1000;
        public const int DefaultBaseSeed = 1;
        private const string JobPath = "Assets/Data/Jobs/Job_JuniorOffice.asset";
        private const string FallbackEventPath = "Assets/Data/Events/Event_Rest_Fallback.asset";
        private const string FallbackEndingPath = "Assets/Data/Endings/Ending_BarelySurvived.asset";
        private const string EventsFolder = "Assets/Data/Events";
        private const string EndingsFolder = "Assets/Data/Endings";
        private const string BalanceNotesPath = "Docs/BalanceNotes.md";

        private static readonly SimulatorChoicePolicy[] Policies =
        {
            SimulatorChoicePolicy.Random,
            SimulatorChoicePolicy.Safe,
            SimulatorChoicePolicy.Thrifty,
            SimulatorChoicePolicy.Risky
        };

        [MenuItem("Tools/Surviving Until Payday/Run Balance Pass Report (Unit 27)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[BalancePass] 완료. 리포트: {path}");
        }

        public static string RunAndSaveReport(
            int iterations = DefaultIterations,
            int baseSeed = DefaultBaseSeed)
        {
            var job = AssetDatabase.LoadAssetAtPath<JobData>(JobPath);
            var fallbackEvent = AssetDatabase.LoadAssetAtPath<EventData>(FallbackEventPath);
            var fallbackEnding = AssetDatabase.LoadAssetAtPath<EndingData>(FallbackEndingPath);

            if (job == null || fallbackEvent == null)
            {
                throw new InvalidOperationException(
                    "[BalancePass] Job 또는 Fallback Event 에셋을 찾을 수 없습니다. Sample 데이터를 먼저 생성하세요.");
            }

            var events = LoadAll<EventData>(EventsFolder);
            if (events.Count == 0)
            {
                events.Add(fallbackEvent);
            }

            var endings = LoadAll<EndingData>(EndingsFolder);
            var simulator = new RunSimulator(
                job,
                trait: null,
                events,
                fallbackEvent,
                endings,
                fallbackEnding);

            var report = new StringBuilder();
            report.AppendLine("=== Balance Pass Report (Unit 27) ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Job: {job.name} (trait=null, first-run baseline)");
            report.AppendLine($"Iterations per policy: {iterations}, BaseSeed: {baseSeed}");
            report.AppendLine();

            SimulationSummary randomSummary = null;
            foreach (var policy in Policies)
            {
                var summary = simulator.Run(iterations, baseSeed, policy);
                if (policy == SimulatorChoicePolicy.Random)
                {
                    randomSummary = summary;
                }

                report.AppendLine($"--- {policy} ---");
                report.AppendLine(summary.ToString());
                report.AppendLine();
            }

            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDir);
            var fileName = $"balance_pass_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var reportPath = Path.Combine(logsDir, fileName);
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

            AppendBalanceNotes(randomSummary, reportPath);
            Debug.Log(report.ToString());
            return reportPath;
        }

        private static void AppendBalanceNotes(SimulationSummary randomSummary, string reportPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var notesPath = Path.Combine(projectRoot, BalanceNotesPath);
            if (!File.Exists(notesPath))
            {
                Debug.LogWarning($"[BalancePass] {BalanceNotesPath} 없음. 리포트 파일만 저장했습니다.");
                return;
            }

            var section = new StringBuilder();
            section.AppendLine();
            section.AppendLine($"### {DateTime.Now:yyyy-MM-dd HH:mm} 측정");
            section.AppendLine($"- 리포트: `{Path.GetFileName(reportPath)}`");
            if (randomSummary != null)
            {
                section.AppendLine(
                    $"- Random KPI: Day7={randomSummary.ReachRate(7):P1}, " +
                    $"Day15={randomSummary.ReachRate(15):P1}, " +
                    $"Day30Success={randomSummary.SuccessRate:P1}, " +
                    $"Day1Fail={randomSummary.Day1FailureRate:P1}");
            }

            var content = File.ReadAllText(notesPath, Encoding.UTF8);
            const string marker = "## 조정 후 시뮬";
            var markerIndex = content.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                content += Environment.NewLine + marker + Environment.NewLine;
            }

            File.AppendAllText(notesPath, section.ToString(), Encoding.UTF8);
        }

        private static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return list;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }
    }
}
