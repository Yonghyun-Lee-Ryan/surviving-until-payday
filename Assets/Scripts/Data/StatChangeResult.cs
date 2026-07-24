using System;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 단일 능력치 변경 전후 기록.
    /// </summary>
    public readonly struct StatChangeResult : IEquatable<StatChangeResult>
    {
        public StatType StatType { get; }
        public long Before { get; }
        public long After { get; }
        public long AppliedDelta { get; }

        public long ActualDelta => After - Before;
        public bool Changed => Before != After;
        public bool WasClamped => AppliedDelta != ActualDelta;

        public StatChangeResult(StatType statType, long before, long after, long appliedDelta)
        {
            StatType = statType;
            Before = before;
            After = after;
            AppliedDelta = appliedDelta;
        }

        public bool Equals(StatChangeResult other)
        {
            return StatType == other.StatType
                   && Before == other.Before
                   && After == other.After
                   && AppliedDelta == other.AppliedDelta;
        }

        public override bool Equals(object obj)
        {
            return obj is StatChangeResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)StatType;
                hash = (hash * 397) ^ Before.GetHashCode();
                hash = (hash * 397) ^ After.GetHashCode();
                hash = (hash * 397) ^ AppliedDelta.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{StatType}: {Before} -> {After} (delta {ActualDelta}, requested {AppliedDelta})";
        }
    }
}
