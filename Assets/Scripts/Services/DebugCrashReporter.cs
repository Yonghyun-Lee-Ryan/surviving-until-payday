using System;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Editor/개발용 크래시 수집. Console과 내부 카운터에 남긴다.
    /// </summary>
    public sealed class DebugCrashReporter : ICrashReporter
    {
        private bool initialized;

        public int ExceptionCount { get; private set; }
        public int LogCount { get; private set; }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Application.logMessageReceived += OnLogMessageReceived;
            Debug.Log("[DebugCrashReporter] Initialized.");
        }

        public void Log(string message)
        {
            LogCount++;
            Debug.Log($"[CrashReporter] {message}");
        }

        public void RecordException(Exception exception)
        {
            if (exception == null)
            {
                Debug.LogWarning("[CrashReporter] RecordException called with null.");
                return;
            }

            // LogException은 Test Runner/Console에 실제 실패처럼 보여 오해를 낳는다.
            // 개발용 기록은 Warning으로 남기고, 엔진이 올린 Exception/Error는 OnLogMessageReceived로 집계한다.
            ExceptionCount++;
            Debug.LogWarning(
                $"[CrashReporter] Recorded exception: {exception.GetType().Name}: {exception.Message}");
        }

        public void SetCustomKey(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            Debug.Log($"[CrashReporter] key {key}={value}");
        }

        public void Dispose()
        {
            if (!initialized)
            {
                return;
            }

            Application.logMessageReceived -= OnLogMessageReceived;
            initialized = false;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
            {
                return;
            }

            ExceptionCount++;
        }
    }
}
