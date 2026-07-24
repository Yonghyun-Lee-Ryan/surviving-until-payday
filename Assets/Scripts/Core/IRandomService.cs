namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// UnityEngine.Random에 의존하지 않는 난수 추상화.
    /// </summary>
    public interface IRandomService
    {
        int Seed { get; }

        /// <summary> [minInclusive, maxExclusive) </summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary> [0, maxExclusive) </summary>
        int Next(int maxExclusive);

        /// <summary> [0.0, 1.0) </summary>
        float NextFloat();
    }
}
