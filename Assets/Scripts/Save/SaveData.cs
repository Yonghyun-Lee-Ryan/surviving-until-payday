using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Save
{
    public static class SaveVersion
    {
        public const int Current = 2;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = SaveVersion.Current;
        public RunSaveData run = new RunSaveData();
        public MetaSaveData meta = new MetaSaveData();
    }

    [Serializable]
    public sealed class RunSaveData
    {
        public bool hasActiveRun;
        public int currentDay = 1;
        public string jobId = string.Empty;
        public string traitId = string.Empty;
        public long salary;
        public int randomSeed = 1;
        public int consumedRandomCalls;
        public long cash;
        public int health = 80;
        public int stress = 20;
        public int happiness = 50;
        public int companyScore = 50;
        public List<string> recentEventIds = new List<string>();
        public string lastSelectedEventId = string.Empty;
        public string pendingEventId = string.Empty;
    }

    [Serializable]
    public sealed class MetaSaveData
    {
        public List<string> unlockedEndingIds = new List<string>();
        public List<string> unlockedEventIds = new List<string>();
        public List<string> unlockedTraitIds = new List<string>();
        public List<string> unlockedAchievementIds = new List<string>();
        public int totalExperience;
    }
}
