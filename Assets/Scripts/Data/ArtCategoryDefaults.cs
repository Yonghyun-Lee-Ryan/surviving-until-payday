using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 사건 카테고리 → 기본 배경 매핑.
    /// </summary>
    public static class ArtCategoryDefaults
    {
        public static BackgroundId BackgroundFor(EventCategory category)
        {
            switch (category)
            {
                case EventCategory.Work:
                case EventCategory.Opportunity:
                    return BackgroundId.Office;
                case EventCategory.Health:
                    return BackgroundId.Hospital;
                case EventCategory.Consumption:
                    return BackgroundId.Restaurant;
                case EventCategory.Relationship:
                case EventCategory.Rest:
                    return BackgroundId.Home;
                case EventCategory.Accident:
                    return BackgroundId.Subway;
                case EventCategory.FixedExpense:
                    return BackgroundId.Home;
                case EventCategory.Special:
                    return BackgroundId.Spare1;
                default:
                    return BackgroundId.Office;
            }
        }

        public static string BackgroundPlaceholderLabel(BackgroundId id)
        {
            switch (id)
            {
                case BackgroundId.Home:
                    return "배경: 집";
                case BackgroundId.Office:
                    return "배경: 회사";
                case BackgroundId.Subway:
                    return "배경: 지하철";
                case BackgroundId.Restaurant:
                    return "배경: 식당";
                case BackgroundId.Hospital:
                    return "배경: 병원";
                case BackgroundId.Spare1:
                    return "배경: 예비1";
                case BackgroundId.Spare2:
                    return "배경: 예비2";
                case BackgroundId.Spare3:
                    return "배경: 예비3";
                default:
                    return "배경: Placeholder";
            }
        }

        public static string ExpressionPlaceholderLabel(ExpressionId id)
        {
            switch (id)
            {
                case ExpressionId.Happy:
                    return "표정: 행복";
                case ExpressionId.Surprised:
                    return "표정: 당황";
                case ExpressionId.Angry:
                    return "표정: 분노";
                case ExpressionId.Tired:
                    return "표정: 피곤";
                case ExpressionId.Despair:
                    return "표정: 절망";
                default:
                    return "표정: 기본";
            }
        }

        public static Color BackgroundPlaceholderColor(BackgroundId id)
        {
            switch (id)
            {
                case BackgroundId.Home:
                    return new Color(0.86f, 0.82f, 0.76f, 1f);
                case BackgroundId.Office:
                    return new Color(0.72f, 0.78f, 0.86f, 1f);
                case BackgroundId.Subway:
                    return new Color(0.68f, 0.70f, 0.74f, 1f);
                case BackgroundId.Restaurant:
                    return new Color(0.90f, 0.78f, 0.70f, 1f);
                case BackgroundId.Hospital:
                    return new Color(0.78f, 0.88f, 0.86f, 1f);
                case BackgroundId.Spare1:
                    return new Color(0.80f, 0.76f, 0.86f, 1f);
                case BackgroundId.Spare2:
                    return new Color(0.76f, 0.86f, 0.80f, 1f);
                case BackgroundId.Spare3:
                    return new Color(0.86f, 0.76f, 0.80f, 1f);
                default:
                    return new Color(0.78f, 0.82f, 0.86f, 1f);
            }
        }
    }
}
