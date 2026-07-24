using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SurviveUntilPayday.Analytics
{
    /// <summary>
    /// Editor/개발용. Console에 snake_case 이벤트를 출력한다.
    /// </summary>
    public sealed class DebugAnalyticsService : IAnalyticsService
    {
        public int EventCount { get; private set; }

        public readonly List<(string Name, IReadOnlyDictionary<string, object> Parameters)> History =
            new List<(string, IReadOnlyDictionary<string, object>)>();

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("[DebugAnalytics] eventName is empty.");
                return;
            }

            EventCount++;
            var copy = CopyParameters(parameters);
            History.Add((eventName, copy));
            Debug.Log($"[Analytics] {eventName} {FormatParameters(copy)}");
        }

        public void ClearHistory()
        {
            History.Clear();
            EventCount = 0;
        }

        private static IReadOnlyDictionary<string, object> CopyParameters(
            IReadOnlyDictionary<string, object> parameters)
        {
            var dict = new Dictionary<string, object>();
            if (parameters == null)
            {
                return dict;
            }

            foreach (var pair in parameters)
            {
                if (string.IsNullOrEmpty(pair.Key))
                {
                    continue;
                }

                dict[pair.Key] = pair.Value;
            }

            return dict;
        }

        private static string FormatParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "{}";
            }

            var builder = new StringBuilder();
            builder.Append('{');
            var first = true;
            foreach (var pair in parameters)
            {
                if (!first)
                {
                    builder.Append(", ");
                }

                first = false;
                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value != null ? pair.Value.ToString() : "null");
            }

            builder.Append('}');
            return builder.ToString();
        }
    }
}
