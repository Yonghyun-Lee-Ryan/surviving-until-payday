using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 업적 표시 데이터 조회. Resources/Achievements SO가 있으면 우선, 없으면 AchievementIds.
    /// </summary>
    public static class AchievementCatalog
    {
        private static Dictionary<string, AchievementData> cached;

        public static void InvalidateCache()
        {
            cached = null;
        }

        public static AchievementDefinition Get(string id)
        {
            EnsureCache();
            if (!string.IsNullOrEmpty(id) && cached != null && cached.TryGetValue(id, out var data) && data != null)
            {
                return new AchievementDefinition(
                    data.Id,
                    data.Title,
                    data.Description);
            }

            for (var i = 0; i < AchievementIds.Catalog.Count; i++)
            {
                if (AchievementIds.Catalog[i].Id == id)
                {
                    return AchievementIds.Catalog[i];
                }
            }

            return new AchievementDefinition(id ?? string.Empty, id ?? string.Empty, string.Empty);
        }

        public static int ResourceCount
        {
            get
            {
                EnsureCache();
                return cached != null ? cached.Count : 0;
            }
        }

        private static void EnsureCache()
        {
            if (cached != null)
            {
                return;
            }

            cached = new Dictionary<string, AchievementData>();
            var loaded = Resources.LoadAll<AchievementData>("Achievements");
            if (loaded == null)
            {
                return;
            }

            for (var i = 0; i < loaded.Length; i++)
            {
                var data = loaded[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                cached[data.Id] = data;
            }
        }
    }
}
