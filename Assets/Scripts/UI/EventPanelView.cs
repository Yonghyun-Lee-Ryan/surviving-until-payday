using System.Collections;
using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 중앙 사건 카드. 배경·표정 스프라이트가 없으면 카테고리별 Placeholder를 표시한다.
    /// </summary>
    public sealed class EventPanelView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text backgroundPlaceholderLabel;
        [SerializeField] private Image expressionImage;
        [SerializeField] private Text expressionPlaceholderLabel;

        [SerializeField] private Image illustrationImage;
        [SerializeField] private Text placeholderLabel;

        private Coroutine shakeRoutine;
        private Coroutine fadeRoutine;
        private Coroutine punchRoutine;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Show(
            string title,
            string description,
            BackgroundId backgroundId,
            Sprite backgroundSprite,
            ExpressionId expressionId,
            Sprite expressionSprite)
        {
            if (titleLabel != null)
            {
                // 두번째 사진 스타일: 제목 숨기고 설명 카드만 사용
                titleLabel.gameObject.SetActive(false);
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.gameObject.SetActive(true);
                descriptionLabel.text = description ?? string.Empty;
                UiFont.Apply(descriptionLabel);
                descriptionLabel.fontSize = 34;
                descriptionLabel.alignment = TextAnchor.MiddleCenter;
                descriptionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionLabel.verticalOverflow = VerticalWrapMode.Truncate;
                descriptionLabel.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            }

            ApplyBackground(backgroundId, backgroundSprite);
            // 표정 초상화는 사용하지 않는다(상황 이미지를 크게 보여 줌).
            HideExpression();
            PlayFadeIn();
        }

        /// <summary>하위 호환: 단일 illustration만 있을 때.</summary>
        public void Show(string title, string description, Sprite illustration)
        {
            Show(
                title,
                description,
                BackgroundId.Office,
                illustration,
                ExpressionId.Default,
                null);
        }

        public void SetExpression(ExpressionId expressionId, Sprite expressionSprite, bool shake = true)
        {
            // 요청: 남자 초상화 제거 — 표정 슬롯은 항상 숨김.
            HideExpression();
        }

        private void HideExpression()
        {
            if (expressionImage != null)
            {
                expressionImage.enabled = false;
                expressionImage.sprite = null;
                expressionImage.color = new Color(1f, 1f, 1f, 0f);
                expressionImage.gameObject.SetActive(false);
            }

            if (expressionPlaceholderLabel != null)
            {
                expressionPlaceholderLabel.gameObject.SetActive(false);
            }
        }

        public void Bind(
            Text title,
            Text description,
            Image background,
            Text backgroundPlaceholder,
            Image expression,
            Text expressionPlaceholder)
        {
            titleLabel = title;
            descriptionLabel = description;
            backgroundImage = background;
            backgroundPlaceholderLabel = backgroundPlaceholder;
            expressionImage = expression;
            expressionPlaceholderLabel = expressionPlaceholder;
        }

        public void Bind(Text title, Text description, Image illustration, Text placeholder)
        {
            titleLabel = title;
            descriptionLabel = description;
            illustrationImage = illustration;
            placeholderLabel = placeholder;
            backgroundImage = illustration;
            backgroundPlaceholderLabel = placeholder;
        }

        private void ApplyBackground(BackgroundId backgroundId, Sprite sprite)
        {
            var target = backgroundImage != null ? backgroundImage : illustrationImage;
            var label = backgroundPlaceholderLabel != null ? backgroundPlaceholderLabel : placeholderLabel;
            var hasSprite = sprite != null;

            if (target != null)
            {
                target.sprite = sprite;
                target.enabled = true;
                target.preserveAspect = true;
                target.color = hasSprite
                    ? Color.white
                    : ArtCategoryDefaults.BackgroundPlaceholderColor(backgroundId);
                target.raycastTarget = false;
                // 이미지가 설명 카드보다 뒤에, 패널 안에서 크게
                target.transform.SetAsFirstSibling();
            }

            // 실에셋이 있으면 플레이스홀더 라벨(네모 위 글자)은 숨긴다.
            if (label != null)
            {
                label.gameObject.SetActive(!hasSprite);
                if (!hasSprite)
                {
                    label.text = ArtCategoryDefaults.BackgroundPlaceholderLabel(backgroundId);
                }
            }
        }

        private void ApplyExpression(ExpressionId expressionId, Sprite sprite, bool shake)
        {
            var hasSprite = sprite != null;
            if (expressionImage != null)
            {
                expressionImage.sprite = sprite;
                expressionImage.preserveAspect = true;
                expressionImage.raycastTarget = false;
                // 스프라이트 없으면 베이지 네모를 아예 끈다.
                expressionImage.enabled = hasSprite;
                expressionImage.color = hasSprite ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            if (expressionPlaceholderLabel != null)
            {
                expressionPlaceholderLabel.gameObject.SetActive(false);
            }

            if (shake && hasSprite)
            {
                PlayWeakShake();
                PlayPunch();
            }
        }

        private void PlayFadeIn()
        {
            if (canvasGroup == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            const float duration = 0.22f;
            var elapsed = 0f;
            canvasGroup.alpha = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0.35f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeRoutine = null;
        }

        private void PlayPunch()
        {
            if (expressionImage == null)
            {
                return;
            }

            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
            }

            punchRoutine = StartCoroutine(PunchRoutine(expressionImage.rectTransform));
        }

        private IEnumerator PunchRoutine(RectTransform target)
        {
            var origin = target.localScale;
            const float duration = 0.2f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = elapsed / duration;
                var scale = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
                target.localScale = origin * scale;
                yield return null;
            }

            target.localScale = origin;
            punchRoutine = null;
        }

        private void PlayWeakShake()
        {
            var target = expressionImage != null
                ? expressionImage.rectTransform
                : (backgroundImage != null ? backgroundImage.rectTransform : null);
            if (target == null)
            {
                return;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ShakeRoutine(target));
        }

        private IEnumerator ShakeRoutine(RectTransform target)
        {
            var origin = target.anchoredPosition;
            const float duration = 0.18f;
            const float amplitude = 6f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = elapsed / duration;
                var offset = (1f - t) * amplitude * Mathf.Sin(elapsed * 55f);
                target.anchoredPosition = origin + new Vector2(offset, 0f);
                yield return null;
            }

            target.anchoredPosition = origin;
            shakeRoutine = null;
        }
    }
}
