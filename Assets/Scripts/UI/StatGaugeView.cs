using System.Collections;
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

        public int DisplayedValue => displayedValue;

        public void ConfigureThresholds(int highWarning, int lowWarning)
        {
            warningHighThreshold = highWarning;
            warningLowThreshold = lowWarning;
        }

        public void SetName(string displayName)
        {
            if (nameLabel != null)
            {
                nameLabel.text = displayName;
            }
        }

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
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = maxValue <= 0 ? 0f : displayedValue / (float)maxValue;
                fillImage.color = IsWarning(displayedValue) ? warningFillColor : normalFillColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = IsWarning(displayedValue) ? warningBackgroundColor : normalBackgroundColor;
            }
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
    }
}
