namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// GameState 불변 스냅샷. 원본과 공유하지 않는 복사본이다.
    /// </summary>
    public sealed class GameStateSnapshot
    {
        public int CurrentDay { get; }
        public string JobId { get; }
        public string TraitId { get; }
        public long Salary { get; }
        public int RandomSeed { get; }
        public PlayerStats Stats { get; }

        public GameStateSnapshot(
            int currentDay,
            string jobId,
            string traitId,
            long salary,
            int randomSeed,
            PlayerStats stats)
        {
            CurrentDay = currentDay;
            JobId = jobId ?? string.Empty;
            TraitId = traitId ?? string.Empty;
            Salary = salary;
            RandomSeed = randomSeed;
            Stats = stats != null ? stats.Clone() : new PlayerStats();
        }
    }
}
