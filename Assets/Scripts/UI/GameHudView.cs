using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 상단 HUD. Presenter가 값을 밀어 넣는다.
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private Text dayLabel;
        [SerializeField] private Text cashLabel;
        [SerializeField] private StatGaugeView healthGauge;
        [SerializeField] private StatGaugeView stressGauge;
        [SerializeField] private StatGaugeView happinessGauge;
        [SerializeField] private StatGaugeView companyGauge;
        [SerializeField] private GameObject crisisBanner;
        [SerializeField] private Text crisisBannerLabel;
        [SerializeField] private Button settingsButton;

        public StatGaugeView HealthGauge => healthGauge;
        public StatGaugeView StressGauge => stressGauge;
        public StatGaugeView HappinessGauge => happinessGauge;
        public StatGaugeView CompanyGauge => companyGauge;

        public void SetDayText(string text)
        {
            if (dayLabel != null)
            {
                dayLabel.text = text;
                UiFont.Apply(dayLabel, bold: true);
                dayLabel.transform.SetAsLastSibling();
            }
        }

        public void SetCash(long cash)
        {
            if (cashLabel != null)
            {
                cashLabel.text = KoreanWonFormatter.Format(cash);
                UiFont.Apply(cashLabel, bold: true);
                cashLabel.transform.SetAsLastSibling();
            }
        }

        private void OnEnable()
        {
            // 다른 패널에 가려지지 않도록 HUD를 앞으로
            transform.SetAsLastSibling();
        }

        public void SetCrisis(bool active, string message)
        {
            if (crisisBanner != null)
            {
                crisisBanner.SetActive(active);
            }

            if (crisisBannerLabel != null)
            {
                crisisBannerLabel.text = message ?? string.Empty;
            }
        }

        public void ShowStatHelp(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (crisisBannerLabel != null)
            {
                crisisBannerLabel.text = message;
                if (crisisBanner != null)
                {
                    crisisBanner.SetActive(true);
                }
            }
            else
            {
                Debug.Log($"[HUD] {message}");
            }
        }

        public void BindGauges(
            StatGaugeView health,
            StatGaugeView stress,
            StatGaugeView happiness,
            StatGaugeView company)
        {
            healthGauge = health;
            stressGauge = stress;
            happinessGauge = happiness;
            companyGauge = company;
        }

        public void BindSettingsButton(Button button)
        {
            settingsButton = button;
        }

        public void SetSettingsClickHandler(UnityEngine.Events.UnityAction handler)
        {
            EnsureSettingsButton();
            if (settingsButton == null)
            {
                return;
            }

            settingsButton.onClick.RemoveAllListeners();
            if (handler != null)
            {
                settingsButton.onClick.AddListener(handler);
            }

            settingsButton.transform.SetAsLastSibling();
        }

        /// <summary>레이아웃 적용 시 설정 버튼만 생성·배치한다.</summary>
        public void EnsureInGameSettingsButton()
        {
            EnsureSettingsButton();
            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
            }
        }

        private void EnsureSettingsButton()
        {
            if (settingsButton != null)
            {
                settingsButton.gameObject.SetActive(true);
                return;
            }

            var existing = transform.Find("SettingsButton");
            if (existing != null)
            {
                settingsButton = existing.GetComponent<Button>();
                if (settingsButton != null)
                {
                    return;
                }
            }

            var go = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -68f);
            rect.sizeDelta = new Vector2(120f, 52f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.28f, 0.48f, 0.62f, 1f);
            settingsButton = go.GetComponent<Button>();
            settingsButton.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "설정";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);
        }

        public void BindLabels(Text day, Text cash, GameObject crisisRoot, Text crisisText)
        {
            dayLabel = day;
            cashLabel = cash;
            crisisBanner = crisisRoot;
            crisisBannerLabel = crisisText;
        }

        /// <summary>자식에서 Day/Cash 라벨을 다시 찾아 바인딩하고 맨 앞으로 올린다.</summary>
        public void RefreshTopLabelBindings()
        {
            var day = transform.Find("DayLabel")?.GetComponent<Text>();
            var cash = transform.Find("CashLabel")?.GetComponent<Text>();
            if (day != null)
            {
                dayLabel = day;
            }

            if (cash != null)
            {
                cashLabel = cash;
            }

            transform.SetAsLastSibling();
            dayLabel?.transform.SetAsLastSibling();
            cashLabel?.transform.SetAsLastSibling();
        }
    }
}
