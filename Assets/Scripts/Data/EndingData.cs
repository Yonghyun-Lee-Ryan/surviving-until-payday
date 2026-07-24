using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 엔딩 정의 데이터.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Ending_",
        menuName = "Survive Until Payday/Data/Ending",
        order = 40)]
    public sealed class EndingData : ScriptableObject
    {
        [SerializeField] private string id = "ending_barely_survived";
        [SerializeField] private string title = "겨우 살아남았다";
        [SerializeField] [TextArea(2, 5)] private string description;
        [SerializeField] private int priority;
        [SerializeField] private bool isFailureEnding;
        [SerializeField] private FailureReason linkedFailureReason = FailureReason.None;
        [SerializeField] private EndingCondition condition = new EndingCondition();

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public int Priority => priority;
        public bool IsFailureEnding => isFailureEnding;
        public FailureReason LinkedFailureReason => linkedFailureReason;
        public EndingCondition Condition => condition;

        private void OnValidate()
        {
            foreach (var error in Validate())
            {
                Debug.LogWarning($"[EndingData:{name}] {error}", this);
            }
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("id가 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add("title이 비어 있습니다.");
            }

            if (isFailureEnding && linkedFailureReason == FailureReason.None)
            {
                errors.Add("실패 엔딩은 linkedFailureReason이 필요합니다.");
            }

            if (condition == null)
            {
                errors.Add("condition이 null입니다.");
            }
            else
            {
                var conditionError = condition.Validate("condition");
                if (conditionError != null)
                {
                    errors.Add(conditionError);
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            string newTitle,
            string newDescription,
            int newPriority,
            bool failureEnding,
            FailureReason failureReason,
            EndingCondition newCondition)
        {
            id = newId;
            title = newTitle;
            description = newDescription;
            priority = newPriority;
            isFailureEnding = failureEnding;
            linkedFailureReason = failureReason;
            condition = newCondition ?? new EndingCondition();
        }
#endif
    }
}
