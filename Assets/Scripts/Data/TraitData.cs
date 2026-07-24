using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 특성 정의 데이터. 시작 능력치 보정과 런타임 배율을 정의한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Trait_",
        menuName = "Survive Until Payday/Data/Trait",
        order = 20)]
    public sealed class TraitData : ScriptableObject
    {
        [SerializeField] private string id = "trait_thrifty";
        [SerializeField] private string displayName = "짠돌이";
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private int unlockLevel;
        [SerializeField] private List<StatEffect> startingStatModifiers = new List<StatEffect>();

        [Header("Runtime Multipliers (1 = no change)")]
        [SerializeField] [Range(0f, 2f)] private float cashLossMultiplier = 1f;
        [SerializeField] [Range(0f, 2f)] private float happinessGainMultiplier = 1f;
        [SerializeField] [Range(0f, 2f)] private float workStressGainMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int UnlockLevel => unlockLevel;
        public IReadOnlyList<StatEffect> StartingStatModifiers => startingStatModifiers;
        public float CashLossMultiplier => cashLossMultiplier;
        public float HappinessGainMultiplier => happinessGainMultiplier;
        public float WorkStressGainMultiplier => workStressGainMultiplier;

        /// <summary>
        /// 시작 스탯 복사본에만 적용한다. ScriptableObject 원본은 변경하지 않는다.
        /// </summary>
        public void ApplyStartingModifiers(PlayerStats stats)
        {
            if (stats == null)
            {
                Debug.LogError($"[TraitData:{name}] ApplyStartingModifiers stats is null.");
                return;
            }

            if (startingStatModifiers == null)
            {
                return;
            }

            for (var i = 0; i < startingStatModifiers.Count; i++)
            {
                var effect = startingStatModifiers[i];
                if (effect == null)
                {
                    Debug.LogWarning($"[TraitData:{name}] startingStatModifiers[{i}] is null.");
                    continue;
                }

                var next = stats.GetStat(effect.StatType) + effect.Value;
                if (StatLimits.IsGaugeStat(effect.StatType))
                {
                    next = StatLimits.ClampGauge((int)next);
                }

                stats.SetStat(effect.StatType, next);
            }
        }

        private void OnValidate()
        {
            foreach (var error in Validate())
            {
                Debug.LogWarning($"[TraitData:{name}] {error}", this);
            }
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("id가 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add("displayName이 비어 있습니다.");
            }

            if (unlockLevel < 0)
            {
                errors.Add($"unlockLevel({unlockLevel})는 0 이상이어야 합니다.");
            }

            ValidateMultiplier("cashLossMultiplier", cashLossMultiplier, errors);
            ValidateMultiplier("happinessGainMultiplier", happinessGainMultiplier, errors);
            ValidateMultiplier("workStressGainMultiplier", workStressGainMultiplier, errors);

            if (startingStatModifiers == null)
            {
                errors.Add("startingStatModifiers가 null입니다.");
                return errors;
            }

            for (var i = 0; i < startingStatModifiers.Count; i++)
            {
                if (startingStatModifiers[i] == null)
                {
                    errors.Add($"startingStatModifiers[{i}]가 null입니다.");
                    continue;
                }

                var effectError = startingStatModifiers[i].Validate($"startingStatModifiers[{i}]");
                if (effectError != null)
                {
                    errors.Add(effectError);
                }
            }

            return errors;
        }

        private static void ValidateMultiplier(string fieldName, float value, List<string> errors)
        {
            if (value < 0f || value > 2f)
            {
                errors.Add($"{fieldName}({value})는 0~2 범위여야 합니다.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(string newId, string newDisplayName, string newDescription, int newUnlockLevel)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            unlockLevel = newUnlockLevel;
        }

        public void EditorSetRuntimeMultipliers(
            float cashLoss,
            float happinessGain,
            float workStressGain)
        {
            cashLossMultiplier = cashLoss;
            happinessGainMultiplier = happinessGain;
            workStressGainMultiplier = workStressGain;
        }
#endif
    }
}
