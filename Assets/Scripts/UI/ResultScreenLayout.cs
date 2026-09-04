using UnityEngine;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Result 화면 하단 버튼과 본문 영역의 겹침을 피하기 위한 좌표 계산.
    /// SafeArea 중앙 앵커(0,0) 기준.
    /// </summary>
    public static class ResultScreenLayout
    {
        public const float BackHeight = 110f;
        public const float ShareHeight = 90f;
        public const float DoubleXpHeight = 90f;
        public const float BottomPad = 28f;
        public const float ButtonGap = 14f;
        public const float BodyButtonGap = 20f;

        public struct ButtonStack
        {
            public float BackCenterY;
            public float ShareCenterY;
            public float DoubleXpCenterY;
            public float StackTopY;
        }

        public static ButtonStack ComputeButtonStack(float parentHeight, bool showDoubleXp)
        {
            var height = parentHeight > 100f ? parentHeight : 1920f;
            var bottom = -height * 0.5f;
            var backBottom = bottom + BottomPad;
            var backCenter = backBottom + BackHeight * 0.5f;
            var shareBottom = backBottom + BackHeight + ButtonGap;
            var shareCenter = shareBottom + ShareHeight * 0.5f;
            var stackTop = shareBottom + ShareHeight;
            var doubleXpCenter = shareCenter;

            if (showDoubleXp)
            {
                var xpBottom = shareBottom + ShareHeight + ButtonGap;
                doubleXpCenter = xpBottom + DoubleXpHeight * 0.5f;
                stackTop = xpBottom + DoubleXpHeight;
            }

            return new ButtonStack
            {
                BackCenterY = backCenter,
                ShareCenterY = shareCenter,
                DoubleXpCenterY = doubleXpCenter,
                StackTopY = stackTop
            };
        }

        public static float BodyViewportHeight(float statsBottomY, float stackTopY)
        {
            return Mathf.Max(120f, statsBottomY - BodyButtonGap - (stackTopY + BodyButtonGap));
        }
    }
}
