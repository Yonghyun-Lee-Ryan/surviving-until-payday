using System;
using System.Collections.Generic;
using SurviveUntilPayday.Analytics;
using UnityEngine;

#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Firebase Analytics. FIREBASE_ANALYTICS가 없으면 Debug로 폴백한다.
    /// </summary>
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsService fallback;
        private readonly bool sdkAvailable;

        public FirebaseAnalyticsService(IAnalyticsService fallback = null)
        {
            this.fallback = fallback;
#if FIREBASE_ANALYTICS
            sdkAvailable = true;
            Debug.Log("[FirebaseAnalyticsService] FIREBASE_ANALYTICS enabled.");
#else
            sdkAvailable = false;
            Debug.Log(
                "[FirebaseAnalyticsService] FIREBASE_ANALYTICS 미정의. " +
                "Fallback analytics를 사용합니다. Firebase Unity SDK 추가 후 심볼을 켜세요.");
#endif
        }

        public bool IsSdkAvailable => sdkAvailable;

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            try
            {
#if FIREBASE_ANALYTICS
                if (parameters == null || parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                }
                else
                {
                    var list = new List<Parameter>(parameters.Count);
                    foreach (var pair in parameters)
                    {
                        if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                        {
                            continue;
                        }

                        switch (pair.Value)
                        {
                            case long l:
                                list.Add(new Parameter(pair.Key, l));
                                break;
                            case int i:
                                list.Add(new Parameter(pair.Key, i));
                                break;
                            case double d:
                                list.Add(new Parameter(pair.Key, d));
                                break;
                            case float f:
                                list.Add(new Parameter(pair.Key, f));
                                break;
                            default:
                                list.Add(new Parameter(pair.Key, pair.Value.ToString()));
                                break;
                        }
                    }

                    FirebaseAnalytics.LogEvent(eventName, list.ToArray());
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAnalyticsService] LogEvent failed: {ex.Message}");
            }

            fallback?.LogEvent(eventName, parameters);
        }
    }
}
