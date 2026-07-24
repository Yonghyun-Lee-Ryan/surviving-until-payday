using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 엔딩 도감. UnlockCodex 래퍼(기존 테스트/호출 호환).
    /// </summary>
    public sealed class EndingCodex
    {
        private readonly UnlockCodex inner;

        public EndingCodex()
            : this(new UnlockCodex())
        {
        }

        public EndingCodex(UnlockCodex inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public UnlockCodex Inner => inner;

        public IReadOnlyCollection<string> UnlockedIds => inner.UnlockedIds;

        public int UnlockedCount => inner.UnlockedCount;

        public bool IsUnlocked(string endingId)
        {
            return inner.IsUnlocked(endingId);
        }

        public bool TryUnlock(string endingId)
        {
            return inner.TryUnlock(endingId);
        }

        public void LoadFrom(IEnumerable<string> ids)
        {
            inner.LoadFrom(ids);
        }

        public void Clear()
        {
            inner.Clear();
        }
    }
}
