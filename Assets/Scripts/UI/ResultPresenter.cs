using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Audio;
using SurviveUntilPayday.Core;
using System.Collections.Generic;
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
        [SerializeField] private Text tipLabel;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button doubleXpAdButton;

        private bool runCompletionNotified;
        private bool navigatingToMenu;
        private RectTransform eventUnlockRoot;
        private Text eventUnlockHeader;
        private Transform eventUnlockContent;

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
            var audio = AppRoot.EnsureCreated().Audio;
            audio?.SetBgm(BgmId.Result);

            var session = AppRoot.Instance != null ? AppRoot.Instance.Session : null;
            var result = session?.LastResult;
            if (result == null)
            {
                ShowPlaceholder();
                RefreshDoubleXpButton(null);
                return;
            }

            audio?.PlaySfx(result.IsSuccess ? SfxId.Success : SfxId.Fail);
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

        public void BindTipLabel(Text tip)
        {
            tipLabel = tip;
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

                experienceLabel.alignment = TextAnchor.UpperCenter;
                experienceLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                experienceLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (unlockLabel != null)
            {
                unlockLabel.text = BuildUnlockHighlight(result, unlockedCount);
                unlockLabel.alignment = TextAnchor.UpperCenter;
                unlockLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                unlockLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            PopulateEventUnlocks(result);
            EnsureTipLabel();
            if (tipLabel != null)
            {
                tipLabel.text = FailureTipCatalog.GetTip(result.FailureReason, result.IsSuccess);
                tipLabel.alignment = TextAnchor.UpperCenter;
                tipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                tipLabel.verticalOverflow = VerticalWrapMode.Overflow;
                UiFont.Apply(tipLabel);
            }

            RelayoutResultBody();
        }

        private void PopulateEventUnlocks(ResultData result)
        {
            var titles = ResolveEventTitles(result?.MetaProgress);
            EnsureEventUnlockPanel();
            if (eventUnlockRoot == null)
            {
                return;
            }

            if (titles.Count == 0)
            {
                eventUnlockRoot.gameObject.SetActive(false);
                return;
            }

            eventUnlockRoot.gameObject.SetActive(true);
            if (eventUnlockHeader != null)
            {
                eventUnlockHeader.text = titles.Count == 1
                    ? "★ 새 사건 1개"
                    : $"★ 새 사건 {titles.Count}개 · 아래로 스크롤";
                UiFont.Apply(eventUnlockHeader, bold: true);
            }

            ClearEventRows();
            for (var i = 0; i < titles.Count; i++)
            {
                CreateEventRow(titles[i], i);
            }

            Canvas.ForceUpdateCanvases();
            if (eventUnlockContent is RectTransform contentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            }
        }

        private static List<string> ResolveEventTitles(MetaProgressResult meta)
        {
            if (meta == null)
            {
                return new List<string>();
            }

            if (meta.NewlyUnlockedEventTitles != null && meta.NewlyUnlockedEventTitles.Count > 0)
            {
                return new List<string>(meta.NewlyUnlockedEventTitles);
            }

            // 구 세이브/테스트 호환: id만 있을 때 카탈로그·fallback 해석
            return UnlockDisplayNames.MapEventTitles(meta.NewlyUnlockedEvents);
        }

        private void EnsureEventUnlockPanel()
        {
            if (eventUnlockRoot != null)
            {
                return;
            }

            var parent = unlockLabel != null
                ? unlockLabel.transform.parent
                : (titleLabel != null ? titleLabel.transform.parent : transform);
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find("EventUnlockBlock") as RectTransform;
            if (existing != null)
            {
                eventUnlockRoot = existing;
                eventUnlockHeader = existing.Find("Header")?.GetComponent<Text>();
                eventUnlockContent = existing.Find("Scroll/Viewport/Content");
                return;
            }

            var rootGo = new GameObject("EventUnlockBlock", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(parent, false);
            eventUnlockRoot = rootGo.GetComponent<RectTransform>();
            eventUnlockRoot.anchorMin = eventUnlockRoot.anchorMax = new Vector2(0.5f, 0.5f);
            eventUnlockRoot.pivot = new Vector2(0.5f, 1f);
            eventUnlockRoot.sizeDelta = new Vector2(900f, 220f);
            rootGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            rootGo.GetComponent<Image>().raycastTarget = true;

            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(rootGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 40f);
            eventUnlockHeader = headerGo.AddComponent<Text>();
            eventUnlockHeader.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            eventUnlockHeader.fontSize = 26;
            eventUnlockHeader.alignment = TextAnchor.MiddleCenter;
            eventUnlockHeader.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            eventUnlockHeader.raycastTarget = false;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(rootGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(16f, 12f);
            scrollRt.offsetMax = new Vector2(-16f, -44f);
            scrollGo.GetComponent<Image>().color = new Color(0.97f, 0.96f, 0.94f, 0.9f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            eventUnlockContent = content.transform;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
        }

        private void ClearEventRows()
        {
            if (eventUnlockContent == null)
            {
                return;
            }

            for (var i = eventUnlockContent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(eventUnlockContent.GetChild(i).gameObject);
            }
        }

        private void CreateEventRow(string title, int index)
        {
            var go = new GameObject($"Event_{index}", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(eventUnlockContent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = 32f;
            element.preferredHeight = 32f;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.18f, 0.2f, 0.24f, 1f);
            text.text = $"· {title}";
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UiFont.Apply(text);
        }

        /// <summary>
        /// XP/해금/팁이 여러 줄일 때 고정 Rect에 겹치지 않도록 높이와 버튼 위치를 재배치한다.
        /// </summary>
        private void RelayoutResultBody()
        {
            const float width = 900f;
            const float gap = 16f;
            var cursorY = -10f;

            Canvas.ForceUpdateCanvases();

            if (experienceLabel != null)
            {
                var xpHeight = Mathf.Max(48f, experienceLabel.preferredHeight + 8f);
                experienceLabel.rectTransform.anchoredPosition = new Vector2(0f, cursorY);
                experienceLabel.rectTransform.sizeDelta = new Vector2(width, xpHeight);
                experienceLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
                cursorY -= xpHeight + gap;
            }

            if (unlockLabel != null)
            {
                unlockLabel.rectTransform.sizeDelta = new Vector2(width, 800f);
                Canvas.ForceUpdateCanvases();
                var unlockHeight = Mathf.Clamp(unlockLabel.preferredHeight + 12f, 40f, 200f);
                unlockLabel.rectTransform.anchoredPosition = new Vector2(0f, cursorY);
                unlockLabel.rectTransform.sizeDelta = new Vector2(width, unlockHeight);
                unlockLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
                cursorY -= unlockHeight + gap;
            }

            if (eventUnlockRoot != null && eventUnlockRoot.gameObject.activeSelf)
            {
                var eventCount = eventUnlockContent != null ? eventUnlockContent.childCount : 0;
                var eventHeight = Mathf.Clamp(56f + eventCount * 34f, 120f, 260f);
                eventUnlockRoot.anchoredPosition = new Vector2(0f, cursorY);
                eventUnlockRoot.sizeDelta = new Vector2(width, eventHeight);
                eventUnlockRoot.pivot = new Vector2(0.5f, 1f);
                cursorY -= eventHeight + gap;
            }

            if (tipLabel != null && !string.IsNullOrWhiteSpace(tipLabel.text))
            {
                var tipHeight = Mathf.Clamp(tipLabel.preferredHeight + 8f, 40f, 100f);
                tipLabel.rectTransform.anchoredPosition = new Vector2(0f, cursorY);
                tipLabel.rectTransform.sizeDelta = new Vector2(width, tipHeight);
                tipLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
                cursorY -= tipHeight + gap;
            }

            // 버튼은 SafeArea 중앙 앵커 기준. 해금 블록 아래에 두고 하단과 겹치지 않게 클램핑.
            var buttonTop = Mathf.Min(cursorY - 8f, -200f);
            var doubleXpY = buttonTop - 45f;
            var backY = doubleXpY - 110f;
            if (backY < -820f)
            {
                var shift = -820f - backY;
                doubleXpY += shift;
                backY += shift;
            }

            if (doubleXpAdButton != null)
            {
                var rect = doubleXpAdButton.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(0f, doubleXpY);
                }
            }

            if (backToMenuButton != null)
            {
                var rect = backToMenuButton.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(0f, backY);
                }
            }
        }

        private void EnsureTipLabel()
        {
            if (tipLabel != null)
            {
                return;
            }

            var parent = unlockLabel != null
                ? unlockLabel.transform.parent
                : (titleLabel != null ? titleLabel.transform.parent : transform);
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find("TipLabel")?.GetComponent<Text>();
            if (existing != null)
            {
                tipLabel = existing;
                return;
            }

            var go = new GameObject("TipLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -200f);
            rect.sizeDelta = new Vector2(900f, 70f);

            tipLabel = go.GetComponent<Text>();
            tipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            tipLabel.fontSize = 22;
            tipLabel.alignment = TextAnchor.UpperCenter;
            tipLabel.color = new Color(0.2f, 0.25f, 0.3f, 1f);
            tipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            tipLabel.verticalOverflow = VerticalWrapMode.Overflow;
            tipLabel.raycastTarget = false;
        }

        private static string BuildUnlockHighlight(ResultData result, int unlockedCount)
        {
            UnlockDisplayNames.EnsureLoaded();
            var parts = new List<string>();
            if (result.EndingNewlyUnlocked && result.Ending != null)
            {
                parts.Add($"★ 새 엔딩: {result.Ending.Title}");
            }
            else if (result.Ending != null)
            {
                parts.Add($"엔딩: {result.Ending.Title} (도감 {unlockedCount}개)");
            }

            var meta = result.MetaProgress;
            if (meta == null)
            {
                return string.Join("\n", parts);
            }

            AppendDisplayUnlocks(
                parts,
                "★ 특성",
                ResolveTraitNames(meta),
                maxShow: 4);
            AppendDisplayUnlocks(
                parts,
                "★ 직업",
                ResolveJobNames(meta),
                maxShow: 4);
            // 사건은 스크롤 목록으로 따로 표시한다.

            if (meta.NewlyUnlockedAchievements.Count > 0)
            {
                var names = new List<string>();
                var maxAchievements = Mathf.Min(meta.NewlyUnlockedAchievements.Count, 4);
                for (var i = 0; i < maxAchievements; i++)
                {
                    names.Add(AchievementIds.GetDisplayName(meta.NewlyUnlockedAchievements[i]));
                }

                var suffix = meta.NewlyUnlockedAchievements.Count > maxAchievements
                    ? $" 외 {meta.NewlyUnlockedAchievements.Count - maxAchievements}개"
                    : string.Empty;
                parts.Add("★ 업적: " + string.Join(", ", names) + suffix);
            }

            if (meta.TraitFragmentsGained > 0)
            {
                parts.Add($"특성 조각 +{meta.TraitFragmentsGained}");
            }

            return string.Join("\n", parts);
        }

        private static List<string> ResolveTraitNames(MetaProgressResult meta)
        {
            if (meta.NewlyUnlockedTraitNames != null && meta.NewlyUnlockedTraitNames.Count > 0)
            {
                return new List<string>(meta.NewlyUnlockedTraitNames);
            }

            return UnlockDisplayNames.MapTraitNames(meta.NewlyUnlockedTraits);
        }

        private static List<string> ResolveJobNames(MetaProgressResult meta)
        {
            if (meta.NewlyUnlockedJobNames != null && meta.NewlyUnlockedJobNames.Count > 0)
            {
                return new List<string>(meta.NewlyUnlockedJobNames);
            }

            return UnlockDisplayNames.MapJobNames(meta.NewlyUnlockedJobs);
        }

        private static void AppendDisplayUnlocks(
            List<string> parts,
            string prefix,
            List<string> names,
            int maxShow = 8)
        {
            if (names == null || names.Count == 0)
            {
                return;
            }

            if (names.Count == 1)
            {
                parts.Add($"{prefix}: {names[0]}");
                return;
            }

            // 여러 개면 개수 + 대표 이름 (한 줄이 과하게 길어지지 않게)
            var shown = names.Count <= maxShow
                ? string.Join(", ", names)
                : string.Join(", ", names.GetRange(0, maxShow)) + $" 외 {names.Count - maxShow}개";
            parts.Add($"{prefix} {names.Count}개: {shown}");
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

            EnsureTipLabel();
            if (tipLabel != null)
            {
                tipLabel.text = string.Empty;
            }
        }

        private void OnBackToMenuClicked()
        {
            if (navigatingToMenu)
            {
                return;
            }

            AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);

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
