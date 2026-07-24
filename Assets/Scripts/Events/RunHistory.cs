using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Events
{
    /// <summary>
    /// 회차 중 사건 선택 이력.
    /// </summary>
    public sealed class RunHistory
    {
        private readonly List<ChoiceResult> entries = new List<ChoiceResult>();

        public IReadOnlyList<ChoiceResult> Entries => entries;

        public int Count => entries.Count;

        public void Add(ChoiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            entries.Add(result);
        }

        public void Clear()
        {
            entries.Clear();
        }

        public bool TryGetLast(out ChoiceResult result)
        {
            if (entries.Count == 0)
            {
                result = null;
                return false;
            }

            result = entries[entries.Count - 1];
            return true;
        }

        public bool TryRemoveLast(out ChoiceResult result)
        {
            if (!TryGetLast(out result))
            {
                return false;
            }

            entries.RemoveAt(entries.Count - 1);
            return true;
        }
    }
}
