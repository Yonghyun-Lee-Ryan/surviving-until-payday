using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.DebugTools
{
    /// <summary>
    /// 시뮬레이터 선택 정책. 안전=첫 선택, 절약=중간, 위험=마지막.
    /// </summary>
    public enum SimulatorChoicePolicy
    {
        Random = 0,
        Safe = 1,
        Thrifty = 2,
        Risky = 3
    }

    public sealed class SimulationSummary
    {
        public int Iterations { get; set; }
        public int SuccessCount { get; set; }
        public double AverageDaysSurvived { get; set; }
        public double AverageCash { get; set; }
        public double SuccessRate => Iterations <= 0 ? 0 : SuccessCount / (double)Iterations;
        public Dictionary<FailureReason, int> FailureCounts { get; } = new Dictionary<FailureReason, int>();
        public Dictionary<string, int> EndingCounts { get; } = new Dictionary<string, int>();

        public int FailureCount
        {
            get
            {
                var total = 0;
                foreach (var pair in FailureCounts)
                {
                    total += pair.Value;
                }

                return total;
            }
        }

        public override string ToString()
        {
            var lines = new List<string>
            {
                $"Iterations={Iterations}",
                $"SuccessRate={SuccessRate:P1} ({SuccessCount}/{Iterations})",
                $"AvgDays={AverageDaysSurvived:F2}",
                $"AvgCash={AverageCash:F0}"
            };

            var failures = FailureCount;
            foreach (var pair in FailureCounts)
            {
                var ofAll = Iterations <= 0 ? 0 : pair.Value / (double)Iterations;
                var ofFails = failures <= 0 ? 0 : pair.Value / (double)failures;
                lines.Add(
                    $"Fail:{pair.Key}={pair.Value} (전체 {ofAll:P1}, 실패 중 {ofFails:P1})");
            }

            foreach (var pair in EndingCounts)
            {
                var ofAll = Iterations <= 0 ? 0 : pair.Value / (double)Iterations;
                lines.Add($"Ending:{pair.Key}={pair.Value} ({ofAll:P1})");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// 리포트를 텍스트 파일로 저장하고 경로를 반환한다.
        /// </summary>
        public string WriteToFile(string directoryPath, string fileNamePrefix = "run_sim")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("directoryPath is required.", nameof(directoryPath));
            }

            Directory.CreateDirectory(directoryPath);
            var fileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(directoryPath, fileName);
            File.WriteAllText(path, ToString(), Encoding.UTF8);
            return path;
        }
    }

    /// <summary>
    /// Scene 없이 30일 회차를 자동 실행한다. Editor/테스트용.
    /// </summary>
    public sealed class RunSimulator
    {
        private readonly JobData job;
        private readonly TraitData trait;
        private readonly IReadOnlyList<EventData> catalog;
        private readonly EventData fallbackEvent;
        private readonly IReadOnlyList<EndingData> endings;
        private readonly EndingData fallbackEnding;

        public RunSimulator(
            JobData job,
            TraitData trait,
            IReadOnlyList<EventData> catalog,
            EventData fallbackEvent,
            IReadOnlyList<EndingData> endings,
            EndingData fallbackEnding)
        {
            this.job = job ?? throw new ArgumentNullException(nameof(job));
            this.trait = trait;
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.fallbackEvent = fallbackEvent ?? throw new ArgumentNullException(nameof(fallbackEvent));
            this.endings = endings ?? Array.Empty<EndingData>();
            this.fallbackEnding = fallbackEnding;
        }

        public SimulationSummary Run(
            int iterations,
            int baseSeed,
            SimulatorChoicePolicy policy)
        {
            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations));
            }

            var summary = new SimulationSummary { Iterations = iterations };
            long totalDays = 0;
            long totalCash = 0;

            for (var i = 0; i < iterations; i++)
            {
                var seed = unchecked(baseSeed + i * 997);
                var result = RunOnce(seed, policy);
                totalDays += result.DaysSurvived;
                totalCash += result.FinalStats.Cash;

                if (result.IsSuccess)
                {
                    summary.SuccessCount++;
                }
                else
                {
                    if (!summary.FailureCounts.ContainsKey(result.FailureReason))
                    {
                        summary.FailureCounts[result.FailureReason] = 0;
                    }

                    summary.FailureCounts[result.FailureReason]++;
                }

                var endingId = result.Ending != null ? result.Ending.Id : "none";
                if (!summary.EndingCounts.ContainsKey(endingId))
                {
                    summary.EndingCounts[endingId] = 0;
                }

                summary.EndingCounts[endingId]++;
            }

            summary.AverageDaysSurvived = totalDays / (double)iterations;
            summary.AverageCash = totalCash / (double)iterations;
            return summary;
        }

        public ResultData RunOnce(int seed, SimulatorChoicePolicy policy)
        {
            var random = new SeededRandomService(seed);
            var run = new RunManager();
            run.StartRun(job, trait, seed);
            var selector = new EventSelector(catalog, fallbackEvent, random);
            var history = new RunHistory();
            var resolver = new EffectResolver(run.State, random, history, run.Days);

            while (run.Status == RunStatus.InProgress)
            {
                var dayEvent = selector.Select(run.State, run.Days);
                resolver.BeginEvent(dayEvent);

                var choiceIndex = PickChoiceIndex(dayEvent, random, policy);
                if (!resolver.TryResolveChoice(choiceIndex, out _, out var error))
                {
                    if (!resolver.TryResolveChoice(0, out _, out error))
                    {
                        throw new InvalidOperationException($"Simulator choice failed: {error}");
                    }
                }

                var advance = run.TryCompleteCurrentDayAfterChoice(resolver);
                if (!advance.Accepted)
                {
                    throw new InvalidOperationException($"Simulator advance failed: {advance.Message}");
                }

                if (advance.RunFailed || advance.RunSucceeded)
                {
                    break;
                }
            }

            var failure = run.Status == RunStatus.Failed
                ? run.State.EvaluateFailure()
                : FailureReason.None;
            var success = run.Status == RunStatus.Succeeded;
            var evaluator = new EndingEvaluator(endings, fallbackEnding);
            var ending = evaluator.Evaluate(run.State, success, failure);
            return ResultData.Create(run.State, success, failure, ending);
        }

        private static int PickChoiceIndex(
            EventData eventData,
            IRandomService random,
            SimulatorChoicePolicy policy)
        {
            var count = eventData.Choices != null ? eventData.Choices.Count : 0;
            if (count <= 0)
            {
                return 0;
            }

            switch (policy)
            {
                case SimulatorChoicePolicy.Safe:
                    return 0;
                case SimulatorChoicePolicy.Thrifty:
                    return count / 2;
                case SimulatorChoicePolicy.Risky:
                    return count - 1;
                default:
                    return random.Next(count);
            }
        }
    }
}
