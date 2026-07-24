using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 중복 없는 ID 해금 집합.
    /// </summary>
    public sealed class UnlockCodex
    {
        private readonly HashSet<string> unlockedIds = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> UnlockedIds => unlockedIds;

        public int UnlockedCount => unlockedIds.Count;

        public event Action<string> Unlocked;

        public bool IsUnlocked(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && unlockedIds.Contains(id);
        }

        /// <summary>
        /// 새로 해금되면 true.
        /// </summary>
        public bool TryUnlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            if (!unlockedIds.Add(id))
            {
                return false;
            }

            Unlocked?.Invoke(id);
            return true;
        }

        public void LoadFrom(IEnumerable<string> ids)
        {
            unlockedIds.Clear();
            if (ids == null)
            {
                return;
            }

            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    unlockedIds.Add(id);
                }
            }
        }

        public void Clear()
        {
            unlockedIds.Clear();
        }

        public float GetUnlockRate(int totalDefined)
        {
            if (totalDefined <= 0)
            {
                return 0f;
            }

            return unlockedIds.Count / (float)totalDefined;
        }
    }
}
