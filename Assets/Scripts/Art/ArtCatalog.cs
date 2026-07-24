using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Art
{
    /// <summary>
    /// 배경·표정 스프라이트 카탈로그. 슬롯이 null이어도 안전하다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArtCatalog",
        menuName = "Survive Until Payday/Art/Art Catalog",
        order = 50)]
    public sealed class ArtCatalog : ScriptableObject
    {
        [Header("Backgrounds (index = BackgroundId)")]
        [SerializeField] private Sprite[] backgrounds = new Sprite[8];

        [Header("Expressions (index = ExpressionId)")]
        [SerializeField] private Sprite[] expressions = new Sprite[6];

        public Sprite GetBackground(BackgroundId id)
        {
            return GetSlot(backgrounds, (int)id);
        }

        public Sprite GetExpression(ExpressionId id)
        {
            return GetSlot(expressions, (int)id);
        }

        private static Sprite GetSlot(Sprite[] slots, int index)
        {
            if (slots == null || index < 0 || index >= slots.Length)
            {
                return null;
            }

            return slots[index];
        }

#if UNITY_EDITOR
        public void EditorEnsureSlotSizes()
        {
            if (backgrounds == null || backgrounds.Length != 8)
            {
                var next = new Sprite[8];
                if (backgrounds != null)
                {
                    for (var i = 0; i < backgrounds.Length && i < next.Length; i++)
                    {
                        next[i] = backgrounds[i];
                    }
                }

                backgrounds = next;
            }

            if (expressions == null || expressions.Length != 6)
            {
                var next = new Sprite[6];
                if (expressions != null)
                {
                    for (var i = 0; i < expressions.Length && i < next.Length; i++)
                    {
                        next[i] = expressions[i];
                    }
                }

                expressions = next;
            }
        }
#endif
    }
}
