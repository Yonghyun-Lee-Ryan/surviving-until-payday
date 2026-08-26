using System;
using UnityEngine;

#if FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Crashlytics 연동. FIREBASE_CRASHLYTICS가 없으면 DebugCrashReporter로 폴백.
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
            try
            {
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                Debug.Log("[FirebaseCrashReporter] FIREBASE_CRASHLYTICS enabled.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseCrashReporter] Initialize failed: {ex.Message}");
            }
#else
            Debug.Log(
                "[FirebaseCrashReporter] FIREBASE_CRASHLYTICS 미정의. DebugCrashReporter로 폴백합니다.");
#endif
            fallback.Initialize();
        }

        public void Log(string message)
        {
#if FIREBASE_CRASHLYTICS
            try
            {
                Crashlytics.Log(message ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseCrashReporter] Log failed: {ex.Message}");
            }
#endif
            fallback.Log(message);
        }

        public void RecordException(Exception exception)
        {
#if FIREBASE_CRASHLYTICS
            try
            {
                if (exception != null)
                {
                    Crashlytics.LogException(exception);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseCrashReporter] RecordException failed: {ex.Message}");
            }
#endif
            fallback.RecordException(exception);
        }

        public void SetCustomKey(string key, string value)
        {
#if FIREBASE_CRASHLYTICS
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    Crashlytics.SetCustomKey(key, value ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseCrashReporter] SetCustomKey failed: {ex.Message}");
            }
#endif
            fallback.SetCustomKey(key, value);
        }
    }
}
