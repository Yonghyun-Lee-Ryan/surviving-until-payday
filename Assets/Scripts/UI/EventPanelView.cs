using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 중앙 사건 카드. 이미지가 없으면 Placeholder만 표시한다.
    /// </summary>
    public sealed class EventPanelView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Text placeholderLabel;

        public void Show(string title, string description, Sprite illustration)
        {
            if (titleLabel != null)
            {
                titleLabel.text = title ?? string.Empty;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = description ?? string.Empty;
            }

            var hasSprite = illustration != null;
            if (illustrationImage != null)
            {
                illustrationImage.sprite = illustration;
                illustrationImage.enabled = hasSprite;
                if (!hasSprite)
                {
                    illustrationImage.color = new Color(0.78f, 0.82f, 0.86f, 1f);
                }
                else
                {
                    illustrationImage.color = Color.white;
                }
            }

            if (placeholderLabel != null)
            {
                placeholderLabel.gameObject.SetActive(!hasSprite);
                placeholderLabel.text = "사건 이미지 (Placeholder)";
            }
        }

        public void Bind(Text title, Text description, Image illustration, Text placeholder)
        {
            titleLabel = title;
            descriptionLabel = description;
            illustrationImage = illustration;
            placeholderLabel = placeholder;
        }
    }
}
