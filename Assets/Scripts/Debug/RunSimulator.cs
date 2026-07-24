using System;
using System.Collections.Generic;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.DebugTools
{
    public enum SimulatorChoicePolicy
    {
        Random = 0,
        FirstChoice = 1,
        LastChoice = 2
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

        public override string ToString()
        {
            var lines = new List<string>
            {
                $"Iterations={Iterations}",
                $"SuccessRate={SuccessRate:P1} ({SuccessCount}/{Iterations})",
                $"AvgDays={AverageDaysSurvived:F2}",
                $"AvgCash={AverageCash:F0}"
            };

            foreach (var pair in FailureCounts)
            {
                lines.Add($"Fail:{pair.Key}={pair.Value}");
            }

            foreach (var pair in EndingCounts)
            {
                lines.Add($"Ending:{pair.Key}={pair.Value}");
            }

            return string.Join("\n", lines);
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
                    // 선택 실패 시 0번 재시도
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
                case SimulatorChoicePolicy.FirstChoice:
                    return 0;
                case SimulatorChoicePolicy.LastChoice:
                    return count - 1;
                default:
                    return random.Next(count);
            }
        }
    }
}
