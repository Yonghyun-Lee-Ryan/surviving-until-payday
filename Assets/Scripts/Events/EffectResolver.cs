using System;
using System.Collections.Generic;
using System.Text;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Events
{
    /// <summary>
    /// 선택지 고정 효과·확률형 결과 적용 및 선택 잠금.
    /// </summary>
    public sealed class EffectResolver
    {
        private readonly GameState state;
        private readonly IRandomService random;
        private readonly RunHistory history;
        private readonly DayManager dayManager;
        private readonly TraitData activeTrait;

        private EventData activeEvent;
        private ChoicePhase phase = ChoicePhase.NoActiveEvent;
        private ChoiceResult lastResult;

        public EffectResolver(
            GameState state,
            IRandomService random,
            RunHistory history,
            DayManager dayManager,
            TraitData activeTrait = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.history = history ?? throw new ArgumentNullException(nameof(history));
            this.dayManager = dayManager ?? throw new ArgumentNullException(nameof(dayManager));
            this.activeTrait = activeTrait;
        }

        public ChoicePhase Phase => phase;
        public EventData ActiveEvent => activeEvent;
        public ChoiceResult LastResult => lastResult;
        public bool CanSelectChoice => phase == ChoicePhase.AwaitingChoice;
        public bool CanAdvanceDay => phase == ChoicePhase.ResultReady;

        public event Action<ChoiceResult> ChoiceResolved;
        public event Action<EventData> EventStarted;

        /// <summary>
        /// 하루의 사건을 시작하고 선택 입력을 연다.
        /// </summary>
        /// <param name="replaceActiveChoice">
        /// true면 선택 대기 중 교체(광고 새로고침 등)로 보고 경고를 내지 않는다.
        /// </param>
        public void BeginEvent(EventData eventData, bool replaceActiveChoice = false)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (phase == ChoicePhase.AwaitingChoice && !replaceActiveChoice)
            {
                Debug.LogWarning(
                    $"[EffectResolver] BeginEvent called while awaiting choice for '{activeEvent?.Id}'. Replacing active event.");
            }

            activeEvent = eventData;
            lastResult = null;
            phase = ChoicePhase.AwaitingChoice;
            dayManager.ResetReadyForNextDay();
            EventStarted?.Invoke(eventData);
        }

        /// <summary>
        /// 선택지를 적용한다. 중복 입력이면 false.
        /// </summary>
        public bool TryResolveChoice(int choiceIndex, out ChoiceResult result, out string error)
        {
            result = null;
            error = null;

            if (phase != ChoicePhase.AwaitingChoice)
            {
                error = phase == ChoicePhase.ResultReady
                    ? "Choice already resolved. Wait for next day."
                    : "No active event awaiting choice.";
                return false;
            }

            if (activeEvent == null)
            {
                error = "Active event is null.";
                phase = ChoicePhase.NoActiveEvent;
                return false;
            }

            var choices = activeEvent.Choices;
            if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                error = $"Invalid choiceIndex {choiceIndex}. Choice count={choices?.Count ?? 0}.";
                return false;
            }

            var choice = choices[choiceIndex];
            if (choice == null)
            {
                error = $"Choice at index {choiceIndex} is null.";
                return false;
            }

            var statsBefore = state.Stats.Clone();
            var effectsToApply = new List<StatEffect>();

            if (choice.FixedEffects != null)
            {
                for (var i = 0; i < choice.FixedEffects.Count; i++)
                {
                    if (choice.FixedEffects[i] != null)
                    {
                        effectsToApply.Add(choice.FixedEffects[i]);
                    }
                }
            }

            RandomOutcome selectedOutcome = null;
            if (choice.RandomOutcomes != null && choice.RandomOutcomes.Count > 0)
            {
                selectedOutcome = PickRandomOutcome(choice.RandomOutcomes);
                if (selectedOutcome?.Effects != null)
                {
                    for (var i = 0; i < selectedOutcome.Effects.Count; i++)
                    {
                        if (selectedOutcome.Effects[i] != null)
                        {
                            effectsToApply.Add(selectedOutcome.Effects[i]);
                        }
                    }
                }
            }

            effectsToApply = TraitRuntimeModifier.Apply(activeTrait, activeEvent.Category, effectsToApply);
            effectsToApply = ApplyDifficultyToCashLosses(effectsToApply);

            ApplyFlagMutations(choice.SetFlags, choice.ClearFlags, null);
            if (selectedOutcome != null)
            {
                ApplyFlagMutations(
                    selectedOutcome.SetFlags,
                    selectedOutcome.ClearFlags,
                    selectedOutcome.QueueEventId);
            }

            var allChanges = effectsToApply.Count > 0
                ? state.ApplyEffects(effectsToApply)
                : (IReadOnlyList<StatChangeResult>)Array.Empty<StatChangeResult>();

            var message = BuildResultMessage(choice, selectedOutcome);
            result = new ChoiceResult(
                state.CurrentDay,
                activeEvent.Id,
                activeEvent.Title,
                choiceIndex,
                choice.ChoiceId,
                choice.Text,
                message,
                selectedOutcome?.OutcomeId,
                selectedOutcome?.ResultMessage,
                statsBefore,
                state.Stats.Clone(),
                allChanges,
                state.EvaluateFailure());

            lastResult = result;
            history.Add(result);
            phase = ChoicePhase.ResultReady;
            dayManager.MarkReadyForNextDay();
            ChoiceResolved?.Invoke(result);
            return true;
        }

        /// <summary>
        /// 다음 날로 넘어간 뒤 호출해 선택 잠금을 해제 준비 상태로 되돌린다.
        /// </summary>
        public void PrepareForNextEvent()
        {
            activeEvent = null;
            lastResult = null;
            phase = ChoicePhase.NoActiveEvent;
            dayManager.ResetReadyForNextDay();
        }

        /// <summary>
        /// 광고(결과 재시도)용: 직전 선택 효과를 되돌리고 다시 선택 가능하게 한다.
        /// </summary>
        public bool TryUndoLastChoice(out string error)
        {
            error = null;
            if (phase != ChoicePhase.ResultReady || lastResult == null || activeEvent == null)
            {
                error = "No resolved choice to undo.";
                return false;
            }

            history.TryRemoveLast(out _);
            state.Stats.CopyFrom(lastResult.StatsBefore);
            lastResult = null;
            phase = ChoicePhase.AwaitingChoice;
            dayManager.ResetReadyForNextDay();
            return true;
        }

        private RandomOutcome PickRandomOutcome(IReadOnlyList<RandomOutcome> outcomes)
        {
            var totalWeight = 0;
            for (var i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] == null)
                {
                    continue;
                }

                totalWeight += Math.Max(0, outcomes[i].ProbabilityWeight);
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning("[EffectResolver] RandomOutcomes total weight is 0. Skipping random outcome.");
                return null;
            }

            var roll = random.Next(totalWeight);
            var cumulative = 0;
            for (var i = 0; i < outcomes.Count; i++)
            {
                var outcome = outcomes[i];
                if (outcome == null)
                {
                    continue;
                }

                cumulative += Math.Max(0, outcome.ProbabilityWeight);
                if (roll < cumulative)
                {
                    return outcome;
                }
            }

            for (var i = outcomes.Count - 1; i >= 0; i--)
            {
                if (outcomes[i] != null)
                {
                    return outcomes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Unit 19: 난도 계수는 현금 손실에만 적용한다. (DifficultyScaler 문서 참고)
        /// </summary>
        private List<StatEffect> ApplyDifficultyToCashLosses(List<StatEffect> effects)
        {
            if (effects == null || effects.Count == 0 || dayManager == null)
            {
                return effects;
            }

            var multiplier = dayManager.DifficultyMultiplier;
            if (Math.Abs(multiplier - 1f) < 0.0001f)
            {
                return effects;
            }

            var scaled = new List<StatEffect>(effects.Count);
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                if (effect.StatType == StatType.Cash && effect.Value < 0L)
                {
                    scaled.Add(new StatEffect(
                        StatType.Cash,
                        DifficultyScaler.ScaleCashDelta(effect.Value, multiplier)));
                }
                else
                {
                    scaled.Add(effect);
                }
            }

            return scaled;
        }

        private void ApplyFlagMutations(
            IReadOnlyList<string> setFlags,
            IReadOnlyList<string> clearFlags,
            string queueEventId)
        {
            if (clearFlags != null)
            {
                for (var i = 0; i < clearFlags.Count; i++)
                {
                    state.ClearFlag(clearFlags[i]);
                }
            }

            if (setFlags != null)
            {
                for (var i = 0; i < setFlags.Count; i++)
                {
                    state.SetFlag(setFlags[i]);
                }
            }

            if (!string.IsNullOrWhiteSpace(queueEventId))
            {
                state.EnqueueFollowUp(queueEventId);
            }
        }

        private static string BuildResultMessage(EventChoiceData choice, RandomOutcome outcome)
        {
            if (outcome != null && !string.IsNullOrWhiteSpace(outcome.ResultMessage))
            {
                return outcome.ResultMessage;
            }

            if (!string.IsNullOrWhiteSpace(choice.Text))
            {
                return $"「{choice.Text}」를 선택했다.";
            }

            return "선택을 반영했다.";
        }

        public static string FormatStatChanges(IReadOnlyList<StatChangeResult> changes)
        {
            if (changes == null || changes.Count == 0)
            {
                return "변화 없음";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < changes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                var change = changes[i];
                var sign = change.ActualDelta > 0 ? "+" : string.Empty;
                builder.Append(change.StatType);
                builder.Append(' ');
                builder.Append(sign);
                builder.Append(change.ActualDelta);
            }

            return builder.ToString();
        }
    }
}
