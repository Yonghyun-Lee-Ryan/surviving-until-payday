using System;
using System.Collections.Generic;
using SurviveUntilPayday.Analytics;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Firebase Analytics 연동 지점. 패키지/심볼이 없으면 Debug로 폴백한다.
    /// 정의 심볼: FIREBASE_ANALYTICS (Firebase SDK import 후)
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

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

#if FIREBASE_ANALYTICS
            try
            {
                // Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, ConvertParams(parameters));
                // 패키지 설치 후 위 호출을 활성화한다.
                fallback?.LogEvent(eventName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAnalyticsService] LogEvent failed: {ex.Message}");
                fallback?.LogEvent(eventName, parameters);
            }
#else
            fallback?.LogEvent(eventName, parameters);
#endif
        }

        public bool IsSdkAvailable => sdkAvailable;
    }
}
