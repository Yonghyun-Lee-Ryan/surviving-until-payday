using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 날짜 시드로 일일 미션 1~3개를 고른다.
    /// </summary>
    public static class DailyMissionAssigner
    {
        public const int MinAssign = 1;
        public const int MaxAssign = 3;

        public static List<DailyMissionData> Assign(
            IReadOnlyList<DailyMissionData> pool,
            int seed,
            int? forcedCount = null)
        {
            var result = new List<DailyMissionData>();
            if (pool == null || pool.Count == 0)
            {
                return result;
            }

            var candidates = new List<DailyMissionData>();
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !string.IsNullOrWhiteSpace(pool[i].Id))
                {
                    candidates.Add(pool[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return result;
            }

            var rng = new SeededRandomService(seed);
            var count = forcedCount ?? (MinAssign + rng.Next(MaxAssign - MinAssign + 1));
            count = Math.Max(MinAssign, Math.Min(count, Math.Min(MaxAssign, candidates.Count)));

            for (var i = 0; i < count; i++)
            {
                var index = rng.Next(candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return result;
        }
    }
}
