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
            }
        }

        public void SetCash(long cash)
        {
            if (cashLabel != null)
            {
                cashLabel.text = KoreanWonFormatter.Format(cash);
            }
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
    }
}
