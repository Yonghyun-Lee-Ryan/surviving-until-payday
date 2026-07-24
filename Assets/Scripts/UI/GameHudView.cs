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
