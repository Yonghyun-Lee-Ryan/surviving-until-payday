using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 사건 정의 데이터. 선택지·조건·가중치를 포함하며 런타임에서 원본을 수정하지 않는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Event_",
        menuName = "Survive Until Payday/Data/Event",
        order = 30)]
    public sealed class EventData : ScriptableObject
    {
        public const int ExpectedChoiceCount = 3;

        [SerializeField] private string id = "event_untitled";
        [SerializeField] private string title = "제목 없는 사건";
        [SerializeField] [TextArea(3, 6)] private string description;
        [SerializeField] private EventCategory category = EventCategory.Work;
        [SerializeField] private int minDay = GameState.MinDay;
        [SerializeField] private int maxDay = GameState.MaxDay;
        [SerializeField] [Min(0)] private int weight = 100;
        [SerializeField] private bool isFixedEvent;
        [SerializeField] private int fixedDay;
        [SerializeField] private EventCondition conditions = new EventCondition();
        [SerializeField] private List<EventChoiceData> choices = new List<EventChoiceData>();

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public EventCategory Category => category;
        public int MinDay => minDay;
        public int MaxDay => maxDay;
        public int Weight => weight;
        public bool IsFixedEvent => isFixedEvent;
        public int FixedDay => fixedDay;
        public EventCondition Conditions => conditions;
        public IReadOnlyList<EventChoiceData> Choices => choices;

        private void OnValidate()
        {
            foreach (var error in Validate())
            {
                Debug.LogWarning($"[EventData:{name}] {error}", this);
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

            if (string.IsNullOrWhiteSpace(description))
            {
                errors.Add("description이 비어 있습니다.");
            }

            if (minDay < GameState.MinDay || maxDay > GameState.MaxDay)
            {
                errors.Add($"날짜 범위는 {GameState.MinDay}~{GameState.MaxDay}여야 합니다. (minDay={minDay}, maxDay={maxDay})");
            }

            if (minDay > maxDay)
            {
                errors.Add($"minDay({minDay}) > maxDay({maxDay})");
            }

            if (weight < 0)
            {
                errors.Add($"weight({weight})는 0 이상이어야 합니다.");
            }

            if (isFixedEvent)
            {
                if (fixedDay < GameState.MinDay || fixedDay > GameState.MaxDay)
                {
                    errors.Add($"fixedDay({fixedDay})는 {GameState.MinDay}~{GameState.MaxDay}여야 합니다.");
                }
            }

            if (conditions == null)
            {
                errors.Add("conditions가 null입니다.");
            }
            else
            {
                var conditionError = conditions.Validate("conditions");
                if (conditionError != null)
                {
                    errors.Add(conditionError);
                }
            }

            if (choices == null)
            {
                errors.Add("choices가 null입니다.");
                return errors;
            }

            if (choices.Count != ExpectedChoiceCount)
            {
                errors.Add($"선택지는 {ExpectedChoiceCount}개여야 합니다. (현재 {choices.Count}개)");
            }

            for (var i = 0; i < choices.Count; i++)
            {
                if (choices[i] == null)
                {
                    errors.Add($"choices[{i}]가 null입니다.");
                    continue;
                }

                errors.AddRange(choices[i].Validate($"choices[{i}]"));
            }

            return errors;
        }

#if UNITY_EDITOR
        public void EditorSetCore(
            string newId,
            string newTitle,
            string newDescription,
            EventCategory newCategory,
            int newMinDay,
            int newMaxDay,
            int newWeight,
            EventCondition newConditions,
            List<EventChoiceData> newChoices)
        {
            id = newId;
            title = newTitle;
            description = newDescription;
            category = newCategory;
            minDay = newMinDay;
            maxDay = newMaxDay;
            weight = newWeight;
            conditions = newConditions ?? new EventCondition();
            choices = newChoices ?? new List<EventChoiceData>();
        }

        public void EditorSetFixed(bool isFixed, int day)
        {
            isFixedEvent = isFixed;
            fixedDay = day;
        }
#endif
    }
}
