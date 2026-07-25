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
    /// Unit 23: 직업 3개 × N회 RunSimulator 성공률 비교 로그.
    /// </summary>
    public static class JobCompareSimulator
    {
        public const int DefaultIterations = 200;
        public const int DefaultBaseSeed = 1;
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string EventsFolder = "Assets/Data/Events";
        private const string EndingsFolder = "Assets/Data/Endings";
        private const string FallbackEventPath = "Assets/Data/Events/Event_Rest_Fallback.asset";
        private const string FallbackEndingPath = "Assets/Data/Endings/Ending_BarelySurvived.asset";

        private static readonly string[] PreferredJobOrder =
        {
            "job_junior_office",
            "job_civil_prep",
            "job_freelancer"
        };

        [MenuItem("Tools/Surviving Until Payday/Run Job Compare Sim (Unit 23)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[JobCompare] 완료. 리포트: {path}");
        }

        public static string RunAndSaveReport(
            int iterations = DefaultIterations,
            int baseSeed = DefaultBaseSeed,
            SimulatorChoicePolicy policy = SimulatorChoicePolicy.Random)
        {
            var fallbackEvent = AssetDatabase.LoadAssetAtPath<EventData>(FallbackEventPath);
            var fallbackEnding = AssetDatabase.LoadAssetAtPath<EndingData>(FallbackEndingPath);
            if (fallbackEvent == null)
            {
                throw new InvalidOperationException(
                    "[JobCompare] Fallback Event를 찾을 수 없습니다.");
            }

            var jobs = LoadJobsInPreferredOrder();
            if (jobs.Count == 0)
            {
                throw new InvalidOperationException(
                    "[JobCompare] Job 에셋을 찾을 수 없습니다.");
            }

            var events = LoadAll<EventData>(EventsFolder);
            if (events.Count == 0)
            {
                events.Add(fallbackEvent);
            }

            var endings = LoadAll<EndingData>(EndingsFolder);
            var report = new StringBuilder();
            report.AppendLine("=== Job Compare Sim Report (Unit 23) ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Policy: {policy}, Iterations: {iterations}, BaseSeed: {baseSeed}");
            report.AppendLine($"Events: {events.Count}, Jobs: {jobs.Count}");
            report.AppendLine();

            foreach (var job in jobs)
            {
                var simulator = new RunSimulator(
                    job,
                    trait: null,
                    events,
                    fallbackEvent,
                    endings,
                    fallbackEnding);
                var summary = simulator.Run(iterations, baseSeed, policy);

                report.AppendLine($"--- {job.Id} ({job.DisplayName}) unlock={job.UnlockLevel} ---");
                report.AppendLine(
                    $"SuccessRate={summary.SuccessRate:P1}, " +
                    $"AvgDays={summary.AverageDaysSurvived:F2}, " +
                    $"Day7={summary.ReachRate(7):P1}, " +
                    $"Day15={summary.ReachRate(15):P1}, " +
                    $"Day1Fail={summary.Day1FailureRate:P1}");
                report.AppendLine(summary.ToString());
                report.AppendLine();
            }

            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDir);
            var fileName = $"job_compare_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var reportPath = Path.Combine(logsDir, fileName);
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
            return reportPath;
        }

        private static List<JobData> LoadJobsInPreferredOrder()
        {
            var all = LoadAll<JobData>(JobsFolder);
            var ordered = new List<JobData>(PreferredJobOrder.Length);
            foreach (var id in PreferredJobOrder)
            {
                for (var i = 0; i < all.Count; i++)
                {
                    if (all[i] != null && all[i].Id == id)
                    {
                        ordered.Add(all[i]);
                        break;
                    }
                }
            }

            foreach (var job in all)
            {
                if (job == null || ordered.Contains(job))
                {
                    continue;
                }

                ordered.Add(job);
            }

            return ordered;
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
