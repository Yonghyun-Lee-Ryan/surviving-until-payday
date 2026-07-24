using System;
using System.Collections.Generic;
using SurviveUntilPayday.Analytics;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// 여러 Analytics 구현에 동일 이벤트를 전달한다.
    /// </summary>
    public sealed class CompositeAnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsService[] services;

        public CompositeAnalyticsService(params IAnalyticsService[] services)
        {
            this.services = services ?? Array.Empty<IAnalyticsService>();
        }

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            for (var i = 0; i < services.Length; i++)
            {
                services[i]?.LogEvent(eventName, parameters);
            }
        }
    }
}
