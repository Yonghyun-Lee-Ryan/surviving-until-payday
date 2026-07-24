using System;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 능력치 증감량. 원본 SO 데이터이며 런타임에서 직접 수정하지 않는다.
    /// </summary>
    [Serializable]
    public sealed class StatEffect
    {
        [SerializeField] private StatType statType;
        [SerializeField] private long value;

        public StatType StatType => statType;
        public long Value => value;

        public StatEffect()
        {
        }

        public StatEffect(StatType statType, long value)
        {
            this.statType = statType;
            this.value = value;
        }

        public string Validate(string context)
        {
            if (StatLimits.IsGaugeStat(statType) && Math.Abs(value) > StatLimits.MaxGauge)
            {
                return $"{context}: {statType} 효과 절대값({value})이 {StatLimits.MaxGauge}을 초과합니다.";
            }

            return null;
        }
    }
}
