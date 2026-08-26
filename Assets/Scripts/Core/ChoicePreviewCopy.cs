using System.Text;
using SurviveUntilPayday.Data;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 선택지 경향 미리보기. 정답을 알려주는 공략이 아니라, 확정 효과의 방향(↑↓)만 보여 준다.
    /// 확률 분기는 「운 요소」로만 표시한다. 설정 「선택 미리보기」로 끌 수 있다.
    /// </summary>
    public static class ChoicePreviewCopy
    {
        public static string FormatTrend(EventChoiceData choice)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var effects = choice.FixedEffects;
            if (effects != null)
            {
                for (var i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (effect == null || effect.Value == 0)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(StatCopy.GetDisplayName(effect.StatType));
                    builder.Append(effect.Value > 0 ? '↑' : '↓');
                }
            }

            var hasRandom = choice.RandomOutcomes != null && choice.RandomOutcomes.Count > 0;
            if (builder.Length == 0)
            {
                return hasRandom ? "결과 불확실" : string.Empty;
            }

            if (hasRandom)
            {
                builder.Append(" · 운 요소");
            }

            return builder.ToString();
        }

        public static string CombineLabel(string choiceText, string trend, bool showPreview)
        {
            if (string.IsNullOrEmpty(choiceText))
            {
                return string.Empty;
            }

            if (!showPreview || string.IsNullOrEmpty(trend))
            {
                return choiceText;
            }

            return choiceText + "\n(" + trend + ")";
        }
    }
}
