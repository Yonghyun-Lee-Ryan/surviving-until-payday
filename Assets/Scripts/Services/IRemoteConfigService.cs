using System;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// 원격/로컬 설정 조회. 실패해도 게임 진행을 막지 않는다.
    /// </summary>
    public interface IRemoteConfigService
    {
        bool IsFetched { get; }

        /// <summary>비동기 fetch. 완료 콜백은 성공/실패와 무관하게 호출된다.</summary>
        void FetchAndActivate(Action<bool> onCompleted);

        int GetInt(string key, int defaultValue);

        float GetFloat(string key, float defaultValue);

        bool GetBool(string key, bool defaultValue);

        string GetString(string key, string defaultValue);
    }
}
