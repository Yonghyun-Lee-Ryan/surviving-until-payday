namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 접근성·오프라인 안내 (R-QA-09). 저사양에서도 본편은 로컬로 진행된다.
    /// </summary>
    public static class AccessibilityCopy
    {
        public const string OfflineNote =
            "오프라인에서도 본편(사건·저장·이어하기)은 진행됩니다. 광고와 개인정보 링크만 네트워크가 필요합니다.";

        public const string BgmLabel = "배경음";
        public const string SfxLabel = "효과음";
        public const string CreditsButton = "크레딧·라이선스";
        public const string ChoicePreviewToggle = "선택 미리보기";

        public const int MinBodyFontSize = 20;
        public const float MinTapHeight = 56f;
    }
}
