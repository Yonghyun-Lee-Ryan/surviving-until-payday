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

        /// <summary>결과 화면에서 경험치 2배 광고를 이미 수령했는지.</summary>
        public bool DoubleExperienceClaimedForLastResult { get; set; }

        public int TraitFragmentCount { get; set; }

        public int TotalExperience => Meta.TotalExperience;

        public GameSession()
        {
            EndingCodex = new EndingCodex(Meta.Endings);
        }

        public bool HasActiveRun =>
            CachedSave?.run != null && CachedSave.run.hasActiveRun;

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
        }
    }
}
