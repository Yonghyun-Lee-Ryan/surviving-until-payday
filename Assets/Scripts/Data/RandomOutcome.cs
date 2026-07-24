using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 확률형 결과 한 갈래. probabilityWeight 비율로 선택된다.
    /// </summary>
    [Serializable]
    public sealed class RandomOutcome
    {
        [SerializeField] private string outcomeId;
        [SerializeField] private string resultMessage;
        [SerializeField] [Min(0)] private int probabilityWeight = 100;
        [SerializeField] private List<StatEffect> effects = new List<StatEffect>();
        [SerializeField] private List<string> setFlags = new List<string>();
        [SerializeField] private List<string> clearFlags = new List<string>();
        [SerializeField] private string queueEventId = string.Empty;

        public string OutcomeId => outcomeId;
        public string ResultMessage => resultMessage;
        public int ProbabilityWeight => probabilityWeight;
        public IReadOnlyList<StatEffect> Effects => effects;
        public IReadOnlyList<string> SetFlags => setFlags ??= new List<string>();
        public IReadOnlyList<string> ClearFlags => clearFlags ??= new List<string>();
        public string QueueEventId => queueEventId;

        public RandomOutcome()
        {
        }

        public RandomOutcome(
            string outcomeId,
            string resultMessage,
            int probabilityWeight,
            params StatEffect[] effects)
            : this(
                outcomeId,
                resultMessage,
                probabilityWeight,
                (IEnumerable<StatEffect>)effects,
                null,
                null,
                null)
        {
        }

        public RandomOutcome(
            string outcomeId,
            string resultMessage,
            int probabilityWeight,
            IEnumerable<StatEffect> effects,
            IEnumerable<string> setFlags,
            IEnumerable<string> clearFlags,
            string queueEventId)
        {
            this.outcomeId = outcomeId;
            this.resultMessage = resultMessage;
            this.probabilityWeight = probabilityWeight;
            this.effects = effects != null
                ? new List<StatEffect>(effects)
                : new List<StatEffect>();
            this.setFlags = setFlags != null ? new List<string>(setFlags) : new List<string>();
            this.clearFlags = clearFlags != null ? new List<string>(clearFlags) : new List<string>();
            this.queueEventId = queueEventId ?? string.Empty;
        }

        public List<string> Validate(string context)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(outcomeId))
            {
                errors.Add($"{context}: outcomeId가 비어 있습니다.");
            }

            if (probabilityWeight < 0)
            {
                errors.Add($"{context}: probabilityWeight는 0 이상이어야 합니다.");
            }

            if (effects == null)
            {
                errors.Add($"{context}: effects가 null입니다.");
                return errors;
            }

            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                {
                    errors.Add($"{context}.effects[{i}]가 null입니다.");
                    continue;
                }

                var effectError = effects[i].Validate($"{context}.effects[{i}]");
                if (effectError != null)
                {
                    errors.Add(effectError);
                }
            }

            return errors;
        }
    }
}
