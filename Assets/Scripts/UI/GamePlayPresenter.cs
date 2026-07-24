using System.Collections;
using System.Collections.Generic;
using System.Text;
using SurviveUntilPayday.Ads;
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

        [Header("Run Data")]
        [SerializeField] private JobData startingJob;
        [SerializeField] private TraitData startingTrait;
        [SerializeField] private List<EventData> eventCatalog = new List<EventData>();
        [SerializeField] private EventData fallbackEvent;
        [SerializeField] private int randomSeed = 1;

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
        private string pendingEventId;
        private readonly HashSet<string> discoveredEventIdsThisRun = new HashSet<string>();

        [Header("Meta")]
        [SerializeField] private List<TraitData> allTraits = new List<TraitData>();

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

        public void BindViews(
            GameHudView hud,
            EventPanelView eventPanel,
            ChoicePanelView choicePanel,
            ResultPopupView resultPopup)
        {
            hudView = hud;
            eventPanelView = eventPanel;
            choicePanelView = choicePanel;
            resultPopupView = resultPopup;
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

            randomService = new SeededRandomService(randomSeed);
            runHistory = new RunHistory();
            runManager = new RunManager();
            runManager.StartRun(startingJob, startingTrait, randomSeed);

            eventSelector = new EventSelector(eventCatalog, fallbackEvent, randomService);
            effectResolver = new EffectResolver(
                runManager.State,
                randomService,
                runHistory,
                runManager.Days);

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

            eventSelector = new EventSelector(eventCatalog, fallbackEvent, randomService);
            eventSelector.RestoreHistory(runSave.recentEventIds, runSave.lastSelectedEventId);
            effectResolver = new EffectResolver(
                runManager.State,
                randomService,
                runHistory,
                runManager.Days);

            EnsureSessionEndings();
            SubscribeStateEvents();
            ConfigureGaugeThresholds();
            resultPopupView.Hide();
            RefreshHudInstant();
            TrackRunStarted(continued: true);
            PresentSavedEventOrSelect(runSave.pendingEventId);
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
        }

        private void ConfigureGaugeThresholds()
        {
            hudView.HealthGauge?.ConfigureThresholds(-1, HealthWarning);
            hudView.StressGauge?.ConfigureThresholds(StressWarning, -1);
            hudView.HappinessGauge?.ConfigureThresholds(-1, 20);
            hudView.CompanyGauge?.ConfigureThresholds(-1, CompanyWarning);

            hudView.HealthGauge?.SetName("건강");
            hudView.StressGauge?.SetName("스트레스");
            hudView.HappinessGauge?.SetName("행복도");
            hudView.CompanyGauge?.SetName("회사 평가");
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
            eventPanelView.Show(selected.Title, selected.Description, null);

            var texts = new string[3];
            for (var i = 0; i < 3; i++)
            {
                texts[i] = i < selected.Choices.Count && selected.Choices[i] != null
                    ? selected.Choices[i].Text
                    : string.Empty;
            }

            choicePanelView.SetChoices(texts);
            choicePanelView.SetInteractable(true);
            resultPopupView.Hide();
            RefreshHudInstant();
            RefreshChoiceAdButtons();

            var analytics = AppRoot.Instance?.Analytics;
            if (analytics != null && selected != null && runManager?.State != null)
            {
                analytics.EventShown(selected.Id, runManager.State.CurrentDay);
            }
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
            yield return AnimateStatsTo(result.StatsAfter);

            var changes = BuildChangesText(result);
            var nextLabel = runManager.Days.IsFinalDay ? "결과 보기" : "다음 날";
            resultPopupView.Show("선택 결과", result.Message, changes, nextLabel);
            RefreshCrisis(result.StatsAfter);
            hudView.SetCash(result.StatsAfter.Cash);
            RefreshResultAdButtons(result);
        }

        private void RefreshChoiceAdButtons()
        {
            if (choicePanelView == null)
            {
                return;
            }

            var rewarded = AppRoot.Instance?.RewardedAds;
            var can = rewarded != null
                      && !choiceLocked
                      && effectResolver != null
                      && effectResolver.CanSelectChoice
                      && rewarded.CanRequest(RewardedAdPlacement.ChoiceReroll, out _);
            var remaining = AppRoot.Instance?.AdQuota?.GetRemaining(RewardedAdPlacement.ChoiceReroll) ?? 0;
            choicePanelView.SetRerollVisible(
                visible: remaining > 0 || can,
                interactable: can,
                label: $"광고: 선택지 새로고침 ({remaining})");
        }

        private void RefreshResultAdButtons(ChoiceResult result)
        {
            if (resultPopupView == null)
            {
                return;
            }

            var rewarded = AppRoot.Instance?.RewardedAds;
            var quota = AppRoot.Instance?.AdQuota;
            var canRetry = rewarded != null && rewarded.CanRequest(RewardedAdPlacement.RetryOutcome, out _);
            var canSide = rewarded != null && rewarded.CanRequest(RewardedAdPlacement.DailySideJob, out _);
            var cash = result?.StatsAfter != null ? result.StatsAfter.Cash : runManager?.State?.Stats.Cash ?? 0;
            var needsLoan = cash < 50_000L
                            || (result != null && result.FailureAfter == FailureReason.Bankruptcy);
            var canLoan = needsLoan
                          && rewarded != null
                          && rewarded.CanRequest(RewardedAdPlacement.EmergencyLoan, out _);

            resultPopupView.SetAdButtons(
                retryVisible: quota == null || quota.GetRemaining(RewardedAdPlacement.RetryOutcome) > 0,
                retryInteractable: canRetry,
                sideJobVisible: quota == null || quota.GetRemaining(RewardedAdPlacement.DailySideJob) > 0,
                sideJobInteractable: canSide,
                loanVisible: needsLoan,
                loanInteractable: canLoan);
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
        /// 광고 선택지 새로고침: 같은 날 다른 사건으로 교체한다(의도적 BeginEvent 교체).
        /// </summary>
        private void RerollTodaysEvent()
        {
            if (runManager?.State == null || eventSelector == null)
            {
                return;
            }

            choiceLocked = false;
            var previousId = pendingEventId;
            EventData selected = null;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidate = eventSelector.Select(runManager.State, runManager.Days);
                if (candidate == null)
                {
                    continue;
                }

                selected = candidate;
                if (string.IsNullOrEmpty(previousId) || candidate.Id != previousId)
                {
                    break;
                }
            }

            if (selected == null)
            {
                selected = fallbackEvent;
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

            if (advance.RunFailed || advance.RunSucceeded)
            {
                // Scene 이동은 이벤트 핸들러에서 처리
                return;
            }

            resultPopupView.Hide();
            PresentTodaysEvent();
            // PresentTodaysEvent 내부에서 자동 저장
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
        }

        private void OnRunSucceeded(GameState state)
        {
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

            var metaResult = session.Meta.ApplyRunResult(draft, traitsForUnlock, discoveredEventIdsThisRun);
            session.LastResult = draft.WithMeta(metaResult);

            appRoot.ClearActiveRunAndSave();
            session.StartMode = GameStartMode.NewRun;
            discoveredEventIdsThisRun.Clear();
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
                messages.Add("건강 위험");
            }

            if (stats.Stress >= StressWarning)
            {
                messages.Add("스트레스 경고");
            }

            if (stats.CompanyScore <= CompanyWarning)
            {
                messages.Add("해고 위기");
            }

            hudView.SetCrisis(messages.Count > 0, string.Join(" · ", messages));
        }

        private static string BuildChangesText(ChoiceResult result)
        {
            if (result.StatChanges == null || result.StatChanges.Count == 0)
            {
                return "능력치 변화 없음";
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

            return builder.Length > 0 ? builder.ToString() : "능력치 변화 없음";
        }

        private static string GetStatDisplayName(StatType statType)
        {
            switch (statType)
            {
                case StatType.Health:
                    return "건강";
                case StatType.Stress:
                    return "스트레스";
                case StatType.Happiness:
                    return "행복도";
                case StatType.CompanyScore:
                    return "회사 평가";
                case StatType.Cash:
                    return "현금";
                default:
                    return statType.ToString();
            }
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
                runManager.Days);
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
    }
}
