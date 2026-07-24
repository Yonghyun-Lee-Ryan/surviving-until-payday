using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 데이터 모델 사용 예시. 런타임에 붙이지 않는 참고용 컴포넌트다.
    /// </summary>
    public sealed class GameStateFactoryExample : MonoBehaviour
    {
        [SerializeField] private JobData job;
        [SerializeField] private TraitData trait;
        [SerializeField] private EventData sampleEvent;
        [SerializeField] private int seed = 1;

        [ContextMenu("Log Sample GameState")]
        private void LogSampleGameState()
        {
            if (job == null)
            {
                Debug.LogError("[GameStateFactoryExample] job is not assigned.", this);
                return;
            }

            var state = GameState.CreateFromJob(job, trait, seed);
            Debug.Log(
                $"[GameStateFactoryExample] Day {state.CurrentDay}, Job={state.JobId}, " +
                $"Cash={state.Stats.Cash}, Health={state.Stats.Health}, Stress={state.Stats.Stress}",
                this);

            if (sampleEvent == null)
            {
                return;
            }

            var errors = sampleEvent.Validate();
            if (errors.Count == 0)
            {
                Debug.Log(
                    $"[GameStateFactoryExample] Event '{sampleEvent.Title}' OK, choices={sampleEvent.Choices.Count}",
                    this);
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogWarning($"[GameStateFactoryExample] {error}", sampleEvent);
                }
            }
        }
    }
}
