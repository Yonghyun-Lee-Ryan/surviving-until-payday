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
        public string PolicyName { get; set; } = string.Empty;
        public int Iterations { get; set; }
        public int BaseSeed { get; set; }
        public int SuccessCount { get; set; }
        public double AverageDaysSurvived { get; set; }
        public double AverageCash { get; set; }
        public int Day1FailureCount { get; set; }
        public int[] ReachCounts { get; } = new int[GameState.MaxDay + 1];
        public long[] CashSumByEndDay { get; } = new long[GameState.MaxDay + 1];
        public int[] EndCountByDay { get; } = new int[GameState.MaxDay + 1];
        public int[] FailCountByEndDay { get; } = new int[GameState.MaxDay + 1];
        public double SuccessRate => Iterations <= 0 ? 0 : SuccessCount / (double)Iterations;
        public Dictionary<FailureReason, int> FailureCounts { get; } = new Dictionary<FailureReason, int>();
        public Dictionary<string, int> EndingCounts { get; } = new Dictionary<string, int>();

        public int ReachDay7Count => ReachCounts[7];
        public int ReachDay15Count => ReachCounts[15];
        public int ReachDay21Count => ReachCounts[21];
        public int ReachDay30SuccessCount => SuccessCount;

        public double ReachRate(int day)
        {
            if (day < GameState.MinDay || day > GameState.MaxDay || Iterations <= 0)
            {
                return 0;
            }

            return ReachCounts[day] / (double)Iterations;
        }

        public double Day1FailureRate => Iterations <= 0 ? 0 : Day1FailureCount / (double)Iterations;

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

        public void RecordRun(ResultData result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.IsSuccess)
            {
                SuccessCount++;
            }
            else
            {
                if (!FailureCounts.ContainsKey(result.FailureReason))
                {
                    FailureCounts[result.FailureReason] = 0;
                }

                FailureCounts[result.FailureReason]++;
            }

            if (!result.IsSuccess && result.DaysSurvived <= 1)
            {
                Day1FailureCount++;
            }

            var maxReach = result.IsSuccess
                ? GameState.MaxDay
                : Math.Min(result.DaysSurvived, GameState.MaxDay);
            for (var day = GameState.MinDay; day <= maxReach; day++)
            {
                ReachCounts[day]++;
            }

            var endingId = result.Ending != null ? result.Ending.Id : "none";
            if (!EndingCounts.ContainsKey(endingId))
            {
                EndingCounts[endingId] = 0;
            }

            EndingCounts[endingId]++;

            var endDay = Math.Max(GameState.MinDay, Math.Min(result.DaysSurvived, GameState.MaxDay));
            EndCountByDay[endDay]++;
            if (result.FinalStats != null)
            {
                CashSumByEndDay[endDay] += result.FinalStats.Cash;
            }

            if (!result.IsSuccess)
            {
                FailCountByEndDay[endDay]++;
            }
        }

        public override string ToString()
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(PolicyName))
            {
                lines.Add($"Policy={PolicyName}");
            }

            lines.Add($"Seed={BaseSeed}");
            lines.Add($"Iterations={Iterations}");
            lines.Add($"SuccessRate={SuccessRate:P1} ({SuccessCount}/{Iterations})");
            lines.Add($"AvgDays={AverageDaysSurvived:F2}");
            lines.Add($"AvgCash={AverageCash:F0}");
            lines.Add($"Day1FailRate={Day1FailureRate:P1} ({Day1FailureCount}/{Iterations})");
            lines.Add(
                $"ReachDay7={ReachRate(7):P1} ({ReachDay7Count}/{Iterations}), " +
                $"ReachDay15={ReachRate(15):P1} ({ReachDay15Count}/{Iterations}), " +
                $"ReachDay21={ReachRate(21):P1} ({ReachDay21Count}/{Iterations}), " +
                $"ReachDay30Success={SuccessRate:P1} ({ReachDay30SuccessCount}/{Iterations})");
            lines.Add(BuildSurvivalCurveLine());
            lines.Add(BuildBucketLine(1, 7));
            lines.Add(BuildBucketLine(8, 14));
            lines.Add(BuildBucketLine(15, 21));
            lines.Add(BuildBucketLine(22, 30));

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

        private string BuildSurvivalCurveLine()
        {
            var parts = new List<string> { "SurvivalCurve:" };
            var highlightDays = new[] { 1, 3, 5, 7, 10, 15, 21, 28, 30 };
            for (var i = 0; i < highlightDays.Length; i++)
            {
                var day = highlightDays[i];
                parts.Add($"D{day}={ReachRate(day):P0}");
            }

            return string.Join(" ", parts);
        }

        private string BuildBucketLine(int fromDay, int toDay)
        {
            var ends = 0;
            var fails = 0;
            long cashSum = 0;
            for (var day = fromDay; day <= toDay; day++)
            {
                ends += EndCountByDay[day];
                fails += FailCountByEndDay[day];
                cashSum += CashSumByEndDay[day];
            }

            var failRate = ends <= 0 ? 0 : fails / (double)ends;
            var avgCash = ends <= 0 ? 0 : cashSum / (double)ends;
            return
                $"Bucket:D{fromDay}-{toDay} ends={ends} failRate={failRate:P1} avgEndCash={avgCash:F0}";
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
            var policySuffix = string.IsNullOrEmpty(PolicyName) ? string.Empty : $"_{PolicyName}";
            var fileName = $"{fileNamePrefix}{policySuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
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

            var summary = new SimulationSummary
            {
                Iterations = iterations,
                BaseSeed = baseSeed,
                PolicyName = policy.ToString()
            };
            long totalDays = 0;
            long totalCash = 0;

            for (var i = 0; i < iterations; i++)
            {
                var seed = unchecked(baseSeed + i * 997);
                var result = RunOnce(seed, policy);
                totalDays += result.DaysSurvived;
                totalCash += result.FinalStats.Cash;
                summary.RecordRun(result);
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
            var resolver = new EffectResolver(run.State, random, history, run.Days, trait);

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
