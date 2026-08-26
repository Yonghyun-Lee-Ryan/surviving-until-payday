using System;
using System.Collections.Generic;

namespace SurviveUntilPayday.Save
{
    public static class SaveVersion
    {
        public const int Current = 8;
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
        public List<string> runFlags = new List<string>();
        public List<string> queuedEventIds = new List<string>();
        public int sideJobCount;
    }

    [Serializable]
    public sealed class MetaSaveData
    {
        public List<string> unlockedEndingIds = new List<string>();
        public List<string> unlockedEventIds = new List<string>();
        public List<string> unlockedTraitIds = new List<string>();
        public List<string> unlockedJobIds = new List<string>();
        public List<string> unlockedAchievementIds = new List<string>();
        public int totalExperience;
        public int traitFragmentCount;

        // Unit 25 — 일일 콘텐츠 (로컬)
        public string dailyDateKey = string.Empty;
        public long dailyBestCash;
        public bool dailyBestSurvived;
        public int dailyBestStress = 999;
        public int dailyBestCompanyScore;
        public int dailyBestDaysSurvived;
        public bool dailyHasBestRecord;
        public List<DailyMissionSaveEntry> dailyMissions = new List<DailyMissionSaveEntry>();
        public int dailyLoginStreak = 1;
        public string dailyLastVisitDateKey = string.Empty;
        public bool dailyStreakBonusGranted;

        // Unit 26 — 첫 실행 튜토리얼
        public bool firstRunTutorialCompleted;
    }

    [Serializable]
    public sealed class DailyMissionSaveEntry
    {
        public string missionId = string.Empty;
        public bool completed;
        public bool rewardClaimed;
    }
}
