using System.Collections.Generic;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;

namespace SurviveUntilPayday.Core
{
    public enum GameStartMode
    {
        NewRun = 0,
        ContinueRun = 1
    }

    /// <summary>
    /// 회차 간·씬 간 공유 세션 상태. AppRoot 하에서 유지한다.
    /// </summary>
    public sealed class GameSession
    {
        public ResultData LastResult { get; set; }
        public MetaProgressionManager Meta { get; } = new MetaProgressionManager();
        public EndingCodex EndingCodex { get; }
        public List<EndingData> EndingCatalog { get; } = new List<EndingData>();
        public EndingData FallbackSuccessEnding { get; set; }
        public GameStartMode StartMode { get; set; } = GameStartMode.NewRun;
        public SaveData CachedSave { get; set; }

        /// <summary>메인 메뉴에서 고른 직업. UsePendingRunSelection이 true일 때만 사용.</summary>
        public JobData PendingJob { get; private set; }

        /// <summary>메인 메뉴에서 고른 특성. null이면 특성 없이 시작.</summary>
        public TraitData PendingTrait { get; private set; }

        /// <summary>PendingJob/PendingTrait를 새 회차에 적용할지.</summary>
        public bool UsePendingRunSelection { get; private set; }

        /// <summary>메뉴에서 지정한 회차 시드. null이면 Presenter 기본값.</summary>
        public int? PendingRandomSeed { get; private set; }

        /// <summary>오늘의 직장인 회차 여부.</summary>
        public bool IsDailyChallengeRun { get; private set; }

        /// <summary>결과 화면에서 경험치 2배 광고를 이미 수령했는지.</summary>
        public bool DoubleExperienceClaimedForLastResult { get; set; }

        public int TraitFragmentCount { get; set; }

        public int TotalExperience => Meta.TotalExperience;

        /// <summary>메뉴·Game에서 공유하는 일일 미션 풀.</summary>
        public List<DailyMissionData> DailyMissionPool { get; } = new List<DailyMissionData>();

        public GameSession()
        {
            EndingCodex = new EndingCodex(Meta.Endings);
        }

        public void SetDailyMissionPool(IEnumerable<DailyMissionData> missions)
        {
            DailyMissionPool.Clear();
            if (missions == null)
            {
                return;
            }

            foreach (var mission in missions)
            {
                if (mission != null)
                {
                    DailyMissionPool.Add(mission);
                }
            }
        }

        public bool HasActiveRun =>
            CachedSave?.run != null && CachedSave.run.hasActiveRun;

        /// <summary>
        /// 새 회차용 직업·특성을 세션에 맡긴다. Game Scene이 읽어 초기화한다.
        /// </summary>
        public void SetPendingNewRun(JobData job, TraitData trait)
        {
            PendingJob = job;
            PendingTrait = trait;
            UsePendingRunSelection = true;
            PendingRandomSeed = null;
            IsDailyChallengeRun = false;
            StartMode = GameStartMode.NewRun;
        }

        /// <summary>
        /// 오늘의 직장인: 고정 시드·직업으로 새 회차를 시작한다.
        /// </summary>
        public void SetPendingDailyRun(JobData job, TraitData trait, int seed)
        {
            PendingJob = job;
            PendingTrait = trait;
            UsePendingRunSelection = true;
            PendingRandomSeed = seed;
            IsDailyChallengeRun = true;
            StartMode = GameStartMode.NewRun;
        }

        public void ClearPendingRunSelection()
        {
            PendingJob = null;
            PendingTrait = null;
            UsePendingRunSelection = false;
            PendingRandomSeed = null;
            IsDailyChallengeRun = false;
        }

        public bool TryConsumePendingRandomSeed(out int seed)
        {
            if (PendingRandomSeed.HasValue)
            {
                seed = PendingRandomSeed.Value;
                PendingRandomSeed = null;
                return true;
            }

            seed = 0;
            return false;
        }

        public bool ConsumeDailyChallengeFlag()
        {
            var value = IsDailyChallengeRun;
            IsDailyChallengeRun = false;
            return value;
        }

        public void SetEndingCatalog(IEnumerable<EndingData> endings, EndingData fallbackSuccess)
        {
            EndingCatalog.Clear();
            if (endings != null)
            {
                foreach (var ending in endings)
                {
                    if (ending != null)
                    {
                        EndingCatalog.Add(ending);
                    }
                }
            }

            FallbackSuccessEnding = fallbackSuccess;
        }

        public EndingEvaluator CreateEndingEvaluator()
        {
            return new EndingEvaluator(EndingCatalog, FallbackSuccessEnding);
        }

        public void ApplyLoadedSave(SaveData save)
        {
            CachedSave = SaveRepository.Normalize(save ?? SaveRepository.CreateDefault());
            SaveMapper.ApplyMeta(CachedSave.meta, Meta);
            TraitFragmentCount = Meta.TraitFragmentCount;
        }

        public void SyncTraitFragmentsFromMeta()
        {
            TraitFragmentCount = Meta != null ? Meta.TraitFragmentCount : 0;
        }
    }
}
