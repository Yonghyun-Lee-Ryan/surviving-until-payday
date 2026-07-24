using System.Collections.Generic;

namespace SurviveUntilPayday.Analytics
{
    /// <summary>
    /// 분석 SDK 추상화. 게임 로직은 Firebase 등을 직접 참조하지 않는다.
    /// </summary>
    public interface IAnalyticsService
    {
        void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null);
    }
}
