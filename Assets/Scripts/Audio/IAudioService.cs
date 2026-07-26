namespace SurviveUntilPayday.Audio
{
    public enum SfxId
    {
        Click = 0,
        CashGain = 1,
        CashLoss = 2,
        StressUp = 3,
        Success = 4,
        Fail = 5,
        Payday = 6
    }

    public enum BgmId
    {
        None = 0,
        Main = 1,
        Play = 2,
        Crisis = 3,
        Result = 4
    }

    /// <summary>
    /// 사운드 추상화. 클립 미할당·뮤트 시 no-op.
    /// </summary>
    public interface IAudioService
    {
        void PlaySfx(SfxId id);

        void SetBgm(BgmId id);

        void StopBgm();

        void ApplySettings(bool soundEnabled, float bgmVolume, float sfxVolume);
    }
}
