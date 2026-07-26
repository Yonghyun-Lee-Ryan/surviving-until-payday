using System.Collections;
using SurviveUntilPayday.Audio;
using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 단일 능력치 게이지. GameState를 직접 알지 않는다.
    /// </summary>
    public sealed class StatGaugeView : MonoBehaviour
    {
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text valueLabel;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalFillColor = new Color(0.25f, 0.55f, 0.45f);
        [SerializeField] private Color warningFillColor = new Color(0.75f, 0.25f, 0.22f);
        [SerializeField] private Color normalBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        [SerializeField] private Color warningBackgroundColor = new Color(0.95f, 0.8f, 0.78f, 0.95f);
        [SerializeField] private int warningHighThreshold = -1;
        [SerializeField] private int warningLowThreshold = -1;
        [SerializeField] private int maxValue = 100;

        private int displayedValue;
        private Coroutine animationRoutine;
        private string helpDescription = string.Empty;
        private Button helpButton;

        public int DisplayedValue => displayedValue;

        public void ConfigureThresholds(int highWarning, int lowWarning)
        {
            warningHighThreshold = highWarning;
            warningLowThreshold = lowWarning;
        }

        public void SetName(string displayName)
        {
            if (nameLabel == null)
            {
                nameLabel = transform.Find("Name")?.GetComponent<Text>();
            }

            if (nameLabel != null)
            {
                nameLabel.text = displayName ?? string.Empty;
                UiFont.Apply(nameLabel, bold: true);
                nameLabel.gameObject.SetActive(true);
                nameLabel.enabled = true;
                nameLabel.transform.SetAsLastSibling();
            }
        }

        public void SetHelpDescription(string description)
        {
            helpDescription = description ?? string.Empty;
            EnsureHelpButton();
        }

        public void BindNameLabel(Text name)
        {
            nameLabel = name;
        }

#if UNITY_EDITOR
        public void EditorBind(Text name, Text value, Image fill, Image background)
        {
            nameLabel = name;
            valueLabel = value;
            fillImage = fill;
            backgroundImage = background;
        }
#endif

        public void SetValueInstant(int value)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            ApplyValue(value);
        }

        public Coroutine AnimateTo(int targetValue, float durationSeconds)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(AnimateRoutine(displayedValue, targetValue, durationSeconds));
            return animationRoutine;
        }

        private IEnumerator AnimateRoutine(int from, int to, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                ApplyValue(to);
                animationRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / durationSeconds);
                var value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                ApplyValue(value);
                yield return null;
            }

            ApplyValue(to);
            animationRoutine = null;
        }

        private void ApplyValue(int value)
        {
            displayedValue = Mathf.Clamp(value, 0, maxValue);
            if (valueLabel != null)
            {
                valueLabel.text = displayedValue.ToString();
                UiFont.Apply(valueLabel);
            }

            var amount = maxValue <= 0 ? 0f : displayedValue / (float)maxValue;
            if (fillImage != null)
            {
                // 스프라이트 없는 Image는 Filled+fillAmount가 시각적으로 먹지 않으므로
                // RectTransform 가로 비율로 게이지를 줄인다.
                ApplyFillAmount(fillImage, amount);
                fillImage.color = IsWarning(displayedValue) ? warningFillColor : normalFillColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = IsWarning(displayedValue) ? warningBackgroundColor : normalBackgroundColor;
            }
        }

        private static void ApplyFillAmount(Image fill, float amount)
        {
            amount = Mathf.Clamp01(amount);
            fill.type = Image.Type.Simple;
            fill.fillAmount = amount;

            var rect = fill.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(amount, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private bool IsWarning(int value)
        {
            if (warningHighThreshold >= 0 && value >= warningHighThreshold)
            {
                return true;
            }

            if (warningLowThreshold >= 0 && value <= warningLowThreshold)
            {
                return true;
            }

            return false;
        }

        private void EnsureHelpButton()
        {
            if (helpButton != null || string.IsNullOrEmpty(helpDescription))
            {
                return;
            }

            helpButton = GetComponent<Button>();
            if (helpButton == null)
            {
                helpButton = gameObject.AddComponent<Button>();
                var graphic = backgroundImage != null
                    ? (Graphic)backgroundImage
                    : GetComponent<Image>();
                if (graphic != null)
                {
                    graphic.raycastTarget = true;
                    helpButton.targetGraphic = graphic;
                }
            }

            helpButton.onClick.RemoveListener(OnHelpClicked);
            helpButton.onClick.AddListener(OnHelpClicked);
        }

        private void OnHelpClicked()
        {
            if (string.IsNullOrEmpty(helpDescription))
            {
                return;
            }

            var hud = GetComponentInParent<GameHudView>();
            hud?.ShowStatHelp(helpDescription);
            AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);
        }
    }
}
