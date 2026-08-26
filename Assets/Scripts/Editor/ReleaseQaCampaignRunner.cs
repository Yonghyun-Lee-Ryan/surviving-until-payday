using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 릴리즈 QA: 10 페르소나 × 5사이클 RunSimulator 캠페인.
    /// Unity: D:\Unity\Editor\6000.5.4f1\Editor\Unity.exe -batchmode -executeMethod ...
    /// </summary>
    public static class ReleaseQaCampaignRunner
    {
        public const int CyclesPerTester = 5;
        public const int RunsPerCycle = 40;
        public const int SmokeCyclesPerTester = 1;
        public const int SmokeRunsPerCycle = 10;

        public static int LastTotalRuns { get; private set; }
        public static double LastSuccessRate { get; private set; }
        public static string LastReportPath { get; private set; }
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string TraitsFolder = "Assets/Data/Traits";
        private const string EventsFolder = "Assets/Data/Events";
        private const string EndingsFolder = "Assets/Data/Endings";
        private const string FallbackEventPath = "Assets/Data/Events/Event_Rest_Fallback.asset";
        private const string FallbackEndingPath = "Assets/Data/Endings/Ending_BarelySurvived.asset";

        private sealed class TesterPersona
        {
            public string Id;
            public string DisplayName;
            public string Focus;
            public string PreferredJobId;
            public string PreferredTraitId;
            public SimulatorChoicePolicy Policy;
            public int BaseSeed;
        }

        private static readonly TesterPersona[] Personas =
        {
            new TesterPersona
            {
                Id = "qa01", DisplayName = "신규 튜토리얼", Focus = "첫 회차·튜토리얼·thrifty",
                PreferredJobId = "job_junior_office", PreferredTraitId = "trait_thrifty",
                Policy = SimulatorChoicePolicy.Safe, BaseSeed = 101
            },
            new TesterPersona
            {
                Id = "qa02", DisplayName = "절약 생존자", Focus = "현금 보존·Thrifty",
                PreferredJobId = "job_junior_office", PreferredTraitId = "trait_thrifty",
                Policy = SimulatorChoicePolicy.Thrifty, BaseSeed = 202
            },
            new TesterPersona
            {
                Id = "qa03", DisplayName = "위험 도박사", Focus = "Risky·주식·사설수리",
                PreferredJobId = "job_junior_office", PreferredTraitId = null,
                Policy = SimulatorChoicePolicy.Risky, BaseSeed = 303
            },
            new TesterPersona
            {
                Id = "qa04", DisplayName = "랜덤 일반인", Focus = "Random KPI 기준선",
                PreferredJobId = "job_junior_office", PreferredTraitId = null,
                Policy = SimulatorChoicePolicy.Random, BaseSeed = 404
            },
            new TesterPersona
            {
                Id = "qa05", DisplayName = "공무원 준비생", Focus = "직업 해금 L2·civil 전용 사건",
                PreferredJobId = "job_civil_prep", PreferredTraitId = "trait_healthy",
                Policy = SimulatorChoicePolicy.Safe, BaseSeed = 505
            },
            new TesterPersona
            {
                Id = "qa06", DisplayName = "프리랜서", Focus = "직업 해금 L3·수입 변동",
                PreferredJobId = "job_freelancer", PreferredTraitId = "trait_positive",
                Policy = SimulatorChoicePolicy.Random, BaseSeed = 606
            },
            new TesterPersona
            {
                Id = "qa07", DisplayName = "야근 전문가", Focus = "trait_overtime_pro·WORK 스트레스",
                PreferredJobId = "job_junior_office", PreferredTraitId = "trait_overtime_pro",
                Policy = SimulatorChoicePolicy.Safe, BaseSeed = 707
            },
            new TesterPersona
            {
                Id = "qa08", DisplayName = "엔딩 수집가", Focus = "성공 엔딩 다양성·Risky",
                PreferredJobId = "job_junior_office", PreferredTraitId = "trait_positive",
                Policy = SimulatorChoicePolicy.Risky, BaseSeed = 808
            },
            new TesterPersona
            {
                Id = "qa09", DisplayName = "실패 경로", Focus = "파산/번아웃/해고 도달",
                PreferredJobId = "job_freelancer", PreferredTraitId = null,
                Policy = SimulatorChoicePolicy.Risky, BaseSeed = 909
            },
            new TesterPersona
            {
                Id = "qa10", DisplayName = "메타 그라인더", Focus = "다회차 XP·해금 곡선",
                PreferredJobId = "job_civil_prep", PreferredTraitId = "trait_healthy",
                Policy = SimulatorChoicePolicy.Thrifty, BaseSeed = 1010
            }
        };

        [MenuItem("Tools/Surviving Until Payday/Run Release QA Campaign (10×5)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[ReleaseQaCampaign] 완료. 리포트: {path}");
        }

        [MenuItem("Tools/Surviving Until Payday/Run Smoke Campaign (R-QA-09)")]
        public static void RunSmokeFromMenu()
        {
            var path = RunSmokeAndSaveReport();
            Debug.Log($"[R-QA-09] 스모크 완료. 리포트: {path}");
        }

        /// <summary>
        /// batchmode: -executeMethod SurviveUntilPayday.EditorTools.ReleaseQaCampaignRunner.RunFromBatch
        /// </summary>
        public static void RunFromBatch()
        {
            try
            {
                var path = RunAndSaveReport();
                Debug.Log($"[ReleaseQaCampaign] batch OK: {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReleaseQaCampaign] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// R-QA-09 스모크. 전체 2,000런이 아니라 10×1×10=100런.
        /// batch: -executeMethod SurviveUntilPayday.EditorTools.ReleaseQaCampaignRunner.RunSmokeFromBatch
        /// </summary>
        public static void RunSmokeFromBatch()
        {
            try
            {
                var path = RunSmokeAndSaveReport();
                Debug.Log($"[R-QA-09] smoke batch OK: {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-09] smoke batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static string RunSmokeAndSaveReport()
        {
            return RunAndSaveReport(
                SmokeCyclesPerTester,
                SmokeRunsPerCycle,
                "rqa09_smoke_campaign",
                "=== R-QA-09 Smoke Campaign (10 testers × 1 cycle × 10 runs) ===");
        }

        public static string RunAndSaveReport()
        {
            return RunAndSaveReport(
                CyclesPerTester,
                RunsPerCycle,
                "release_qa_campaign",
                "=== Release QA Campaign (10 testers × 5 cycles) ===");
        }

        public static string RunAndSaveReport(int cycles, int runsPerCycle, string filePrefix, string header)
        {
            if (cycles < 1)
            {
                cycles = 1;
            }

            if (runsPerCycle < 1)
            {
                runsPerCycle = 1;
            }

            var fallbackEvent = AssetDatabase.LoadAssetAtPath<EventData>(FallbackEventPath);
            var fallbackEnding = AssetDatabase.LoadAssetAtPath<EndingData>(FallbackEndingPath);
            if (fallbackEvent == null)
            {
                throw new InvalidOperationException("[ReleaseQaCampaign] Fallback Event missing.");
            }

            var jobs = LoadAll<JobData>(JobsFolder);
            var traits = LoadAll<TraitData>(TraitsFolder);
            var events = LoadAll<EventData>(EventsFolder);
            if (events.Count == 0)
            {
                events.Add(fallbackEvent);
            }

            var endings = LoadAll<EndingData>(EndingsFolder);
            var report = new StringBuilder();
            report.AppendLine(header);
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Unity: {Application.unityVersion}");
            report.AppendLine($"Content: jobs={jobs.Count}, traits={traits.Count}, events={events.Count}, endings={endings.Count}");
            report.AppendLine($"RunsPerCycle={runsPerCycle}, Cycles={cycles}");
            report.AppendLine("KPI targets (Random baseline): Day7≈70%, Day15≈50%, Day30Success≈15~35%");
            report.AppendLine();

            var aggregateSuccess = 0;
            var aggregateRuns = 0;
            var endingHits = new Dictionary<string, int>();
            var failureHits = new Dictionary<string, int>();

            for (var p = 0; p < Personas.Length; p++)
            {
                var persona = Personas[p];
                var job = FindJob(jobs, persona.PreferredJobId) ?? FindJob(jobs, "job_junior_office");
                if (job == null)
                {
                    throw new InvalidOperationException("[ReleaseQaCampaign] No job assets.");
                }

                var trait = FindTrait(traits, persona.PreferredTraitId);
                var simulator = new RunSimulator(
                    job,
                    trait,
                    events,
                    fallbackEvent,
                    endings,
                    fallbackEnding);

                report.AppendLine(
                    $"## {persona.Id} {persona.DisplayName} | job={job.Id} trait={(trait != null ? trait.Id : "null")} policy={persona.Policy}");
                report.AppendLine($"Focus: {persona.Focus}");

                for (var cycle = 0; cycle < cycles; cycle++)
                {
                    var seed = persona.BaseSeed + cycle * 1000;
                    var summary = simulator.Run(runsPerCycle, seed, persona.Policy);
                    aggregateSuccess += summary.SuccessCount;
                    aggregateRuns += summary.Iterations;

                    report.AppendLine(
                        $"  Cycle{cycle + 1}: Success={summary.SuccessRate:P1}, " +
                        $"AvgDays={summary.AverageDaysSurvived:F1}, " +
                        $"D7={summary.ReachRate(7):P0}, D15={summary.ReachRate(15):P0}, " +
                        $"D1Fail={summary.Day1FailureRate:P1}");

                    foreach (var pair in summary.EndingCounts)
                    {
                        if (!endingHits.ContainsKey(pair.Key))
                        {
                            endingHits[pair.Key] = 0;
                        }

                        endingHits[pair.Key] += pair.Value;
                    }

                    foreach (var pair in summary.FailureCounts)
                    {
                        var key = pair.Key.ToString();
                        if (!failureHits.ContainsKey(key))
                        {
                            failureHits[key] = 0;
                        }

                        failureHits[key] += pair.Value;
                    }
                }

                report.AppendLine();
            }

            report.AppendLine("## Aggregate");
            report.AppendLine(
                $"TotalRuns={aggregateRuns}, SuccessRate={(aggregateRuns <= 0 ? 0 : aggregateSuccess / (double)aggregateRuns):P1}");
            LastTotalRuns = aggregateRuns;
            LastSuccessRate = aggregateRuns <= 0 ? 0 : aggregateSuccess / (double)aggregateRuns;
            report.AppendLine("EndingHits:");
            foreach (var pair in endingHits)
            {
                report.AppendLine($"  - {pair.Key}: {pair.Value}");
            }

            report.AppendLine("FailureHits:");
            foreach (var pair in failureHits)
            {
                report.AppendLine($"  - {pair.Key}: {pair.Value}");
            }

            report.AppendLine();
            report.AppendLine("## Simulator limits (QA notes)");
            report.AppendLine("- Choice policy picks by index (Safe=0, Thrifty=mid, Risky=last), not by semantic label.");
            report.AppendLine("- Ads/UI/tutorial/meta unlock UX are NOT covered by RunSimulator.");
            report.AppendLine("- Full choice-coverage requires ExhaustiveChoiceSweep (see Work Order Unit R-QA-02).");
            report.AppendLine("- freelancer+Risky (qa09) extreme difficulty is ticket T-P2-01; smoke does not fail on that KPI.");

            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDir);
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var reportPath = Path.Combine(logsDir, fileName);
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            LastReportPath = reportPath;
            Debug.Log(report.ToString());
            return reportPath;
        }

        private static JobData FindJob(List<JobData> jobs, string id)
        {
            if (string.IsNullOrEmpty(id) || jobs == null)
            {
                return null;
            }

            for (var i = 0; i < jobs.Count; i++)
            {
                if (jobs[i] != null && jobs[i].Id == id)
                {
                    return jobs[i];
                }
            }

            return null;
        }

        private static TraitData FindTrait(List<TraitData> traits, string id)
        {
            if (string.IsNullOrEmpty(id) || traits == null)
            {
                return null;
            }

            for (var i = 0; i < traits.Count; i++)
            {
                if (traits[i] != null && traits[i].Id == id)
                {
                    return traits[i];
                }
            }

            return null;
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
