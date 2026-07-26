using System.Collections.Generic;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.DebugTools
{
    /// <summary>
    /// GamePlayPresenter가 노출하는 디버그 조작 API.
    /// UI(DebugPanel)는 이 인터페이스만 통해 상태를 변경한다.
    /// </summary>
    public interface IGameDebugAccess
    {
        GameState DebugGetState();
        void DebugSetDay(int day);
        void DebugSetStats(long cash, int health, int stress, int happiness, int companyScore);
        void DebugAdjustCash(long delta);
        void DebugSetSeed(int seed);
        void DebugForceEvent(EventData eventData);
        void DebugForceEnding(EndingData ending);
        void DebugForceSuccess();
        void DebugForceFailure(FailureReason reason);
        void DebugSetFlag(string flagId, bool enabled);
        void DebugClearFlags();
        IReadOnlyList<string> DebugGetFlags();
        string DebugBuildStateDump();
    }
}
