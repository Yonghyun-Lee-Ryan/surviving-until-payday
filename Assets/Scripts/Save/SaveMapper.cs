using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;

namespace SurviveUntilPayday.Save
{
    /// <summary>
    /// GameState/세션 ↔ RunSaveData/MetaSaveData 변환.
    /// </summary>
    public static class SaveMapper
    {
        public static RunSaveData CaptureRun(
            GameState state,
            SeededRandomService random,
            EventSelector selector,
            string pendingEventId)
        {
            var run = new RunSaveData
            {
                hasActiveRun = true,
                currentDay = state.CurrentDay,
                jobId = state.JobId,
                traitId = state.TraitId,
                salary = state.Salary,
                randomSeed = state.RandomSeed,
                consumedRandomCalls = random != null ? random.ConsumedCount : 0,
                cash = state.Stats.Cash,
                health = state.Stats.Health,
                stress = state.Stats.Stress,
                happiness = state.Stats.Happiness,
                companyScore = state.Stats.CompanyScore,
                lastSelectedEventId = selector != null ? selector.LastSelectedEventId ?? string.Empty : string.Empty,
                pendingEventId = pendingEventId ?? string.Empty
            };

            if (selector != null)
            {
                foreach (var id in selector.RecentEventIds)
                {
                    run.recentEventIds.Add(id);
                }
            }

            if (state.RunFlags != null)
            {
                foreach (var flag in state.RunFlags)
                {
                    run.runFlags.Add(flag);
                }
            }

            if (state.QueuedFollowUpEventIds != null)
            {
                foreach (var queued in state.QueuedFollowUpEventIds)
                {
                    run.queuedEventIds.Add(queued);
                }
            }

            return run;
        }

        public static GameState ToGameState(RunSaveData run)
        {
            var state = new GameState
            {
                CurrentDay = run.currentDay,
                JobId = run.jobId,
                TraitId = run.traitId,
                Salary = run.salary,
                RandomSeed = run.randomSeed
            };

            state.Stats.Cash = run.cash;
            state.Stats.Health = run.health;
            state.Stats.Stress = run.stress;
            state.Stats.Happiness = run.happiness;
            state.Stats.CompanyScore = run.companyScore;
            state.LoadRunFlags(run.runFlags);
            state.LoadFollowUpQueue(run.queuedEventIds);
            return state;
        }

        public static void ApplyMeta(MetaSaveData meta, MetaProgressionManager progression)
        {
            if (progression == null)
            {
                return;
            }

            meta ??= new MetaSaveData();
            progression.Load(
                meta.totalExperience,
                meta.unlockedEndingIds,
                meta.unlockedEventIds,
                meta.unlockedTraitIds,
                meta.unlockedAchievementIds,
                meta.unlockedJobIds,
                meta.traitFragmentCount);
        }

        public static MetaSaveData CaptureMeta(MetaProgressionManager progression)
        {
            var meta = new MetaSaveData
            {
                totalExperience = progression != null ? progression.TotalExperience : 0,
                traitFragmentCount = progression != null ? progression.TraitFragmentCount : 0
            };

            if (progression == null)
            {
                return meta;
            }

            CopyIds(progression.Endings, meta.unlockedEndingIds);
            CopyIds(progression.Events, meta.unlockedEventIds);
            CopyIds(progression.Traits, meta.unlockedTraitIds);
            CopyIds(progression.Jobs, meta.unlockedJobIds);
            CopyIds(progression.Achievements, meta.unlockedAchievementIds);
            return meta;
        }

        private static void CopyIds(UnlockCodex codex, System.Collections.Generic.List<string> target)
        {
            if (codex == null || target == null)
            {
                return;
            }

            foreach (var id in codex.UnlockedIds)
            {
                target.Add(id);
            }
        }
    }
}
