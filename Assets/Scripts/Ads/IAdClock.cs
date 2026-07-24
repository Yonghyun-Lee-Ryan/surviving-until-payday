using System;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 테스트·런타임 공용 시각. Unity Time에 의존하지 않는다.
    /// </summary>
    public interface IAdClock
    {
        double UtcSeconds { get; }
    }

    public sealed class SystemAdClock : IAdClock
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public double UtcSeconds => (DateTime.UtcNow - Epoch).TotalSeconds;
    }

    public sealed class ManualAdClock : IAdClock
    {
        public double UtcSeconds { get; set; }
    }
}
