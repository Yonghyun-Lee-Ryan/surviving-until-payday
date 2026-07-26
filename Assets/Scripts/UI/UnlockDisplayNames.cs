using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 결과/도감 UI용 id → 표시명 해석.
    /// </summary>
    public static class UnlockDisplayNames
    {
        private static Dictionary<string, string> eventTitles;
        private static Dictionary<string, string> traitNames;
        private static Dictionary<string, string> jobNames;
        private static bool loaded;

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            eventTitles = new Dictionary<string, string>();
            traitNames = new Dictionary<string, string>();
            jobNames = new Dictionary<string, string>();

            var events = Resources.FindObjectsOfTypeAll<EventData>();
            for (var i = 0; i < events.Length; i++)
            {
                var data = events[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                eventTitles[data.Id] = string.IsNullOrWhiteSpace(data.Title) ? data.Id : data.Title;
            }

            var traits = Resources.FindObjectsOfTypeAll<TraitData>();
            for (var i = 0; i < traits.Length; i++)
            {
                var data = traits[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                traitNames[data.Id] = string.IsNullOrWhiteSpace(data.DisplayName) ? data.Id : data.DisplayName;
            }

            var jobs = Resources.FindObjectsOfTypeAll<JobData>();
            for (var i = 0; i < jobs.Length; i++)
            {
                var data = jobs[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                jobNames[data.Id] = string.IsNullOrWhiteSpace(data.DisplayName) ? data.Id : data.DisplayName;
            }

            loaded = true;
        }

        public static string EventTitle(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return eventTitles.TryGetValue(id, out var title) ? title : HumanizeId(id);
        }

        public static string TraitName(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return traitNames.TryGetValue(id, out var name) ? name : HumanizeId(id);
        }

        public static string JobName(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return jobNames.TryGetValue(id, out var name) ? name : HumanizeId(id);
        }

        public static List<string> MapEventTitles(IList<string> ids)
        {
            var list = new List<string>();
            if (ids == null)
            {
                return list;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var title = EventTitle(ids[i]);
                if (!string.IsNullOrEmpty(title))
                {
                    list.Add(title);
                }
            }

            return list;
        }

        public static List<string> MapTraitNames(IList<string> ids)
        {
            var list = new List<string>();
            if (ids == null)
            {
                return list;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var name = TraitName(ids[i]);
                if (!string.IsNullOrEmpty(name))
                {
                    list.Add(name);
                }
            }

            return list;
        }

        public static List<string> MapJobNames(IList<string> ids)
        {
            var list = new List<string>();
            if (ids == null)
            {
                return list;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var name = JobName(ids[i]);
                if (!string.IsNullOrEmpty(name))
                {
                    list.Add(name);
                }
            }

            return list;
        }

        private static string HumanizeId(string id)
        {
            // trait_overtime_pro → overtime pro (fallback only)
            var trimmed = id;
            if (trimmed.StartsWith("trait_"))
            {
                trimmed = trimmed.Substring(6);
            }
            else if (trimmed.StartsWith("event_"))
            {
                trimmed = trimmed.Substring(6);
            }
            else if (trimmed.StartsWith("job_"))
            {
                trimmed = trimmed.Substring(4);
            }

            return trimmed.Replace('_', ' ');
        }
    }
}
