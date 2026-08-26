using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.Audio;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using SurviveUntilPayday.Events;
using UnityEngine;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Game Scene Presenter. UI는 GameState를 직접 수정하지 않는다.
    /// </summary>
    public sealed class GamePlayPresenter : MonoBehaviour, IGameDebugAccess
    {
        private const float GaugeAnimSeconds = 0.35f;
        private const int StressWarning = 80;
        private const int HealthWarning = 20;
        private const int CompanyWarning = 20;

        [Header("Views")]
        [SerializeField] private GameHudView hudView;
        [SerializeField] private EventPanelView eventPanelView;
        [SerializeField] private ChoicePanelView choicePanelView;
        [SerializeField] private ResultPopupView resultPopupView;
        [SerializeField] private WeeklySummaryPopupView weeklySummaryPopupView;
        [SerializeField] private ArtCatalog artCatalog;

        [Header("Run Data")]
        [SerializeField] private JobData startingJob;
        [SerializeField] private TraitData startingTrait;
        [SerializeField] private List<EventData> eventCatalog = new List<EventData>();
        [SerializeField] private EventData fallbackEvent;
        [SerializeField] private int randomSeed = 1;

        [Header("Daily (Unit 25)")]
        [SerializeField] private List<DailyMissionData> dailyMissionPool = new List<DailyMissionData>();

        [Header("Endings")]
        [SerializeField] private List<EndingData> endingCatalog = new List<EndingData>();
        [SerializeField] private EndingData fallbackSuccessEnding;

        private RunManager runManager;
        private EventSelector eventSelector;
        private EffectResolver effectResolver;
        private RunHistory runHistory;
        private SeededRandomService randomService;
        private bool choiceLocked;
        private bool advancing;
        private bool waitingWeeklyContinue;
        private bool waitingSalaryConfirm;
        private WeeklySummaryInfo lastWeeklySummary;
        private string pendingEventId;
        private readonly HashSet<string> discoveredEventIdsThisRun = new HashSet<string>();
        private bool isDailyChallengeRun;

        [Header("Meta")]
        [SerializeField] private List<TraitData> allTraits = new List<TraitData>();
        [SerializeField] private List<JobData> allJobs = new List<JobData>();

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (choicePanelView != null)
            {
                choicePanelView.ChoiceClicked += OnChoiceClicked;
                choicePanelView.RerollAdClicked += OnRerollAdClicked;
            }

            if (resultPopupView != null)
            {
                resultPopupView.NextDayClicked += OnNextDayClicked;
                resultPopupView.RetryAdClicked += OnRetryAdClicked;
                resultPopupView.SideJobAdClicked += OnSideJobAdClicked;
                resultPopupView.LoanAdClicked += OnLoanAdClicked;
            }

            if (weeklySummaryPopupView != null)
            {
                weeklySummaryPopupView.ContinueClicked += OnWeeklySummaryContinue;
            }

            SubscribeChoicePreviewSetting();
        }

        private void OnDisable()
        {
            if (choicePanelView != null)
            {
                choicePanelView.ChoiceClicked -= OnChoiceClicked;
                choicePanelView.RerollAdClicked -= OnRerollAdClicked;
            }

            if (resultPopupView != null)
            {
                resultPopupView.NextDayClicked -= OnNextDayClicked;
                resultPopupView.RetryAdClicked -= OnRetryAdClicked;
                resultPopupView.SideJobAdClicked -= OnSideJobAdClicked;
                resultPopupView.LoanAdClicked -= OnLoanAdClicked;
            }

            if (weeklySummaryPopupView != null)
            {
                weeklySummaryPopupView.ContinueClicked -= OnWeeklySummaryContinue;
            }

            UnsubscribeChoicePreviewSetting();

            if (runManager != null
                && runManager.Status == RunStatus.InProgress
                && AppRoot.Instance != null)
            {
                SaveActiveRun();
            }

            UnsubscribeStateEvents();
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                return;
            }

            GameplayLayoutApplier.Apply(hudView, eventPanelView, choicePanelView);
            WireHudSettingsButton();
            SubscribeChoicePreviewSetting();
            // 폰트 로드 직후 한 프레임 뒤에도 HUD를 재적용 (씬 Arial 덮어쓰기 보장)
            StartCoroutine(ReapplyHudNextFrame());

            if (startingJob == null)
            {
                Debug.LogError("[GamePlayPresenter] startingJob is not assigned.", this);
                return;
            }

            if (fallbackEvent == null)
            {
                Debug.LogError("[GamePlayPresenter] fallbackEvent is not assigned.", this);
                return;
            }

            var session = AppRoot.Instance != null ? AppRoot.Instance.Session : null;
            if (session != null && session.StartMode == GameStartMode.ContinueRun && session.HasActiveRun)
            {
                BeginContinuedRun(session.CachedSave.run);
            }
            else
            {
                BeginNewRun();
            }
        }

        private IEnumerator ReapplyHudNextFrame()
        {
            yield return null;
            GameplayLayoutApplier.Apply(hudView, eventPanelView, choicePanelView);
            WireHudSettingsButton();
            SubscribeChoicePreviewSetting();
            ConfigureGaugeThresholds();
            if (runManager?.State != null)
            {
                RefreshHudInstant();
            }
        }

        private void WireHudSettingsButton()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.SetSettingsClickHandler(() =>
            {
                AppRoot.EnsureCreated().Audio?.PlaySfx(SfxId.Click);
                AppRoot.EnsureCreated().OpenSettings();
            });
        }

        public void BindViews(
            GameHudView hud,
            EventPanelView eventPanel,
            ChoicePanelView choicePanel,
            ResultPopupView resultPopup,
            WeeklySummaryPopupView weeklyPopup = null)
        {
            hudView = hud;
            eventPanelView = eventPanel;
            choicePanelView = choicePanel;
            resultPopupView = resultPopup;
            weeklySummaryPopupView = weeklyPopup;
        }

        public void BindRunData(JobData job, TraitData trait, List<EventData> catalog, EventData fallback, int seed)
        {
            startingJob = job;
            startingTrait = trait;
            eventCatalog = catalog ?? new List<EventData>();
            fallbackEvent = fallback;
            randomSeed = seed;
        }

        public void BindEndings(List<EndingData> endings, EndingData fallbackSuccess)
        {
            endingCatalog = endings ?? new List<EndingData>();
            fallbackSuccessEnding = fallbackSuccess;
        }

        private void BeginNewRun()
        {
            discoveredEventIdsThisRun.Clear();
            if (AppRoot.Instance?.AdQuota != null)
            {
                AppRoot.Instance.AdQuota.BeginRun();
            }

            if (AppRoot.Instance?.Session != null)
            {
                AppRoot.Instance.Session.DoubleExperienceClaimedForLastResult = false;
            }

            var session = AppRoot.Instance?.Session;
            isDailyChallengeRun = session != null && session.IsDailyChallengeRun;
            if (session != null && session.TryConsumePendingRandomSeed(out var dailySeed))
            {
                randomSeed = dailySeed;
            }

            ResolveNewRunJobAndTrait(out var job, out var trait);

            randomService = new SeededRandomService(randomSeed);
            runHistory = new RunHistory();
            runManager = new RunManager();
            runManager.StartRun(job, trait, randomSeed);

            eventSelector = new EventSelector(eventCatalog, fallbackEvent, randomService);
            effectResolver = new EffectResolver(
                runManager.State,
                randomService,
                runHistory,
                runManager.Days,
                trait);

            EnsureSessionEndings();
            SubscribeStateEvents();
            ConfigureGaugeThresholds();
            resultPopupView.Hide();
            RefreshHudInstant();
            TrackRunStarted(continued: false);
            PresentTodaysEvent();
            SaveActiveRun();
        }

        private void BeginContinuedRun(Save.RunSaveData runSave)
        {
            isDailyChallengeRun = false;
            if (runSave == null || !runSave.hasActiveRun)
            {
                Debug.LogWarning("[GamePlayPresenter] Continue requested but no run save. Starting new run.");
                BeginNewRun();
                return;
            }

            randomService = new SeededRandomService(runSave.randomSeed);
            randomService.FastForward(Mathf.Max(0, runSave.consumedRandomCalls));
            runHistory = new RunHistory();
            runManager = new RunManager();
            runManager.StartRunWithState(Save.SaveMapper.ToGameState(runSave));

            var activeTrait = ResolveTraitById(runManager.State?.TraitId);

            eventSelector = new EventSelector(eventCatalog, fallbackEvent, randomService);
            eventSelector.RestoreHistory(runSave.recentEventIds, runSave.lastSelectedEventId);
            effectResolver = new EffectResolver(
                runManager.State,
                randomService,
                runHistory,
                runManager.Days,
                activeTrait);

            EnsureSessionEndings();
            SubscribeStateEvents();
            ConfigureGaugeThresholds();
            resultPopupView.Hide();
            RefreshHudInstant();
            TrackRunStarted(continued: true);
            PresentSavedEventOrSelect(runSave.pendingEventId);
        }

        private void ResolveNewRunJobAndTrait(out JobData job, out TraitData trait)
        {
            job = startingJob;
            trait = startingTrait;

            var session = AppRoot.Instance?.Session;
            if (session == null || !session.UsePendingRunSelection)
            {
                return;
            }

            if (session.PendingJob != null)
            {
                job = session.PendingJob;
            }

            // 명시적으로 null(특성 없음)도 허용한다.
            trait = session.PendingTrait;
            session.ClearPendingRunSelection();
        }

        private TraitData ResolveTraitById(string traitId)
        {
            if (string.IsNullOrWhiteSpace(traitId))
            {
                return null;
            }

            if (startingTrait != null && startingTrait.Id == traitId)
            {
                return startingTrait;
            }

            if (allTraits != null)
            {
                for (var i = 0; i < allTraits.Count; i++)
                {
                    if (allTraits[i] != null && allTraits[i].Id == traitId)
                    {
                        return allTraits[i];
                    }
                }
            }

            return null;
        }

        private void EnsureSessionEndings()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (appRoot.Session == null)
            {
                return;
            }

            if (endingCatalog != null && endingCatalog.Count > 0)
            {
                appRoot.Session.SetEndingCatalog(endingCatalog, fallbackSuccessEnding);
            }
        }

        private void SubscribeStateEvents()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.RunSucceeded += OnRunSucceeded;
            runManager.RunFailed += OnRunFailed;
            runManager.DayStarted += OnDayStarted;
            runManager.WeeklySummary += OnWeeklySummary;
        }

        private void UnsubscribeStateEvents()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.RunSucceeded -= OnRunSucceeded;
            runManager.RunFailed -= OnRunFailed;
            runManager.DayStarted -= OnDayStarted;
            runManager.WeeklySummary -= OnWeeklySummary;
        }

        private void ConfigureGaugeThresholds()
        {
            hudView.HealthGauge?.ConfigureThresholds(-1, HealthWarning);
            hudView.StressGauge?.ConfigureThresholds(StressWarning, -1);
            hudView.HappinessGauge?.ConfigureThresholds(-1, 20);
            hudView.CompanyGauge?.ConfigureThresholds(-1, CompanyWarning);

            hudView.HealthGauge?.SetName(StatCopy.GetDisplayName(StatType.Health));
            hudView.StressGauge?.SetName(StatCopy.GetDisplayName(StatType.Stress));
            hudView.HappinessGauge?.SetName(StatCopy.GetDisplayName(StatType.Happiness));
            hudView.CompanyGauge?.SetName(StatCopy.GetDisplayName(StatType.CompanyScore));

            hudView.HealthGauge?.SetHelpDescription(StatCopy.GetDescription(StatType.Health));
            hudView.StressGauge?.SetHelpDescription(StatCopy.GetDescription(StatType.Stress));
            hudView.HappinessGauge?.SetHelpDescription(StatCopy.GetDescription(StatType.Happiness));
            hudView.CompanyGauge?.SetHelpDescription(StatCopy.GetDescription(StatType.CompanyScore));
        }

        private void PresentTodaysEvent()
        {
            choiceLocked = false;
            advancing = false;

            var state = runManager.State;
            var selected = eventSelector.Select(state, runManager.Days);
            ShowEvent(selected);
            SaveActiveRun();
        }

        private void PresentSavedEventOrSelect(string eventId)
        {
            choiceLocked = false;
            advancing = false;

            EventData selected = null;
            if (!string.IsNullOrEmpty(eventId))
            {
                for (var i = 0; i < eventCatalog.Count; i++)
                {
                    if (eventCatalog[i] != null && eventCatalog[i].Id == eventId)
                    {
                        selected = eventCatalog[i];
                        break;
                    }
                }

                if (selected == null && fallbackEvent != null && fallbackEvent.Id == eventId)
                {
                    selected = fallbackEvent;
                }
            }

            if (selected == null)
            {
                selected = eventSelector.Select(runManager.State, runManager.Days);
            }

            ShowEvent(selected);
            SaveActiveRun();
        }

        private void ShowEvent(EventData selected, bool replaceActiveChoice = false)
        {
            pendingEventId = selected != null ? selected.Id : string.Empty;
            if (selected != null)
            {
                discoveredEventIdsThisRun.Add(selected.Id);
            }

            effectResolver.BeginEvent(selected, replaceActiveChoice);
            ApplyEventArt(selected, ExpressionId.Default, useEntryExpression: true);
            ApplyChoiceLabels(selected);
            choicePanelView.SetInteractable(true);
            resultPopupView.Hide();
            RefreshHudInstant();
            RefreshPlayBgm();
            RefreshChoiceAdButtons();

            var analytics = AppRoot.Instance?.Analytics;
            if (analytics != null && selected != null && runManager?.State != null)
            {
                analytics.EventShown(selected.Id, runManager.State.CurrentDay);
            }
        }

        private void SubscribeChoicePreviewSetting()
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.ChoicePreviewChanged -= OnChoicePreviewSettingChanged;
            settings.ChoicePreviewChanged += OnChoicePreviewSettingChanged;
        }

        private void UnsubscribeChoicePreviewSetting()
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.ChoicePreviewChanged -= OnChoicePreviewSettingChanged;
        }

        private void OnChoicePreviewSettingChanged(bool _)
        {
            if (effectResolver?.ActiveEvent == null || choicePanelView == null)
            {
                return;
            }

            ApplyChoiceLabels(effectResolver.ActiveEvent);
        }

        private void ApplyChoiceLabels(EventData selected)
        {
            if (choicePanelView == null)
            {
                return;
            }

            var texts = new string[3];
            var showPreview = AppRoot.Instance?.Settings != null
                              && AppRoot.Instance.Settings.ShowChoicePreview;
            var choiceCount = selected != null ? selected.Choices.Count : 0;
            for (var i = 0; i < 3; i++)
            {
                var choice = i < choiceCount ? selected.Choices[i] : null;
                var raw = choice != null ? choice.Text : string.Empty;
                texts[i] = ChoicePreviewCopy.CombineLabel(
                    raw,
                    ChoicePreviewCopy.FormatTrend(choice),
                    showPreview);
            }

            choicePanelView.SetChoices(texts);
        }

        /// <summary>
        /// 메인 메뉴 복귀 등 씬 전환 직전에 활성 회차를 디스크에 남긴다.
        /// </summary>
        public void FlushSaveBeforeExit()
        {
            SaveActiveRun();
        }

        private void SaveActiveRun()
        {
            if (AppRoot.Instance == null || runManager?.State == null)
            {
                return;
            }

            var run = Save.SaveMapper.CaptureRun(
                runManager.State,
                randomService,
                eventSelector,
                pendingEventId);
            AppRoot.Instance.PersistSession(includeActiveRun: true, runOverride: run);
        }

        private void OnChoiceClicked(int choiceIndex)
        {
            if (choiceLocked || advancing || effectResolver == null)
            {
                return;
            }

            if (!effectResolver.CanSelectChoice)
            {
                return;
            }

            AppRoot.Instance?.Audio?.PlaySfx(SfxId.Click);

            choiceLocked = true;
            choicePanelView.SetInteractable(false);
            choicePanelView.SetRerollVisible(false, false);

            if (!effectResolver.TryResolveChoice(choiceIndex, out var result, out var error))
            {
                Debug.LogWarning($"[GamePlayPresenter] Choice failed: {error}", this);
                choiceLocked = false;
                choicePanelView.SetInteractable(true);
                return;
            }

            var analytics = AppRoot.Instance?.Analytics;
            if (analytics != null && runManager?.State != null)
            {
                analytics.ChoiceSelected(
                    result.EventId,
                    choiceIndex,
                    runManager.State.CurrentDay,
                    result.StatsBefore,
                    result.StatsAfter);
            }

            StartCoroutine(ShowResultRoutine(result));
        }

        private IEnumerator ShowResultRoutine(ChoiceResult result)
        {
            GameAudioRules.PlayChoiceResultSfx(AppRoot.Instance?.Audio, result);
            yield return AnimateStatsTo(result.StatsAfter);

            var entryExpression = effectResolver?.ActiveEvent != null
                ? effectResolver.ActiveEvent.ResolveEntryExpression()
                : ExpressionId.Default;
            var resultExpression = ExpressionResolver.FromChoiceResult(result, entryExpression);
            ApplyExpressionOnly(resultExpression);

            var changes = BuildChangesText(result);
            var nextLabel = runManager.Days.IsFinalDay ? "결과 보기" : "다음 날";
            resultPopupView.Show("선택 결과", result.Message, changes, nextLabel);
            RefreshCrisis(result.StatsAfter);
            RefreshPlayBgm();
            hudView.SetCash(result.StatsAfter.Cash);
            RefreshResultAdButtons(result);
        }

        private void ApplyEventArt(EventData selected, ExpressionId fallbackExpression, bool useEntryExpression)
        {
            if (eventPanelView == null || selected == null)
            {
                return;
            }

            var backgroundId = selected.ResolveBackground();
            var expressionId = useEntryExpression
                ? selected.ResolveEntryExpression()
                : fallbackExpression;
            var catalog = ResolveArtCatalog();
            var categoryBg = catalog != null ? catalog.GetBackground(backgroundId) : null;
            var bgSprite = EventArtResolver.ResolveBackgroundSprite(selected.Id, categoryBg);
            var faceSprite = catalog != null ? catalog.GetExpression(expressionId) : null;
            eventPanelView.Show(
                selected.Title,
                selected.Description,
                backgroundId,
                bgSprite,
                expressionId,
                faceSprite);
        }

        private void ApplyExpressionOnly(ExpressionId expressionId)
        {
            if (eventPanelView == null)
            {
                return;
            }

            var catalog = ResolveArtCatalog();
            var sprite = catalog != null ? catalog.GetExpression(expressionId) : null;
            eventPanelView.SetExpression(expressionId, sprite, shake: true);
        }

        private ArtCatalog ResolveArtCatalog()
        {
            if (artCatalog != null)
            {
                return artCatalog;
            }

            artCatalog = Resources.Load<ArtCatalog>("Art/ArtCatalog");
            return artCatalog;
        }

        private void RefreshChoiceAdButtons()
        {
            if (choicePanelView == null)
            {
                return;
            }

            var canSelect = !choiceLocked && !advancing && !waitingWeeklyContinue;
            var rewarded = AppRoot.Instance?.RewardedAds;
            var remaining = AppRoot.Instance?.AdQuota?.GetRemaining(RewardedAdPlacement.ChoiceReroll) ?? 0;
            string blockReason = null;
            var canClick = false;
            if (!canSelect)
            {
                blockReason = null;
            }
            else if (rewarded == null)
            {
                blockReason = AdBlockReasonCopy.ServiceUnavailable;
            }
            else if (remaining <= 0)
            {
                blockReason = AdBlockReasonCopy.QuotaExhausted(RewardedAdPlacement.ChoiceReroll);
            }
            else if (!rewarded.CanRequest(RewardedAdPlacement.ChoiceReroll, out var reason))
            {
                blockReason = AdBlockReasonCopy.FromGatewayReason(reason, RewardedAdPlacement.ChoiceReroll);
            }
            else
            {
                canClick = true;
            }

            var readyLabel = $"광고: 다른 사건 보기 ({remaining})";
            choicePanelView.SetRerollVisible(
                visible: canSelect,
                interactable: canClick,
                label: AdBlockReasonCopy.ButtonLabel(readyLabel, canClick, blockReason));
        }

        private void RefreshResultAdButtons(ChoiceResult result)
        {
            if (resultPopupView == null)
            {
                return;
            }

            var rewarded = AppRoot.Instance?.RewardedAds;
            var quota = AppRoot.Instance?.AdQuota;
            var retryRemaining = quota?.GetRemaining(RewardedAdPlacement.RetryOutcome) ?? 0;
            var sideRemaining = quota?.GetRemaining(RewardedAdPlacement.DailySideJob) ?? 0;
            var loanRemaining = quota?.GetRemaining(RewardedAdPlacement.EmergencyLoan) ?? 0;
            var cash = result?.StatsAfter != null ? result.StatsAfter.Cash : runManager?.State?.Stats.Cash ?? 0;
            var needsLoan = cash < 50_000L
                            || (result != null && result.FailureAfter == FailureReason.Bankruptcy);

            ResolveAdButton(
                rewarded,
                RewardedAdPlacement.RetryOutcome,
                retryRemaining,
                "광고: 결과 재시도",
                out var retryClick,
                out var retryLabel);
            ResolveAdButton(
                rewarded,
                RewardedAdPlacement.DailySideJob,
                sideRemaining,
                "광고: 부업(+30,000원)",
                out var sideClick,
                out var sideLabel);
            string loanLabel;
            bool loanClick;
            if (!needsLoan)
            {
                loanClick = false;
                loanLabel = "광고: 긴급 대출(+100,000원)";
            }
            else
            {
                ResolveAdButton(
                    rewarded,
                    RewardedAdPlacement.EmergencyLoan,
                    loanRemaining,
                    "광고: 긴급 대출(+100,000원)",
                    out loanClick,
                    out loanLabel);
            }

            resultPopupView.SetAdButtons(
                retryVisible: true,
                retryInteractable: retryClick,
                sideJobVisible: true,
                sideJobInteractable: sideClick,
                loanVisible: needsLoan,
                loanInteractable: loanClick,
                retryLabel,
                sideLabel,
                loanLabel);
            UiModalLayer.RestackModalsAboveHud(hudView != null ? hudView.transform : null, resultPopupView, weeklySummaryPopupView);
        }

        private static void ResolveAdButton(
            RewardedAdGateway rewarded,
            RewardedAdPlacement placement,
            int remaining,
            string readyLabel,
            out bool interactable,
            out string label)
        {
            interactable = false;
            if (rewarded == null)
            {
                label = AdBlockReasonCopy.ServiceUnavailable;
                return;
            }

            if (remaining <= 0)
            {
                label = AdBlockReasonCopy.QuotaExhausted(placement);
                return;
            }

            if (!rewarded.CanRequest(placement, out var reason))
            {
                label = AdBlockReasonCopy.FromGatewayReason(reason, placement);
                return;
            }

            interactable = true;
            label = readyLabel;
        }

        private void OnRerollAdClicked()
        {
            RequestRewarded(RewardedAdPlacement.ChoiceReroll, grant =>
            {
                if (grant == null || !grant.Value.ChoiceReroll)
                {
                    return;
                }

                RerollTodaysEvent();
            });
        }

        /// <summary>
        /// 광고「다른 사건 보기」: 같은 날 다른 사건으로 교체한다.
        /// 고정 일자 사건(월세 등)만 있어도 일반 풀에서 대안을 고른다.
        /// </summary>
        private void RerollTodaysEvent()
        {
            if (runManager?.State == null || eventSelector == null)
            {
                return;
            }

            choiceLocked = false;
            var previousId = pendingEventId;
            var selected = eventSelector.SelectRerollAlternative(
                runManager.State,
                runManager.Days,
                previousId);

            if (selected == null)
            {
                // 대안이 없으면 폴백이라도 현재와 다를 때만 적용
                if (fallbackEvent != null
                    && (string.IsNullOrEmpty(previousId) || fallbackEvent.Id != previousId))
                {
                    selected = fallbackEvent;
                }
            }

            if (selected == null)
            {
                Debug.LogWarning(
                    "[GamePlayPresenter] 다른 사건 대안이 없어 현재 사건을 유지합니다.",
                    this);
                RefreshChoiceAdButtons();
                return;
            }

            ShowEvent(selected, replaceActiveChoice: true);
            SaveActiveRun();
        }

        private void OnRetryAdClicked()
        {
            RequestRewarded(RewardedAdPlacement.RetryOutcome, grant =>
            {
                if (grant == null || !grant.Value.RetryOutcome || effectResolver == null)
                {
                    return;
                }

                if (!effectResolver.TryUndoLastChoice(out var error))
                {
                    Debug.LogWarning($"[GamePlayPresenter] Retry undo failed: {error}", this);
                    return;
                }

                choiceLocked = false;
                resultPopupView.Hide();
                RefreshHudInstant();
                var active = effectResolver.ActiveEvent;
                if (active != null)
                {
                    ShowEvent(active);
                }
                else
                {
                    PresentTodaysEvent();
                }
            });
        }

        private void OnSideJobAdClicked()
        {
            RequestRewarded(RewardedAdPlacement.DailySideJob, grant =>
            {
                if (grant == null || runManager?.State == null)
                {
                    return;
                }

                AdRewardApplicator.ApplyCash(runManager.State, grant.Value);
                runManager.State.RegisterSideJobCompletion();
                RefreshHudInstant();
                SaveActiveRun();
                RefreshResultAdButtons(effectResolver?.LastResult);
            });
        }

        private void OnLoanAdClicked()
        {
            RequestRewarded(RewardedAdPlacement.EmergencyLoan, grant =>
            {
                if (grant == null || runManager?.State == null)
                {
                    return;
                }

                AdRewardApplicator.ApplyCash(runManager.State, grant.Value);
                RefreshHudInstant();
                SaveActiveRun();
                RefreshResultAdButtons(effectResolver?.LastResult);
            });
        }

        private void RequestRewarded(
            RewardedAdPlacement placement,
            System.Action<AdRewardGrant?> onGranted)
        {
            var rewarded = AppRoot.Instance?.RewardedAds;
            if (rewarded == null)
            {
                Debug.LogWarning("[GamePlayPresenter] RewardedAds unavailable.", this);
                onGranted?.Invoke(null);
                return;
            }

            rewarded.Request(placement, result =>
            {
                if (!result.RewardGranted)
                {
                    Debug.Log(
                        $"[GamePlayPresenter] Ad not rewarded ({placement}): {result.ShowResult.Status}");
                    RefreshChoiceAdButtons();
                    RefreshResultAdButtons(effectResolver?.LastResult);
                    onGranted?.Invoke(null);
                    return;
                }

                onGranted?.Invoke(result.Reward);
            });
        }

        private IEnumerator AnimateStatsTo(PlayerStats target)
        {
            hudView.HealthGauge?.AnimateTo(target.Health, GaugeAnimSeconds);
            hudView.StressGauge?.AnimateTo(target.Stress, GaugeAnimSeconds);
            hudView.HappinessGauge?.AnimateTo(target.Happiness, GaugeAnimSeconds);
            hudView.CompanyGauge?.AnimateTo(target.CompanyScore, GaugeAnimSeconds);
            yield return new WaitForSecondsRealtime(GaugeAnimSeconds);
        }

        private void OnNextDayClicked()
        {
            if (waitingSalaryConfirm)
            {
                waitingSalaryConfirm = false;
                return;
            }

            if (advancing || effectResolver == null || runManager == null)
            {
                return;
            }

            if (!effectResolver.CanAdvanceDay)
            {
                return;
            }

            advancing = true;
            resultPopupView.SetNextDayInteractable(false);

            var advance = runManager.TryCompleteCurrentDayAfterChoice(effectResolver);
            if (!advance.Accepted)
            {
                Debug.LogWarning($"[GamePlayPresenter] Advance rejected: {advance.Message}", this);
                advancing = false;
                resultPopupView.SetNextDayInteractable(true);
                return;
            }

            if (advance.RunFailed)
            {
                return;
            }

            if (advance.RunSucceeded)
            {
                // OnRunSucceeded 코루틴에서 월급 연출 후 Result로 이동
                return;
            }

            resultPopupView.Hide();

            if (advance.WeeklySummaryTriggered && lastWeeklySummary != null)
            {
                ShowWeeklySummaryPopup(lastWeeklySummary);
                advancing = false;
                return;
            }

            PresentTodaysEvent();
        }

        private void OnWeeklySummary(WeeklySummaryInfo info)
        {
            lastWeeklySummary = info;
        }

        private void ShowWeeklySummaryPopup(WeeklySummaryInfo info)
        {
            if (weeklySummaryPopupView == null)
            {
                Debug.LogWarning("[GamePlayPresenter] weeklySummaryPopupView missing. Skipping weekly UI.");
                PresentTodaysEvent();
                return;
            }

            waitingWeeklyContinue = true;
            weeklySummaryPopupView.Show(
                WeeklySummaryFormatter.BuildTitle(info),
                WeeklySummaryFormatter.BuildBody(info),
                WeeklySummaryFormatter.BuildWarnings(info),
                "다음 주로");
            UiModalLayer.RestackModalsAboveHud(
                hudView != null ? hudView.transform : null,
                resultPopupView,
                weeklySummaryPopupView);
        }

        private void OnWeeklySummaryContinue()
        {
            if (!waitingWeeklyContinue)
            {
                return;
            }

            waitingWeeklyContinue = false;
            weeklySummaryPopupView?.Hide();
            PresentTodaysEvent();
        }

        private void OnDayStarted(GameState state, int day)
        {
            if (AppRoot.Instance?.AdQuota != null)
            {
                AppRoot.Instance.AdQuota.SetGameDay(day);
            }

            var analytics = AppRoot.Instance?.Analytics;
            if (analytics != null && state != null)
            {
                analytics.DayStarted(day, state.Stats.Cash);
            }

            RefreshHudInstant();
            RefreshPlayBgm();
        }

        private void OnRunSucceeded(GameState state)
        {
            StartCoroutine(ShowSalaryThenResultRoutine(state));
        }

        private IEnumerator ShowSalaryThenResultRoutine(GameState state)
        {
            advancing = true;
            resultPopupView?.Hide();
            weeklySummaryPopupView?.Hide();

            if (state != null)
            {
                var before = state.Stats.Cash;
                var salary = Math.Max(0L, state.Salary);
                if (salary > 0L)
                {
                    state.ApplyEffect(new StatEffect(StatType.Cash, salary));
                    AppRoot.Instance?.Audio?.PlaySfx(SfxId.Payday);
                }

                var after = state.Stats.Cash;
                RefreshHudInstant();

                if (resultPopupView != null)
                {
                    resultPopupView.Show(
                        "월급 입금",
                        $"월급날입니다.\n{KoreanWonFormatter.Format(salary)}이 통장에 들어왔습니다.",
                        $"현금 {KoreanWonFormatter.Format(before)} → {KoreanWonFormatter.Format(after)}",
                        "결과 보기");
                    resultPopupView.SetAdButtons(false, false, false, false, false, false);
                    waitingSalaryConfirm = true;
                    resultPopupView.SetNextDayInteractable(true);
                    while (waitingSalaryConfirm)
                    {
                        yield return null;
                    }

                    resultPopupView.Hide();
                }
            }

            var analytics = AppRoot.Instance?.Analytics;
            analytics?.RunCompleted(
                state != null ? state.CurrentDay : 0,
                state != null ? state.Stats.Cash : 0L,
                isSuccess: true);
            PublishResult(state, isSuccess: true, FailureReason.None);
            LoadResultScene();
        }

        private void OnRunFailed(GameState state, FailureReason reason)
        {
            Debug.Log($"[GamePlayPresenter] Run failed: {reason}");
            var analytics = AppRoot.Instance?.Analytics;
            if (analytics != null && state != null)
            {
                analytics.RunFailed(reason, state.CurrentDay, state.Stats.Cash);
                analytics.RunCompleted(state.CurrentDay, state.Stats.Cash, isSuccess: false);
            }

            PublishResult(state, isSuccess: false, reason);
            LoadResultScene();
        }

        private void PublishResult(
            GameState state,
            bool isSuccess,
            FailureReason failureReason,
            EndingData forcedEnding = null)
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var session = appRoot.Session;
            if (session == null)
            {
                Debug.LogError("[GamePlayPresenter] GameSession is missing.", this);
                return;
            }

            if (session.EndingCatalog.Count == 0 && endingCatalog != null && endingCatalog.Count > 0)
            {
                session.SetEndingCatalog(endingCatalog, fallbackSuccessEnding);
            }

            EndingData ending = forcedEnding;
            if (ending == null)
            {
                var evaluator = session.CreateEndingEvaluator();
                ending = evaluator.Evaluate(state, isSuccess, failureReason);
            }

            var draft = ResultData.Create(state, isSuccess, failureReason, ending);

            var traitsForUnlock = allTraits != null && allTraits.Count > 0
                ? allTraits
                : (startingTrait != null
                    ? new List<TraitData> { startingTrait }
                    : new List<TraitData>());

            var jobsForUnlock = allJobs != null && allJobs.Count > 0
                ? allJobs
                : (startingJob != null
                    ? new List<JobData> { startingJob }
                    : new List<JobData>());

            var metaResult = session.Meta.ApplyRunResult(
                draft,
                traitsForUnlock,
                discoveredEventIdsThisRun,
                jobsForUnlock,
                eventCatalog);

            ApplyDailyProgress(
                session,
                state,
                isSuccess,
                draft,
                metaResult,
                isDailyChallengeRun,
                traitsForUnlock,
                jobsForUnlock);

            session.SyncTraitFragmentsFromMeta();
            session.LastResult = draft.WithMeta(metaResult);

            appRoot.ClearActiveRunAndSave();
            session.StartMode = GameStartMode.NewRun;
            discoveredEventIdsThisRun.Clear();
            isDailyChallengeRun = false;
        }

        private void ApplyDailyProgress(
            GameSession session,
            GameState state,
            bool isSuccess,
            ResultData draft,
            MetaProgressResult metaResult,
            bool updateDailyBest,
            IEnumerable<TraitData> traitsForUnlock,
            IEnumerable<JobData> jobsForUnlock)
        {
            if (session?.Meta?.Daily == null || state == null || draft == null)
            {
                return;
            }

            var pool = ResolveDailyMissionPool(session);
            session.Meta.Daily.EnsureForLocalDate(pool);
            session.Meta.Daily.BindMissionDefinitions(pool);
            if (updateDailyBest)
            {
                session.Meta.Daily.TryUpdateBest(draft);
            }

            var completed = session.Meta.Daily.ApplyRunToMissions(state, isSuccess, session.Meta);
            session.Meta.RefreshUnlocksFromLevel(traitsForUnlock, jobsForUnlock, metaResult);
            if (metaResult == null)
            {
                return;
            }

            metaResult.LoginStreak = session.Meta.Daily.LoginStreak;
            metaResult.TotalExperience = session.Meta.TotalExperience;
            metaResult.LevelAfter = session.Meta.Level;
            for (var i = 0; i < completed.Count; i++)
            {
                var mission = completed[i];
                if (mission == null)
                {
                    continue;
                }

                metaResult.NewlyCompletedDailyMissionTitles.Add(
                    string.IsNullOrWhiteSpace(mission.Title) ? mission.Id : mission.Title);
                metaResult.DailyMissionExperienceGained += Math.Max(0, mission.RewardExperience);
            }
        }

        private List<DailyMissionData> ResolveDailyMissionPool(GameSession session)
        {
            if (dailyMissionPool != null && dailyMissionPool.Count > 0)
            {
                return dailyMissionPool;
            }

            if (session?.DailyMissionPool != null && session.DailyMissionPool.Count > 0)
            {
                return session.DailyMissionPool;
            }

            return DailyMissionDefaults.CreateRuntimePool();
        }

        private void LoadResultScene()
        {
            if (AppRoot.Instance == null || AppRoot.Instance.SceneLoader == null)
            {
                Debug.LogError("[GamePlayPresenter] SceneLoader unavailable.", this);
                return;
            }

            AppRoot.Instance.SceneLoader.LoadResult();
        }

        private void RefreshHudInstant()
        {
            if (runManager?.State == null || hudView == null)
            {
                return;
            }

            var state = runManager.State;
            var stats = state.Stats;
            hudView.SetDayText(DayDisplayFormatter.Format(state.CurrentDay, runManager.Days.CurrentDayOfWeek));
            hudView.SetCash(stats.Cash);
            hudView.HealthGauge?.SetValueInstant(stats.Health);
            hudView.StressGauge?.SetValueInstant(stats.Stress);
            hudView.HappinessGauge?.SetValueInstant(stats.Happiness);
            hudView.CompanyGauge?.SetValueInstant(stats.CompanyScore);
            RefreshCrisis(stats);
        }

        private void RefreshCrisis(PlayerStats stats)
        {
            var messages = new List<string>();
            if (stats.Health <= HealthWarning)
            {
                messages.Add(CrisisWarningCopy.HealthWarning);
            }

            if (stats.Stress >= StressWarning)
            {
                messages.Add(CrisisWarningCopy.StressWarning);
            }

            if (stats.CompanyScore <= CompanyWarning)
            {
                messages.Add(CrisisWarningCopy.CompanyWarning);
            }

            if (stats.Cash < CrisisWarningCopy.CriticalCashThreshold)
            {
                messages.Add(CrisisWarningCopy.CriticalCash);
            }
            else if (stats.Cash < CrisisWarningCopy.LowCashThreshold)
            {
                messages.Add(CrisisWarningCopy.LowCash);
            }

            if (runManager?.Days != null && runManager.Days.IsLateCrisisDay())
            {
                messages.Insert(0, CrisisWarningCopy.LateCrisis);
            }

            hudView.SetCrisis(messages.Count > 0, string.Join(" · ", messages));
        }

        private void RefreshPlayBgm()
        {
            var audio = AppRoot.Instance?.Audio;
            if (audio == null || runManager?.State == null)
            {
                return;
            }

            audio.SetBgm(GameAudioRules.ResolvePlayBgm(runManager.State, runManager.Days));
        }

        private static string BuildChangesText(ChoiceResult result)
        {
            if (result.StatChanges == null || result.StatChanges.Count == 0)
            {
                return EmptyStateCopy.NoStatChanges;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < result.StatChanges.Count; i++)
            {
                var change = result.StatChanges[i];
                if (!change.Changed)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                if (change.StatType == StatType.Cash)
                {
                    builder.Append("현금 ");
                    builder.Append(KoreanWonFormatter.FormatDelta(change.ActualDelta));
                }
                else
                {
                    var sign = change.ActualDelta > 0 ? "+" : string.Empty;
                    builder.Append(GetStatDisplayName(change.StatType));
                    builder.Append(' ');
                    builder.Append(sign);
                    builder.Append(change.ActualDelta);
                }
            }

            var numbers = builder.Length > 0 ? builder.ToString() : EmptyStateCopy.NoStatChanges;
            var drama = ChoiceFeedbackCopy.BuildDramaLine(result);
            return string.IsNullOrEmpty(drama) ? numbers : numbers + "\n" + drama;
        }

        private static string GetStatDisplayName(StatType statType)
        {
            return StatCopy.GetDisplayName(statType);
        }

        private bool ValidateReferences()
        {
            var ok = true;
            if (hudView == null)
            {
                Debug.LogError("[GamePlayPresenter] hudView is not assigned.", this);
                ok = false;
            }

            if (eventPanelView == null)
            {
                Debug.LogError("[GamePlayPresenter] eventPanelView is not assigned.", this);
                ok = false;
            }

            if (choicePanelView == null)
            {
                Debug.LogError("[GamePlayPresenter] choicePanelView is not assigned.", this);
                ok = false;
            }

            if (resultPopupView == null)
            {
                Debug.LogError("[GamePlayPresenter] resultPopupView is not assigned.", this);
                ok = false;
            }

            if (weeklySummaryPopupView == null)
            {
                Debug.LogWarning(
                    "[GamePlayPresenter] weeklySummaryPopupView is not assigned. Run Tools > Setup Weekly Summary Popup (Unit 19).",
                    this);
            }

            return ok;
        }

        private void TrackRunStarted(bool continued)
        {
            var analytics = AppRoot.Instance?.Analytics;
            if (analytics == null || runManager?.State == null)
            {
                return;
            }

            analytics.RunStarted(
                runManager.State.JobId,
                runManager.State.TraitId,
                runManager.State.RandomSeed,
                runManager.State.CurrentDay,
                continued);
        }

        public GameState DebugGetState()
        {
            return runManager != null ? runManager.State : null;
        }

        public void DebugSetDay(int day)
        {
            if (runManager?.Days == null || effectResolver == null)
            {
                Debug.LogWarning("[GamePlayPresenter] DebugSetDay: run not ready.", this);
                return;
            }

            var clamped = Mathf.Clamp(day, GameState.MinDay, GameState.MaxDay);
            runManager.Days.SetDay(clamped);
            choiceLocked = false;
            advancing = false;
            resultPopupView?.Hide();
            PresentTodaysEvent();
            RefreshHudInstant();
        }

        public void DebugSetStats(long cash, int health, int stress, int happiness, int companyScore)
        {
            var state = DebugGetState();
            if (state == null)
            {
                return;
            }

            state.Stats.Cash = cash;
            state.Stats.Health = StatLimits.ClampGauge(health);
            state.Stats.Stress = StatLimits.ClampGauge(stress);
            state.Stats.Happiness = StatLimits.ClampGauge(happiness);
            state.Stats.CompanyScore = StatLimits.ClampGauge(companyScore);
            RefreshHudInstant();
            SaveActiveRun();
        }

        public void DebugAdjustCash(long delta)
        {
            var state = DebugGetState();
            if (state == null)
            {
                return;
            }

            state.Stats.Cash += delta;
            RefreshHudInstant();
            SaveActiveRun();
        }

        public void DebugSetSeed(int seed)
        {
            var state = DebugGetState();
            if (state == null || runManager == null)
            {
                return;
            }

            randomSeed = seed;
            state.RandomSeed = seed;
            randomService = new SeededRandomService(seed);
            eventSelector = new EventSelector(eventCatalog, fallbackEvent, randomService);
            effectResolver = new EffectResolver(
                state,
                randomService,
                runHistory ?? new RunHistory(),
                runManager.Days,
                ResolveTraitById(state.TraitId));
            choiceLocked = false;
            advancing = false;
            resultPopupView?.Hide();
            PresentTodaysEvent();
            SaveActiveRun();
        }

        public void DebugForceEvent(EventData eventData)
        {
            if (eventData == null || effectResolver == null || runManager == null)
            {
                return;
            }

            choiceLocked = false;
            advancing = false;
            ShowEvent(eventData);
            SaveActiveRun();
        }

        public void DebugForceEnding(EndingData ending)
        {
            if (ending == null || runManager?.State == null)
            {
                return;
            }

            var isSuccess = !ending.IsFailureEnding;
            var reason = ending.IsFailureEnding ? ending.LinkedFailureReason : FailureReason.None;
            PublishResult(runManager.State, isSuccess, reason, ending);
            LoadResultScene();
        }

        public void DebugForceSuccess()
        {
            if (runManager?.State == null)
            {
                return;
            }

            PublishResult(runManager.State, isSuccess: true, FailureReason.None);
            LoadResultScene();
        }

        public void DebugForceFailure(FailureReason reason)
        {
            if (runManager?.State == null)
            {
                return;
            }

            if (reason == FailureReason.None)
            {
                reason = FailureReason.Bankruptcy;
            }

            PublishResult(runManager.State, isSuccess: false, reason);
            LoadResultScene();
        }

        public void DebugSetFlag(string flagId, bool enabled)
        {
            var state = DebugGetState();
            if (state == null || string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            if (enabled)
            {
                state.SetFlag(flagId);
            }
            else
            {
                state.ClearFlag(flagId);
            }

            SaveActiveRun();
        }

        public void DebugClearFlags()
        {
            var state = DebugGetState();
            if (state == null)
            {
                return;
            }

            state.ClearRunFlags();
            SaveActiveRun();
        }

        public IReadOnlyList<string> DebugGetFlags()
        {
            var state = DebugGetState();
            if (state?.RunFlags == null)
            {
                return System.Array.Empty<string>();
            }

            return state.RunFlags;
        }

        public string DebugBuildStateDump()
        {
            var state = DebugGetState();
            if (state == null)
            {
                return "run not ready";
            }

            var flags = state.RunFlags;
            var flagText = flags == null || flags.Count == 0
                ? "(none)"
                : string.Join(", ", flags);
            var stats = state.Stats;
            return
                $"Day={state.CurrentDay} Seed={state.RandomSeed} " +
                $"Cash={stats.Cash} H={stats.Health} S={stats.Stress} " +
                $"Hp={stats.Happiness} Co={stats.CompanyScore} Flags=[{flagText}]";
        }
    }
}
