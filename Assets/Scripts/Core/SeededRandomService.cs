using System;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// System.Random 기반 시드 난수. 동일 시드면 동일 수열을 만든다.
    /// </summary>
    public sealed class SeededRandomService : IRandomService
    {
        private Random random;

        public int Seed { get; private set; }
        public int ConsumedCount { get; private set; }

        public SeededRandomService(int seed)
        {
            Reseed(seed);
        }

        public void Reseed(int seed)
        {
            Seed = seed;
            ConsumedCount = 0;
            random = new Random(seed);
        }

        public void FastForward(int callCount)
        {
            if (callCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(callCount));
            }

            for (var i = 0; i < callCount; i++)
            {
                // NextDouble consumes one internal sample, matching NextFloat path roughly.
                // Use Next(int.MaxValue) so ConsumedCount increments via Next.
                Next(int.MaxValue);
            }
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "maxExclusive must be > 0.");
            }

            ConsumedCount++;
            return random.Next(maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    maxExclusive,
                    "maxExclusive must be greater than minInclusive.");
            }

            ConsumedCount++;
            return random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            ConsumedCount++;
            return (float)random.NextDouble();
        }
    }
}
