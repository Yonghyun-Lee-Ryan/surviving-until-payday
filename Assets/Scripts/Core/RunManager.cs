using System;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 회차 진행 오케스트레이션. SceneLoader를 호출하지 않는다.
    /// </summary>
    public sealed class RunManager
    {
        private GameState state;
        private DayManager dayManager;
        private RunStatus status = RunStatus.NotStarted;

        public GameState State => state;
        public DayManager Days => dayManager;
        public RunStatus Status => status;

        public event Action<GameState> RunStarted;
        public event Action<GameState, int> DayStarted;
        public event Action<WeeklySummaryInfo> WeeklySummary;
        public event Action<GameState, FailureReason> RunFailed;
        public event Action<GameState> RunSucceeded;

        public void StartRun(JobData job, TraitData trait, int seed)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            state = GameState.CreateFromJob(job, trait, seed);
            dayManager = new DayManager(state);
            status = RunStatus.InProgress;

            RunStarted?.Invoke(state);
            DayStarted?.Invoke(state, state.CurrentDay);
        }

        /// <summary>
        /// 테스트·디버그용. ScriptableObject 없이 상태를 주입한다.
        /// </summary>
        public void StartRunWithState(GameState gameState, DayOfWeek dayOneWeekday)
        {
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            if (state.CurrentDay < GameState.MinDay || state.CurrentDay > GameState.MaxDay)
            {
                state.CurrentDay = GameState.MinDay;
            }

            dayManager = new DayManager(state, dayOneWeekday);
            status = RunStatus.InProgress;

            RunStarted?.Invoke(state);
            DayStarted?.Invoke(state, state.CurrentDay);
        }

        public void StartRunWithState(GameState gameState)
        {
            StartRunWithState(gameState, DayCalendar.DefaultDayOneWeekday);
        }

        /// <summary>
        /// 현재 날짜의 사건 선택이 끝난 뒤 호출한다.
        /// </summary>
        public DayAdvanceResult CompleteCurrentDay()
        {
            if (status != RunStatus.InProgress)
            {
                return DayAdvanceResult.Rejected(
                    $"Cannot complete day while status is {status}.",
                    state != null ? state.CurrentDay : 0);
            }

            if (state == null || dayManager == null)
            {
                Debug.LogError("[RunManager] CompleteCurrentDay called before StartRun.");
                return DayAdvanceResult.Rejected("Run has not started.", 0);
            }

            var dayBefore = state.CurrentDay;
            var failure = state.EvaluateFailure();
            if (failure != FailureReason.None)
            {
                status = RunStatus.Failed;
                RunFailed?.Invoke(state, failure);
                return DayAdvanceResult.Failed(dayBefore, failure);
            }

            var weeklySummary = dayManager.IsWeeklySummaryDay();
            if (weeklySummary)
            {
                var info = new WeeklySummaryInfo(
                    DayManager.GetWeekNumber(dayBefore),
                    dayBefore,
                    state.Clone());
                WeeklySummary?.Invoke(info);
            }

            if (dayManager.IsFinalDay)
            {
                status = RunStatus.Succeeded;
                dayManager.ResetReadyForNextDay();
                RunSucceeded?.Invoke(state);
                return DayAdvanceResult.Succeeded(dayBefore, weeklySummary);
            }

            if (!dayManager.TryAdvanceDay())
            {
                Debug.LogError($"[RunManager] Failed to advance from day {dayBefore}.");
                return DayAdvanceResult.Rejected("Failed to advance day.", dayBefore);
            }

            dayManager.ResetReadyForNextDay();
            DayStarted?.Invoke(state, state.CurrentDay);
            return DayAdvanceResult.Advanced(dayBefore, state.CurrentDay, weeklySummary);
        }

        /// <summary>
        /// EffectResolver 선택 결과가 준비된 경우에만 다음 날로 진행한다.
        /// </summary>
        public DayAdvanceResult TryCompleteCurrentDayAfterChoice(EffectResolver effectResolver)
        {
            if (effectResolver == null)
            {
                Debug.LogError("[RunManager] effectResolver is null.");
                return DayAdvanceResult.Rejected("EffectResolver is null.", state != null ? state.CurrentDay : 0);
            }

            if (!effectResolver.CanAdvanceDay || dayManager == null || !dayManager.ReadyForNextDay)
            {
                return DayAdvanceResult.Rejected(
                    "Choice result is not ready. Resolve a choice before advancing.",
                    state != null ? state.CurrentDay : 0);
            }

            var result = CompleteCurrentDay();
            if (result.Accepted && !result.RunFailed)
            {
                effectResolver.PrepareForNextEvent();
            }

            return result;
        }
    }
}
