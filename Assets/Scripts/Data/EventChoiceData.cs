using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 사건 선택지. 고정 효과와 확률형 결과를 함께 정의할 수 있다.
    /// </summary>
    [Serializable]
    public sealed class EventChoiceData
    {
        [SerializeField] private string choiceId;
        [SerializeField] [TextArea(1, 3)] private string text;
        [SerializeField] private List<StatEffect> fixedEffects = new List<StatEffect>();
        [SerializeField] private List<RandomOutcome> randomOutcomes = new List<RandomOutcome>();
        [SerializeField] private List<string> setFlags = new List<string>();
        [SerializeField] private List<string> clearFlags = new List<string>();

        public string ChoiceId => choiceId;
        public string Text => text;
        public IReadOnlyList<StatEffect> FixedEffects => fixedEffects;
        public IReadOnlyList<RandomOutcome> RandomOutcomes => randomOutcomes;
        public IReadOnlyList<string> SetFlags => setFlags ??= new List<string>();
        public IReadOnlyList<string> ClearFlags => clearFlags ??= new List<string>();

        public EventChoiceData()
        {
        }

        public EventChoiceData(
            string choiceId,
            string text,
            List<StatEffect> fixedEffects = null,
            List<RandomOutcome> randomOutcomes = null,
            List<string> setFlags = null,
            List<string> clearFlags = null)
        {
            this.choiceId = choiceId;
            this.text = text;
            this.fixedEffects = fixedEffects ?? new List<StatEffect>();
            this.randomOutcomes = randomOutcomes ?? new List<RandomOutcome>();
            this.setFlags = setFlags ?? new List<string>();
            this.clearFlags = clearFlags ?? new List<string>();
        }

        public List<string> Validate(string context)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add($"{context}: 선택지 텍스트가 비어 있습니다.");
            }

            if (fixedEffects == null)
            {
                errors.Add($"{context}: fixedEffects가 null입니다.");
            }
            else
            {
                for (var i = 0; i < fixedEffects.Count; i++)
                {
                    if (fixedEffects[i] == null)
                    {
                        errors.Add($"{context}.fixedEffects[{i}]가 null입니다.");
                        continue;
                    }

                    var effectError = fixedEffects[i].Validate($"{context}.fixedEffects[{i}]");
                    if (effectError != null)
                    {
                        errors.Add(effectError);
                    }
                }
            }

            if (randomOutcomes == null)
            {
                errors.Add($"{context}: randomOutcomes가 null입니다.");
                return errors;
            }

            var totalWeight = 0;
            for (var i = 0; i < randomOutcomes.Count; i++)
            {
                if (randomOutcomes[i] == null)
                {
                    errors.Add($"{context}.randomOutcomes[{i}]가 null입니다.");
                    continue;
                }

                errors.AddRange(randomOutcomes[i].Validate($"{context}.randomOutcomes[{i}]"));
                totalWeight += randomOutcomes[i].ProbabilityWeight;
            }

            if (randomOutcomes.Count > 0 && totalWeight <= 0)
            {
                errors.Add($"{context}: 확률형 결과 weight 합이 0 이하입니다.");
            }

            return errors;
        }
    }
}
