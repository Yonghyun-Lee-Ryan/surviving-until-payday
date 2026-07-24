using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 한글 UI 폰트. Arial/LegacyRuntime은 한글이 보이지 않으므로 절대 최종 폴백으로 쓰지 않는다.
    /// </summary>
    public static class UiFont
    {
        private static Font cachedRegular;
        private static Font cachedBold;
        private static bool loadAttempted;

        public static Font Regular
        {
            get
            {
                EnsureLoaded();
                return cachedRegular;
            }
        }

        public static Font Bold
        {
            get
            {
                EnsureLoaded();
                return cachedBold != null ? cachedBold : cachedRegular;
            }
        }

        public static void Apply(Text text, bool bold = false)
        {
            if (text == null)
            {
                return;
            }

            var font = bold ? Bold : Regular;
            if (font != null)
            {
                text.font = font;
            }

            // 한글 메트릭이 커서 Truncate면 한 줄이 통째로 사라질 수 있다.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static void EnsureLoaded()
        {
            if (loadAttempted && cachedRegular != null)
            {
                return;
            }

            loadAttempted = true;

            cachedRegular = Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                            ?? Resources.Load<Font>("Fonts/NotoSansKR-Bold");
            cachedBold = Resources.Load<Font>("Fonts/NotoSansKR-Bold")
                         ?? cachedRegular;

            if (cachedRegular == null)
            {
                cachedRegular = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "Malgun Gothic",
                        "맑은 고딕",
                        "Apple SD Gothic Neo",
                        "Noto Sans CJK KR",
                        "NanumGothic",
                        "Noto Sans KR"
                    },
                    32);
                cachedBold = cachedRegular;
            }

            if (cachedRegular == null)
            {
                Debug.LogError(
                    "[UiFont] 한글 폰트를 불러오지 못했습니다. Resources/Fonts/NotoSansKR-Regular.otf 를 확인하세요.");
                cachedRegular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }
    }
}
