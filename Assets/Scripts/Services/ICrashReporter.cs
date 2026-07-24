using System;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// 크래시/비치명 예외 수집 추상화.
    /// </summary>
    public interface ICrashReporter
    {
        void Initialize();

        void Log(string message);

        void RecordException(Exception exception);

        void SetCustomKey(string key, string value);
    }
}
