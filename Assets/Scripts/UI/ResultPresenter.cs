using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Result Scene Presenter. LastResult를 표시한다.
    /// </summary>
    public sealed class ResultPresenter : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text endingTitleLabel;
        [SerializeField] private Text endingDescriptionLabel;
        [SerializeField] private Text daysLabel;
        [SerializeField] private Text cashLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Text experienceLabel;
        [SerializeField] private Text unlockLabel;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button doubleXpAdButton;

        private bool runCompletionNotified;
        private bool navigatingToMenu;

        private void Awake()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (doubleXpAdButton != null)
            {
                doubleXpAdButton.onClick.AddListener(OnDoubleXpAdClicked);
            }
        }

        private void OnDestroy()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            }

            if (doubleXpAdButton != null)
            {
                doubleXpAdButton.onClick.RemoveListener(OnDoubleXpAdClicked);
            }
        }

        private void Start()
        {
            var session = AppRoot.Instance != null ? AppRoot.Instance.Session : null;
            var result = session?.LastResult;
            if (result == null)
            {
                ShowPlaceholder();
                RefreshDoubleXpButton(null);
                return;
            }

            NotifyRunCompletedOnce();
            ShowResult(result, session.Meta.Endings.UnlockedCount);
            RefreshDoubleXpButton(session);
        }

        public void Bind(
            Text title,
            Text endingTitle,
            Text endingDescription,
            Text days,
            Text cash,
            Text stats,
            Text experience,
            Text unlock,
            Button backButton)
        {
            titleLabel = title;
            endingTitleLabel = endingTitle;
            endingDescriptionLabel = endingDescription;
            daysLabel = days;
            cashLabel = cash;
            statsLabel = stats;
            experienceLabel = experience;
            unlockLabel = unlock;
            backToMenuButton = backButton;
        }

        public void BindDoubleXpButton(Button button)
        {
            doubleXpAdButton = button;
        }

        private void ShowResult(ResultData result, int unlockedCount)
        {
            if (titleLabel != null)
            {
                titleLabel.text = result.IsSuccess ? "월급날 생존!" : "회차 종료";
            }

            if (endingTitleLabel != null)
            {
                endingTitleLabel.text = result.Ending != null
                    ? result.Ending.Title
                    : FailureEvaluator.ToDisplayName(result.FailureReason);
            }

            if (endingDescriptionLabel != null)
            {
                if (result.Ending != null)
                {
                    endingDescriptionLabel.text = result.Ending.Description;
                }
                else if (!result.IsSuccess)
                {
                    endingDescriptionLabel.text =
                        $"{FailureEvaluator.ToDisplayName(result.FailureReason)}로 이번 회차가 끝났습니다.";
                }
                else
                {
                    endingDescriptionLabel.text = "엔딩 데이터가 없습니다.";
                }
            }

            if (daysLabel != null)
            {
                daysLabel.text = $"생존 일수: {result.DaysSurvived}일";
            }

            if (cashLabel != null)
            {
                cashLabel.text = $"남은 현금: {KoreanWonFormatter.Format(result.FinalStats.Cash)}";
            }

            if (statsLabel != null)
            {
                var stats = result.FinalStats;
                statsLabel.text =
                    $"건강 {stats.Health} / 스트레스 {stats.Stress}\n" +
                    $"행복도 {stats.Happiness} / 회사 평가 {stats.CompanyScore}";
            }

            if (experienceLabel != null)
            {
                experienceLabel.text = $"인생 경험치 +{result.ExperienceGained}";
                if (result.MetaProgress != null)
                {
                    experienceLabel.text +=
                        $"\nLv.{result.MetaProgress.LevelBefore} → Lv.{result.MetaProgress.LevelAfter}";
                }
            }

            if (unlockLabel != null)
            {
                var parts = new System.Collections.Generic.List<string>();
                if (result.EndingNewlyUnlocked)
                {
                    parts.Add($"새 엔딩 해금! (엔딩 {unlockedCount}개)");
                }
                else if (result.Ending != null)
                {
                    parts.Add($"이미 해금된 엔딩 (엔딩 {unlockedCount}개)");
                }

                if (result.MetaProgress != null)
                {
                    if (result.MetaProgress.NewlyUnlockedTraits.Count > 0)
                    {
                        parts.Add($"특성 해금 {result.MetaProgress.NewlyUnlockedTraits.Count}개");
                    }

                    if (result.MetaProgress.NewlyUnlockedEvents.Count > 0)
                    {
                        parts.Add($"사건 도감 +{result.MetaProgress.NewlyUnlockedEvents.Count}");
                    }

                    if (result.MetaProgress.NewlyUnlockedAchievements.Count > 0)
                    {
                        parts.Add($"업적 {result.MetaProgress.NewlyUnlockedAchievements.Count}개");
                    }
                }

                unlockLabel.text = string.Join("\n", parts);
            }
        }

        private void ShowPlaceholder()
        {
            if (titleLabel != null)
            {
                titleLabel.text = "결과";
            }

            if (endingTitleLabel != null)
            {
                endingTitleLabel.text = "결과 데이터 없음";
            }

            if (endingDescriptionLabel != null)
            {
                endingDescriptionLabel.text = "Game Scene에서 회차를 완료하면 결과가 표시됩니다.";
            }
        }

        private void OnBackToMenuClicked()
        {
            if (navigatingToMenu)
            {
                return;
            }

            if (AppRoot.Instance == null || AppRoot.Instance.SceneLoader == null)
            {
                Debug.LogError("[ResultPresenter] SceneLoader is unavailable.", this);
                return;
            }

            navigatingToMenu = true;
            if (backToMenuButton != null)
            {
                backToMenuButton.interactable = false;
            }

            var interstitial = AppRoot.Instance.InterstitialAds;
            if (interstitial == null)
            {
                AppRoot.Instance.SceneLoader.LoadMainMenu();
                return;
            }

            // 광고 실패/스킵이어도 메뉴 이동은 진행한다.
            interstitial.TryShowOnReturnToMenu(_ =>
            {
                if (AppRoot.Instance != null && AppRoot.Instance.SceneLoader != null)
                {
                    AppRoot.Instance.SceneLoader.LoadMainMenu();
                }
            });
        }

        private void OnDoubleXpAdClicked()
        {
            var appRoot = AppRoot.Instance;
            var session = appRoot != null ? appRoot.Session : null;
            var gateway = appRoot != null ? appRoot.RewardedAds : null;
            if (session?.LastResult == null || gateway == null)
            {
                return;
            }

            if (session.DoubleExperienceClaimedForLastResult)
            {
                RefreshDoubleXpButton(session);
                return;
            }

            if (doubleXpAdButton != null)
            {
                doubleXpAdButton.interactable = false;
            }

            gateway.Request(RewardedAdPlacement.DoubleExperience, result =>
            {
                if (result.RewardGranted)
                {
                    var bonus = session.LastResult.ExperienceGained;
                    session.Meta.AddBonusExperience(bonus);
                    session.DoubleExperienceClaimedForLastResult = true;
                    appRoot.PersistSession(includeActiveRun: false);

                    if (experienceLabel != null)
                    {
                        experienceLabel.text =
                            $"인생 경험치 +{session.LastResult.ExperienceGained} (광고 2배 적용, 총 +{session.LastResult.ExperienceGained * 2})";
                    }
                }

                RefreshDoubleXpButton(session);
            });
        }

        private void NotifyRunCompletedOnce()
        {
            if (runCompletionNotified || AppRoot.Instance?.InterstitialAds == null)
            {
                return;
            }

            AppRoot.Instance.InterstitialAds.NotifyRunCompleted();
            runCompletionNotified = true;
        }

        private void RefreshDoubleXpButton(GameSession session)
        {
            if (doubleXpAdButton == null)
            {
                return;
            }

            var canShow = session?.LastResult != null
                          && !session.DoubleExperienceClaimedForLastResult
                          && AppRoot.Instance?.RewardedAds != null
                          && AppRoot.Instance.RewardedAds.CanRequest(
                              RewardedAdPlacement.DoubleExperience,
                              out _);

            doubleXpAdButton.gameObject.SetActive(session?.LastResult != null);
            doubleXpAdButton.interactable = canShow;
        }
    }
}
