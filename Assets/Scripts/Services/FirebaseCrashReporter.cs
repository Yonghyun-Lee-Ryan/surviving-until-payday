using System;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Crashlytics 연동 지점. 심볼 FIREBASE_CRASHLYTICS 없으면 DebugCrashReporter로 폴백.
    /// </summary>
    public sealed class FirebaseCrashReporter : ICrashReporter
    {
        private readonly ICrashReporter fallback;
        private bool initialized;

        public FirebaseCrashReporter(ICrashReporter fallback = null)
        {
            this.fallback = fallback ?? new DebugCrashReporter();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
#if FIREBASE_CRASHLYTICS
            // Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
            Debug.Log("[FirebaseCrashReporter] FIREBASE_CRASHLYTICS enabled.");
#else
            Debug.Log(
                "[FirebaseCrashReporter] FIREBASE_CRASHLYTICS 미정의. DebugCrashReporter로 폴백합니다.");
#endif
            fallback.Initialize();
        }

        public void Log(string message)
        {
#if FIREBASE_CRASHLYTICS
            // Firebase.Crashlytics.Crashlytics.Log(message);
#endif
            fallback.Log(message);
        }

        public void RecordException(Exception exception)
        {
#if FIREBASE_CRASHLYTICS
            // Firebase.Crashlytics.Crashlytics.LogException(exception);
#endif
            fallback.RecordException(exception);
        }

        public void SetCustomKey(string key, string value)
        {
#if FIREBASE_CRASHLYTICS
            // Firebase.Crashlytics.Crashlytics.SetCustomKey(key, value ?? string.Empty);
#endif
            fallback.SetCustomKey(key, value);
        }
    }
}
