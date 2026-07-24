using System;
using System.Collections.Generic;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Events
{
    /// <summary>
    /// 날짜·상태 조건에 맞는 사건을 가중치로 선택한다.
    /// </summary>
    public sealed class EventSelector
    {
        public const int DefaultRecentHistorySize = 5;
        public const float DefaultRecentWeightMultiplier = 0.25f;

        private readonly List<EventData> catalog;
        private readonly EventData fallbackEvent;
        private readonly IRandomService random;
        private readonly int recentHistorySize;
        private readonly float recentWeightMultiplier;
        private readonly Queue<string> recentEventIds = new Queue<string>();
        private string lastSelectedEventId;

        public EventSelector(
            IEnumerable<EventData> catalog,
            EventData fallbackEvent,
            IRandomService random,
            int recentHistorySize = DefaultRecentHistorySize,
            float recentWeightMultiplier = DefaultRecentWeightMultiplier)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            this.fallbackEvent = fallbackEvent
                                 ?? throw new ArgumentNullException(nameof(fallbackEvent));
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            if (recentHistorySize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recentHistorySize));
            }

            if (recentWeightMultiplier < 0f || recentWeightMultiplier > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(recentWeightMultiplier));
            }

            this.recentHistorySize = recentHistorySize;
            this.recentWeightMultiplier = recentWeightMultiplier;
            this.catalog = new List<EventData>();

            foreach (var eventData in catalog)
            {
                if (eventData == null)
                {
                    Debug.LogWarning("[EventSelector] Null EventData in catalog was skipped.");
                    continue;
                }

                this.catalog.Add(eventData);
            }
        }

        public IReadOnlyList<string> RecentEventIds => recentEventIds.ToArray();

        public string LastSelectedEventId => lastSelectedEventId;

        public void ClearHistory()
        {
            recentEventIds.Clear();
            lastSelectedEventId = null;
        }

        public void RestoreHistory(IEnumerable<string> recentIds, string lastSelectedId)
        {
            recentEventIds.Clear();
            lastSelectedEventId = string.IsNullOrEmpty(lastSelectedId) ? null : lastSelectedId;

            if (recentIds == null)
            {
                return;
            }

            foreach (var id in recentIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                recentEventIds.Enqueue(id);
                while (recentHistorySize > 0 && recentEventIds.Count > recentHistorySize)
                {
                    recentEventIds.Dequeue();
                }
            }
        }

        public EventData Select(GameState state, bool isWeekend)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var day = state.CurrentDay;

            if (TrySelectQueuedFollowUp(state, isWeekend, out var queued))
            {
                Remember(queued);
                return queued;
            }

            var fixedCandidates = CollectFixedCandidates(day, state, isWeekend);
            if (fixedCandidates.Count > 0)
            {
                return SelectAndRemember(fixedCandidates, preferAvoidLast: false);
            }

            var pool = CollectRandomCandidates(day, state, isWeekend);
            if (pool.Count == 0)
            {
                Remember(fallbackEvent);
                return fallbackEvent;
            }

            return SelectAndRemember(pool, preferAvoidLast: true);
        }

        public EventData Select(GameState state, DayManager dayManager)
        {
            if (dayManager == null)
            {
                throw new ArgumentNullException(nameof(dayManager));
            }

            return Select(state, dayManager.IsWeekend);
        }

        /// <summary>
        /// 광고「다른 사건 보기」용. 고정 일자 사건만 있는 날에도 일반 풀에서 다른 사건을 고른다.
        /// 대안이 없으면 null.
        /// </summary>
        public EventData SelectRerollAlternative(
            GameState state,
            DayManager dayManager,
            string excludeEventId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (dayManager == null)
            {
                throw new ArgumentNullException(nameof(dayManager));
            }

            var isWeekend = dayManager.IsWeekend;
            var day = state.CurrentDay;

            var randomPool = CollectRandomCandidates(day, state, isWeekend);
            var alternatives = ExcludeEvent(randomPool, excludeEventId);
            if (alternatives.Count > 0)
            {
                return SelectAndRemember(alternatives, preferAvoidLast: false);
            }

            // 일반 풀이 비면 같은 날 고정 후보 중 다른 것(드묾)을 시도
            var fixedPool = CollectFixedCandidates(day, state, isWeekend);
            alternatives = ExcludeEvent(fixedPool, excludeEventId);
            if (alternatives.Count > 0)
            {
                return SelectAndRemember(alternatives, preferAvoidLast: false);
            }

            return null;
        }

        private static List<WeightedCandidate> ExcludeEvent(
            List<WeightedCandidate> source,
            string excludeEventId)
        {
            if (source == null || source.Count == 0)
            {
                return new List<WeightedCandidate>();
            }

            if (string.IsNullOrEmpty(excludeEventId))
            {
                return source;
            }

            var filtered = new List<WeightedCandidate>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                if (!string.Equals(source[i].Event.Id, excludeEventId, StringComparison.Ordinal))
                {
                    filtered.Add(source[i]);
                }
            }

            return filtered;
        }

        public List<EventData> GetEligibleEvents(GameState state, bool isWeekend)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var fixedCandidates = CollectFixedCandidates(state.CurrentDay, state, isWeekend);
            if (fixedCandidates.Count > 0)
            {
                var fixedOnly = new List<EventData>(fixedCandidates.Count);
                foreach (var candidate in fixedCandidates)
                {
                    fixedOnly.Add(candidate.Event);
                }

                return fixedOnly;
            }

            var pool = CollectRandomCandidates(state.CurrentDay, state, isWeekend);
            var result = new List<EventData>(pool.Count);
            foreach (var candidate in pool)
            {
                result.Add(candidate.Event);
            }

            return result;
        }

        private bool TrySelectQueuedFollowUp(GameState state, bool isWeekend, out EventData eventData)
        {
            eventData = null;
            while (state.TryDequeueFollowUp(out var queuedId))
            {
                var found = FindById(queuedId);
                if (found == null)
                {
                    Debug.LogWarning($"[EventSelector] Queued follow-up '{queuedId}' not in catalog. Skipping.");
                    continue;
                }

                // 후속 사건은 큐에 들어왔으면 우선 강제. 플래그 금지만 존중한다.
                if (found.Conditions != null
                    && found.Conditions.ForbiddenFlags != null)
                {
                    var blocked = false;
                    for (var i = 0; i < found.Conditions.ForbiddenFlags.Count; i++)
                    {
                        var flag = found.Conditions.ForbiddenFlags[i];
                        if (!string.IsNullOrWhiteSpace(flag) && state.HasFlag(flag))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (blocked)
                    {
                        continue;
                    }
                }

                if (found.Conditions != null
                    && found.Conditions.RequiredFlags != null)
                {
                    var missing = false;
                    for (var i = 0; i < found.Conditions.RequiredFlags.Count; i++)
                    {
                        var flag = found.Conditions.RequiredFlags[i];
                        if (!string.IsNullOrWhiteSpace(flag) && !state.HasFlag(flag))
                        {
                            missing = true;
                            break;
                        }
                    }

                    if (missing)
                    {
                        continue;
                    }
                }

                eventData = found;
                return true;
            }

            return false;
        }

        private EventData FindById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            for (var i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null && catalog[i].Id == eventId)
                {
                    return catalog[i];
                }
            }

            if (fallbackEvent != null && fallbackEvent.Id == eventId)
            {
                return fallbackEvent;
            }

            return null;
        }

        private List<WeightedCandidate> CollectFixedCandidates(int day, GameState state, bool isWeekend)
        {
            var result = new List<WeightedCandidate>();

            for (var i = 0; i < catalog.Count; i++)
            {
                var eventData = catalog[i];
                if (!eventData.IsFixedEvent || eventData.FixedDay != day)
                {
                    continue;
                }

                if (!IsGenerallyEligible(eventData, day, state, isWeekend))
                {
                    continue;
                }

                result.Add(CreateCandidate(eventData, applyRecentPenalty: false));
            }

            return result;
        }

        private List<WeightedCandidate> CollectRandomCandidates(int day, GameState state, bool isWeekend)
        {
            var result = new List<WeightedCandidate>();

            for (var i = 0; i < catalog.Count; i++)
            {
                var eventData = catalog[i];
                if (eventData.IsFixedEvent)
                {
                    continue;
                }

                if (!IsGenerallyEligible(eventData, day, state, isWeekend))
                {
                    continue;
                }

                result.Add(CreateCandidate(eventData, applyRecentPenalty: true));
            }

            return result;
        }

        private static bool IsGenerallyEligible(
            EventData eventData,
            int day,
            GameState state,
            bool isWeekend)
        {
            if (eventData.Weight <= 0)
            {
                return false;
            }

            if (!EventConditionEvaluator.MatchesDayRange(eventData, day))
            {
                return false;
            }

            return EventConditionEvaluator.Matches(eventData.Conditions, state, isWeekend);
        }

        private WeightedCandidate CreateCandidate(EventData eventData, bool applyRecentPenalty)
        {
            var weight = eventData.Weight;
            if (applyRecentPenalty && IsInRecentHistory(eventData.Id))
            {
                var reduced = (int)Math.Round(weight * recentWeightMultiplier);
                weight = Math.Max(1, reduced);
            }

            return new WeightedCandidate(eventData, weight);
        }

        private bool IsInRecentHistory(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            foreach (var recentId in recentEventIds)
            {
                if (string.Equals(recentId, eventId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private EventData SelectAndRemember(List<WeightedCandidate> candidates, bool preferAvoidLast)
        {
            var pool = candidates;
            if (preferAvoidLast && !string.IsNullOrEmpty(lastSelectedEventId) && candidates.Count > 1)
            {
                var filtered = new List<WeightedCandidate>(candidates.Count);
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (!string.Equals(candidates[i].Event.Id, lastSelectedEventId, StringComparison.Ordinal))
                    {
                        filtered.Add(candidates[i]);
                    }
                }

                if (filtered.Count > 0)
                {
                    pool = filtered;
                }
            }

            var selected = PickWeighted(pool);
            Remember(selected);
            return selected;
        }

        private EventData PickWeighted(List<WeightedCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                Remember(fallbackEvent);
                return fallbackEvent;
            }

            var totalWeight = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                totalWeight += Math.Max(0, candidates[i].Weight);
            }

            if (totalWeight <= 0)
            {
                Remember(fallbackEvent);
                return fallbackEvent;
            }

            var roll = random.Next(totalWeight);
            var cumulative = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += Math.Max(0, candidates[i].Weight);
                if (roll < cumulative)
                {
                    return candidates[i].Event;
                }
            }

            return candidates[candidates.Count - 1].Event;
        }

        private void Remember(EventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            lastSelectedEventId = eventData.Id;
            if (recentHistorySize <= 0)
            {
                return;
            }

            recentEventIds.Enqueue(eventData.Id);
            while (recentEventIds.Count > recentHistorySize)
            {
                recentEventIds.Dequeue();
            }
        }

        private readonly struct WeightedCandidate
        {
            public EventData Event { get; }
            public int Weight { get; }

            public WeightedCandidate(EventData eventData, int weight)
            {
                Event = eventData;
                Weight = weight;
            }
        }
    }
}
