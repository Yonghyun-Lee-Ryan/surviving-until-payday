using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-02: 전 사건×선택지 강제 적용 스윕. 미도달·데드·예외·랜덤 분기 미커버를 Logs에 기록.
    /// </summary>
    public static class ExhaustiveChoiceSweepRunner
    {
        private const string EventsFolder = "Assets/Data/Events";
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string FallbackEventPath = "Assets/Data/Events/Event_Rest_Fallback.asset";
        public const int RandomOutcomeProbeSeeds = 64;

        public static int LastExceptionCount { get; private set; }
        public static string LastReportPath { get; private set; }

        private sealed class ChoiceAttempt
        {
            public string EventId;
            public string EventTitle;
            public int ChoiceIndex;
            public string ChoiceId;
            public string ChoiceText;
            public bool IsDead;
            public bool Attempted;
            public bool Resolved;
            public string SkipReason;
            public string Error;
            public string Exception;
            public string StatDeltaSummary;
            public int RandomOutcomeCount;
            public List<string> HitOutcomeIds = new List<string>();
            public List<string> MissedOutcomeIds = new List<string>();
            public List<string> SetFlags = new List<string>();
        }

        [MenuItem("Tools/Surviving Until Payday/Run Exhaustive Choice Sweep (R-QA-02)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[ChoiceSweep] 완료. 리포트: {path}");
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// batchmode: -executeMethod SurviveUntilPayday.EditorTools.ExhaustiveChoiceSweepRunner.RunFromBatch
        /// </summary>
        public static void RunFromBatch()
        {
            try
            {
                var path = RunAndSaveReport();
                Debug.Log($"[ChoiceSweep] batch OK: {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChoiceSweep] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static string RunAndSaveReport()
        {
            var events = LoadAll<EventData>(EventsFolder);
            var jobs = LoadAll<JobData>(JobsFolder);
            if (events.Count == 0)
            {
                var fallback = AssetDatabase.LoadAssetAtPath<EventData>(FallbackEventPath);
                if (fallback != null)
                {
                    events.Add(fallback);
                }
            }

            if (jobs.Count == 0)
            {
                throw new InvalidOperationException("[ChoiceSweep] Job assets missing.");
            }

            var attempts = new List<ChoiceAttempt>();
            var exceptions = 0;
            var flagSetters = CollectFlagSetters(events);

            for (var e = 0; e < events.Count; e++)
            {
                var eventData = events[e];
                if (eventData == null)
                {
                    continue;
                }

                SweepEvent(eventData, jobs, flagSetters, attempts, ref exceptions);
            }

            var report = BuildReport(events.Count, jobs.Count, attempts, exceptions, flagSetters);
            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDir);
            var fileName = $"choice_sweep_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var reportPath = Path.Combine(logsDir, fileName);
            File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            LastExceptionCount = exceptions;
            LastReportPath = reportPath;
            Debug.Log(report);
            return reportPath;
        }

        private static void SweepEvent(
            EventData eventData,
            List<JobData> jobs,
            Dictionary<string, List<string>> flagSetters,
            List<ChoiceAttempt> attempts,
            ref int exceptions)
        {
            var choices = eventData.Choices;
            if (choices == null || choices.Count == 0)
            {
                attempts.Add(new ChoiceAttempt
                {
                    EventId = eventData.Id,
                    EventTitle = eventData.Title,
                    ChoiceIndex = -1,
                    ChoiceText = "(no choices)",
                    SkipReason = "선택지 없음",
                    Attempted = false
                });
                return;
            }

            if (!TryPrepareContext(eventData, jobs, flagSetters, out var job, out var day, out var prepNotes, out var skipReason))
            {
                for (var i = 0; i < choices.Count; i++)
                {
                    var c = choices[i];
                    attempts.Add(new ChoiceAttempt
                    {
                        EventId = eventData.Id,
                        EventTitle = eventData.Title,
                        ChoiceIndex = i,
                        ChoiceId = c != null ? c.ChoiceId : null,
                        ChoiceText = c != null ? c.Text : null,
                        IsDead = IsDeadChoice(c),
                        Attempted = false,
                        SkipReason = skipReason
                    });
                }

                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var attempt = new ChoiceAttempt
                {
                    EventId = eventData.Id,
                    EventTitle = eventData.Title,
                    ChoiceIndex = i,
                    ChoiceId = choice != null ? choice.ChoiceId : null,
                    ChoiceText = choice != null ? choice.Text : null,
                    IsDead = IsDeadChoice(choice),
                    Attempted = true,
                    RandomOutcomeCount = choice?.RandomOutcomes != null ? choice.RandomOutcomes.Count : 0,
                    SkipReason = prepNotes
                };

                if (choice != null && choice.SetFlags != null)
                {
                    for (var f = 0; f < choice.SetFlags.Count; f++)
                    {
                        if (!string.IsNullOrWhiteSpace(choice.SetFlags[f]))
                        {
                            attempt.SetFlags.Add(choice.SetFlags[f]);
                        }
                    }
                }

                try
                {
                    ResolveChoiceWithProbes(eventData, job, day, i, choice, attempt);
                }
                catch (Exception ex)
                {
                    exceptions++;
                    attempt.Resolved = false;
                    attempt.Exception = ex.GetType().Name + ": " + ex.Message;
                }

                attempts.Add(attempt);
            }
        }

        private static void ResolveChoiceWithProbes(
            EventData eventData,
            JobData job,
            int day,
            int choiceIndex,
            EventChoiceData choice,
            ChoiceAttempt attempt)
        {
            var primary = TryResolveOnce(eventData, job, day, choiceIndex, seed: 1000 + choiceIndex);
            if (!primary.ok)
            {
                attempt.Resolved = false;
                attempt.Error = primary.error;
                return;
            }

            attempt.Resolved = true;
            attempt.StatDeltaSummary = primary.deltaSummary;
            if (!string.IsNullOrEmpty(primary.outcomeId))
            {
                attempt.HitOutcomeIds.Add(primary.outcomeId);
            }

            if (choice == null || choice.RandomOutcomes == null || choice.RandomOutcomes.Count == 0)
            {
                return;
            }

            var expected = new HashSet<string>();
            var unlabeled = 0;
            for (var o = 0; o < choice.RandomOutcomes.Count; o++)
            {
                var outcome = choice.RandomOutcomes[o];
                if (outcome == null || outcome.ProbabilityWeight <= 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(outcome.OutcomeId))
                {
                    unlabeled++;
                    continue;
                }

                expected.Add(outcome.OutcomeId);
            }

            if (unlabeled > 0)
            {
                attempt.MissedOutcomeIds.Add($"unlabeled_outcomes={unlabeled}");
            }

            var hit = new HashSet<string>(attempt.HitOutcomeIds);
            for (var seed = 0; seed < RandomOutcomeProbeSeeds && hit.Count < expected.Count; seed++)
            {
                var probe = TryResolveOnce(eventData, job, day, choiceIndex, seed: 2000 + seed * 17 + choiceIndex);
                if (!probe.ok || string.IsNullOrEmpty(probe.outcomeId))
                {
                    continue;
                }

                if (hit.Add(probe.outcomeId))
                {
                    attempt.HitOutcomeIds.Add(probe.outcomeId);
                }
            }

            foreach (var id in expected)
            {
                if (!hit.Contains(id))
                {
                    attempt.MissedOutcomeIds.Add(id);
                }
            }
        }

        private static (bool ok, string error, string deltaSummary, string outcomeId) TryResolveOnce(
            EventData eventData,
            JobData job,
            int day,
            int choiceIndex,
            int seed)
        {
            var state = GameState.CreateFromJob(job, null, seed);
            ApplyConditionSetup(state, eventData, day);

            var isWeekend = DayCalendar.IsWeekend(day);
            if (!EventConditionEvaluator.MatchesDayRange(eventData, day)
                || !EventConditionEvaluator.Matches(eventData.Conditions, state, isWeekend))
            {
                return (false, "조건 매칭 실패(준비 후)", null, null);
            }

            var history = new RunHistory();
            var days = new DayManager(state);
            var random = new SeededRandomService(seed);
            var resolver = new EffectResolver(state, random, history, days, null);
            resolver.BeginEvent(eventData);
            if (!resolver.TryResolveChoice(choiceIndex, out var result, out var error))
            {
                return (false, error ?? "TryResolveChoice failed", null, null);
            }

            var delta = EffectResolver.FormatStatChanges(result.StatChanges);
            return (true, null, delta, result.RandomOutcomeId);
        }

        private static bool TryPrepareContext(
            EventData eventData,
            List<JobData> jobs,
            Dictionary<string, List<string>> flagSetters,
            out JobData job,
            out int day,
            out string prepNotes,
            out string skipReason)
        {
            job = null;
            day = eventData.MinDay;
            prepNotes = null;
            skipReason = null;

            var requiredJobId = eventData.Conditions != null
                ? eventData.Conditions.RequiredJobId
                : null;
            job = FindJob(jobs, requiredJobId) ?? FindJob(jobs, "job_junior_office") ?? jobs[0];
            if (!string.IsNullOrWhiteSpace(requiredJobId) && FindJob(jobs, requiredJobId) == null)
            {
                skipReason = $"RequiredJobId '{requiredJobId}' 에셋 없음";
                return false;
            }

            if (eventData.IsFixedEvent)
            {
                day = eventData.FixedDay;
                if (day < eventData.MinDay || day > eventData.MaxDay)
                {
                    skipReason = $"fixedDay={day}가 min/max({eventData.MinDay}~{eventData.MaxDay}) 밖";
                    return false;
                }
            }
            else if (!TryPickDay(eventData, out day))
            {
                skipReason =
                    $"요일 제약({eventData.Conditions?.DayOfWeekConstraint})을 만족하는 day가 " +
                    $"{eventData.MinDay}~{eventData.MaxDay}에 없음";
                return false;
            }

            if (eventData.Conditions?.RequiredFlags != null)
            {
                for (var i = 0; i < eventData.Conditions.RequiredFlags.Count; i++)
                {
                    var flag = eventData.Conditions.RequiredFlags[i];
                    if (string.IsNullOrWhiteSpace(flag))
                    {
                        continue;
                    }

                    if (!flagSetters.ContainsKey(flag))
                    {
                        prepNotes = (prepNotes ?? string.Empty) +
                                    $" [flag '{flag}' setter 없음 — 강제 SetFlag로 시도]";
                    }
                }
            }

            return true;
        }

        private static void ApplyConditionSetup(GameState state, EventData eventData, int day)
        {
            state.CurrentDay = day;
            var c = eventData.Conditions;
            if (c == null)
            {
                return;
            }

            state.Stats.Health = Mid(c.MinHealth, c.MaxHealth);
            state.Stats.Stress = Mid(c.MinStress, c.MaxStress);
            state.Stats.Happiness = Mid(c.MinHappiness, c.MaxHappiness);
            state.Stats.CompanyScore = Mid(c.MinCompanyScore, c.MaxCompanyScore);

            if (c.UseMinCash || c.UseMaxCash)
            {
                long cash = state.Stats.Cash;
                if (c.UseMinCash && c.UseMaxCash)
                {
                    cash = (c.MinCash + c.MaxCash) / 2;
                }
                else if (c.UseMinCash)
                {
                    cash = Math.Max(state.Stats.Cash, c.MinCash);
                }
                else if (c.UseMaxCash)
                {
                    cash = Math.Min(state.Stats.Cash, c.MaxCash);
                }

                state.Stats.Cash = cash;
            }

            state.ClearRunFlags();
            if (c.RequiredFlags != null)
            {
                for (var i = 0; i < c.RequiredFlags.Count; i++)
                {
                    state.SetFlag(c.RequiredFlags[i]);
                }
            }
        }

        private static bool TryPickDay(EventData eventData, out int day)
        {
            var constraint = eventData.Conditions != null
                ? eventData.Conditions.DayOfWeekConstraint
                : DayOfWeekConstraint.Any;
            var min = Math.Max(GameState.MinDay, eventData.MinDay);
            var max = Math.Min(GameState.MaxDay, eventData.MaxDay);
            for (var d = min; d <= max; d++)
            {
                var weekend = DayCalendar.IsWeekend(d);
                switch (constraint)
                {
                    case DayOfWeekConstraint.WeekdayOnly when weekend:
                        continue;
                    case DayOfWeekConstraint.WeekendOnly when !weekend:
                        continue;
                }

                day = d;
                return true;
            }

            day = min;
            return false;
        }

        private static bool IsDeadChoice(EventChoiceData choice)
        {
            if (choice == null)
            {
                return true;
            }

            var hasFixed = false;
            if (choice.FixedEffects != null)
            {
                for (var i = 0; i < choice.FixedEffects.Count; i++)
                {
                    if (choice.FixedEffects[i] != null && choice.FixedEffects[i].Value != 0)
                    {
                        hasFixed = true;
                        break;
                    }
                }
            }

            var hasRandom = choice.RandomOutcomes != null && choice.RandomOutcomes.Count > 0;
            var hasSet = choice.SetFlags != null && choice.SetFlags.Count > 0;
            var hasClear = choice.ClearFlags != null && choice.ClearFlags.Count > 0;
            return !hasFixed && !hasRandom && !hasSet && !hasClear;
        }

        private static Dictionary<string, List<string>> CollectFlagSetters(List<EventData> events)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (var e = 0; e < events.Count; e++)
            {
                var ev = events[e];
                if (ev?.Choices == null)
                {
                    continue;
                }

                for (var i = 0; i < ev.Choices.Count; i++)
                {
                    var choice = ev.Choices[i];
                    if (choice?.SetFlags == null)
                    {
                        continue;
                    }

                    for (var f = 0; f < choice.SetFlags.Count; f++)
                    {
                        var flag = choice.SetFlags[f];
                        if (string.IsNullOrWhiteSpace(flag))
                        {
                            continue;
                        }

                        if (!map.TryGetValue(flag, out var list))
                        {
                            list = new List<string>();
                            map[flag] = list;
                        }

                        var label = $"{ev.Id}[{i}]";
                        if (!list.Contains(label))
                        {
                            list.Add(label);
                        }
                    }

                    if (choice.RandomOutcomes == null)
                    {
                        continue;
                    }

                    for (var o = 0; o < choice.RandomOutcomes.Count; o++)
                    {
                        var outcome = choice.RandomOutcomes[o];
                        if (outcome?.SetFlags == null)
                        {
                            continue;
                        }

                        for (var f = 0; f < outcome.SetFlags.Count; f++)
                        {
                            var flag = outcome.SetFlags[f];
                            if (string.IsNullOrWhiteSpace(flag))
                            {
                                continue;
                            }

                            if (!map.TryGetValue(flag, out var list))
                            {
                                list = new List<string>();
                                map[flag] = list;
                            }

                            var label = $"{ev.Id}[{i}].R{o}";
                            if (!list.Contains(label))
                            {
                                list.Add(label);
                            }
                        }
                    }
                }
            }

            return map;
        }

        private static string BuildReport(
            int eventCount,
            int jobCount,
            List<ChoiceAttempt> attempts,
            int exceptions,
            Dictionary<string, List<string>> flagSetters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Exhaustive Choice Sweep (R-QA-02) ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity: {Application.unityVersion}");
            sb.AppendLine($"Events={eventCount}, Jobs={jobCount}, ChoiceSlots={attempts.Count}");
            sb.AppendLine();

            var attempted = 0;
            var resolved = 0;
            var skipped = 0;
            var dead = 0;
            var failed = 0;
            var missedRandom = 0;

            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                if (a.IsDead)
                {
                    dead++;
                }

                if (!a.Attempted)
                {
                    skipped++;
                }
                else
                {
                    attempted++;
                    if (a.Resolved)
                    {
                        resolved++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                if (a.MissedOutcomeIds != null && a.MissedOutcomeIds.Count > 0)
                {
                    missedRandom++;
                }
            }

            sb.AppendLine("## Summary");
            sb.AppendLine($"Attempted={attempted}, Resolved={resolved}, FailedResolve={failed}, Skipped={skipped}");
            sb.AppendLine($"DeadChoices={dead}, Exceptions={exceptions}, ChoicesWithMissedRandomOutcomes={missedRandom}");
            sb.AppendLine();

            sb.AppendLine("## Dead choices (no effects / flags / random)");
            var deadCount = 0;
            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                if (!a.IsDead)
                {
                    continue;
                }

                deadCount++;
                sb.AppendLine(
                    $"  - {a.EventId}[{a.ChoiceIndex}] id={a.ChoiceId} text=\"{Trim(a.ChoiceText, 40)}\"");
            }

            if (deadCount == 0)
            {
                sb.AppendLine("  (none)");
            }

            sb.AppendLine();
            sb.AppendLine("## Skipped / unreachable (조건 불가 사유)");
            var skipCount = 0;
            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                if (a.Attempted)
                {
                    continue;
                }

                skipCount++;
                sb.AppendLine(
                    $"  - {a.EventId}[{a.ChoiceIndex}] reason={a.SkipReason}");
            }

            if (skipCount == 0)
            {
                sb.AppendLine("  (none)");
            }

            sb.AppendLine();
            sb.AppendLine("## Resolve failures / exceptions");
            var failLines = 0;
            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                if (a.Attempted && a.Resolved && string.IsNullOrEmpty(a.Exception))
                {
                    continue;
                }

                if (!a.Attempted)
                {
                    continue;
                }

                failLines++;
                sb.AppendLine(
                    $"  - {a.EventId}[{a.ChoiceIndex}] error={a.Error} ex={a.Exception}");
            }

            if (failLines == 0)
            {
                sb.AppendLine("  (none)");
            }

            sb.AppendLine();
            sb.AppendLine("## Random outcomes not hit (probe seeds)");
            var missLines = 0;
            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                if (a.MissedOutcomeIds == null || a.MissedOutcomeIds.Count == 0)
                {
                    continue;
                }

                missLines++;
                sb.AppendLine(
                    $"  - {a.EventId}[{a.ChoiceIndex}] missed=[{string.Join(", ", a.MissedOutcomeIds)}] " +
                    $"hit=[{string.Join(", ", a.HitOutcomeIds)}]");
            }

            if (missLines == 0)
            {
                sb.AppendLine("  (none within probe budget)");
            }

            sb.AppendLine();
            sb.AppendLine("## Required flags without any setter in catalog");
            var orphanFlags = 0;
            AppendOrphanRequiredFlags(sb, flagSetters, ref orphanFlags);
            if (orphanFlags == 0)
            {
                sb.AppendLine("  (none)");
            }

            sb.AppendLine();
            sb.AppendLine("## All attempts (compact)");
            for (var i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                var status = !a.Attempted
                    ? "SKIP"
                    : a.Resolved
                        ? "OK"
                        : "FAIL";
                var deadTag = a.IsDead ? " DEAD" : string.Empty;
                sb.AppendLine(
                    $"  [{status}{deadTag}] {a.EventId}[{a.ChoiceIndex}] " +
                    $"\"{Trim(a.ChoiceText, 28)}\" delta={a.StatDeltaSummary ?? "-"}");
            }

            sb.AppendLine();
            sb.AppendLine("## Notes");
            sb.AppendLine("- Forced resolve with condition-satisfying GameState (not natural EventSelector weights).");
            sb.AppendLine("- RequiredFlags without setters are force-set for resolve; listed separately if catalog has no setter.");
            sb.AppendLine("- Dead = no non-zero fixed effects, no random outcomes, no set/clear flags.");
            return sb.ToString();
        }

        private static void AppendOrphanRequiredFlags(
            StringBuilder sb,
            Dictionary<string, List<string>> flagSetters,
            ref int orphanFlags)
        {
            var events = LoadAll<EventData>(EventsFolder);
            var required = new HashSet<string>(StringComparer.Ordinal);
            for (var e = 0; e < events.Count; e++)
            {
                var flags = events[e]?.Conditions?.RequiredFlags;
                if (flags == null)
                {
                    continue;
                }

                for (var i = 0; i < flags.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(flags[i]))
                    {
                        required.Add(flags[i]);
                    }
                }
            }

            foreach (var flag in required)
            {
                if (flagSetters.ContainsKey(flag))
                {
                    continue;
                }

                orphanFlags++;
                sb.AppendLine($"  - {flag}");
            }
        }

        private static int Mid(int min, int max)
        {
            if (max < min)
            {
                return min;
            }

            return min + (max - min) / 2;
        }

        private static string Trim(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = text.Replace('\n', ' ').Replace('\r', ' ');
            return text.Length <= max ? text : text.Substring(0, max) + "…";
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
