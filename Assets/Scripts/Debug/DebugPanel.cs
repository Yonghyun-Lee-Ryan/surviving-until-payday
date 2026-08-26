using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace SurviveUntilPayday.DebugTools
{
    /// <summary>
    /// 인게임 디버그 패널. Release 빌드에서는 Awake에서 즉시 제거된다.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        private static readonly string[] KnownFlags =
        {
            RunFlags.HasBoughtStock,
            RunFlags.StockBigWin,
            RunFlags.PhoneStillCracked,
            RunFlags.OwesDebt,
            RunFlags.OrderedDelivery
        };

        private static readonly FailureReason[] FailureOptions =
        {
            FailureReason.Bankruptcy,
            FailureReason.Hospitalization,
            FailureReason.Burnout,
            FailureReason.Fired
        };

        [SerializeField] private GamePlayPresenter presenter;
        [SerializeField] private GameObject panelRoot;

        [Header("Inputs")]
        [SerializeField] private InputField dayInput;
        [SerializeField] private InputField cashInput;
        [SerializeField] private InputField healthInput;
        [SerializeField] private InputField stressInput;
        [SerializeField] private InputField happinessInput;
        [SerializeField] private InputField companyInput;
        [SerializeField] private InputField seedInput;
        [SerializeField] private InputField eventFilterInput;
        [SerializeField] private InputField endingFilterInput;
        [SerializeField] private Dropdown eventDropdown;
        [SerializeField] private Dropdown endingDropdown;
        [SerializeField] private Dropdown failureDropdown;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text flagsSummaryLabel;
        [SerializeField] private List<Toggle> flagToggles = new List<Toggle>();
        [SerializeField] private List<string> flagToggleIds = new List<string>();

        [Header("Catalog")]
        [SerializeField] private List<EventData> events = new List<EventData>();
        [SerializeField] private List<EndingData> endings = new List<EndingData>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Key toggleKey = Key.F1;
#endif

        private readonly List<int> filteredEventIndices = new List<int>();
        private readonly List<int> filteredEndingIndices = new List<int>();
        private bool suppressFlagToggleEvents;

        private void Awake()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            DestroyReleaseDebugChrome();
            Destroy(gameObject);
            return;
#else
            if (presenter == null)
            {
                presenter = FindAnyObjectByType<GamePlayPresenter>();
            }

            if (DebugPanelRuntimeBuilder.NeedsRebuild(this))
            {
                DebugPanelRuntimeBuilder.Rebuild(this, presenter);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            WireFilterListeners();
            WireFlagToggleListeners();
            RebuildFailureDropdown();
            RebuildDropdowns();
#endif
        }

        /// <summary>Editor/Development에서만 살아 남는다. Release 플레이어는 Awake에서 제거된다.</summary>
        public static bool IsIncludedInThisBuild =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        public bool HasRequiredBindings()
        {
            return eventFilterInput != null
                   && endingFilterInput != null
                   && failureDropdown != null
                   && dayInput != null
                   && cashInput != null;
        }

        private static void DestroyReleaseDebugChrome()
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas == null)
                {
                    continue;
                }

                var hint = canvas.transform.Find("DebugHint");
                if (hint != null)
                {
                    Destroy(hint.gameObject);
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
        }
#endif

        public void Bind(
            GamePlayPresenter gamePresenter,
            GameObject root,
            InputField day,
            InputField cash,
            InputField health,
            InputField stress,
            InputField happiness,
            InputField company,
            InputField seed,
            InputField eventFilter,
            InputField endingFilter,
            Dropdown eventDd,
            Dropdown endingDd,
            Dropdown failureDd,
            Text status,
            Text flagsSummary,
            List<Toggle> toggles,
            List<string> toggleIds,
            List<EventData> eventList,
            List<EndingData> endingList)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            presenter = gamePresenter;
            panelRoot = root;
            dayInput = day;
            cashInput = cash;
            healthInput = health;
            stressInput = stress;
            happinessInput = happiness;
            companyInput = company;
            seedInput = seed;
            eventFilterInput = eventFilter;
            endingFilterInput = endingFilter;
            eventDropdown = eventDd;
            endingDropdown = endingDd;
            failureDropdown = failureDd;
            statusLabel = status;
            flagsSummaryLabel = flagsSummary;
            flagToggles = toggles ?? new List<Toggle>();
            flagToggleIds = toggleIds ?? new List<string>();
            events = eventList ?? new List<EventData>();
            endings = endingList ?? new List<EndingData>();
            WireFilterListeners();
            WireFlagToggleListeners();
            RebuildFailureDropdown();
            RebuildDropdowns();
#endif
        }

        public void Toggle()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(!panelRoot.activeSelf);
            if (panelRoot.activeSelf)
            {
                RefreshFromState();
            }
#endif
        }

        public void ApplyDay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access) || !TryParseInt(dayInput, out var day))
            {
                SetStatus("Invalid day");
                return;
            }

            access.DebugSetDay(day);
            SetStatus($"Day -> {day}");
            RefreshFromState();
#endif
        }

        public void JumpDay(int day)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetInput(dayInput, day.ToString());
            ApplyDay();
#endif
        }

        public void JumpDay1() => JumpDay(1);
        public void JumpDay7() => JumpDay(7);
        public void JumpDay14() => JumpDay(14);
        public void JumpDay15() => JumpDay(15);
        public void JumpDay21() => JumpDay(21);
        public void JumpDay30() => JumpDay(30);

        public void ApplyStats()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            if (!TryParseLong(cashInput, out var cash)
                || !TryParseInt(healthInput, out var health)
                || !TryParseInt(stressInput, out var stress)
                || !TryParseInt(happinessInput, out var happiness)
                || !TryParseInt(companyInput, out var company))
            {
                SetStatus("Invalid stats");
                return;
            }

            access.DebugSetStats(cash, health, stress, happiness, company);
            SetStatus("Stats applied");
            RefreshFromState();
#endif
        }

        public void AdjustCash(long delta)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            access.DebugAdjustCash(delta);
            SetStatus(delta >= 0 ? $"Cash +{delta:N0}" : $"Cash {delta:N0}");
            RefreshFromState();
#endif
        }

        public void CashPlus100k() => AdjustCash(100_000L);
        public void CashMinus100k() => AdjustCash(-100_000L);
        public void CashPlus500k() => AdjustCash(500_000L);
        public void CashMinus500k() => AdjustCash(-500_000L);

        public void SetCashZero()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyCashAbsolute(0L);
#endif
        }

        public void SetCashRich()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyCashAbsolute(5_000_000L);
#endif
        }

        public void ApplyPresetCrisis()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyPresetStats(keepCash: true, health: 20, stress: 85, happiness: 20, company: 40, "위기 프리셋");
#endif
        }

        public void ApplyPresetStable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyPresetStats(keepCash: true, health: 80, stress: 25, happiness: 70, company: 70, "안정 프리셋");
#endif
        }

        public void ApplyPresetFiredRisk()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyPresetStats(keepCash: true, health: 55, stress: 60, happiness: 35, company: 15, "해고위기 프리셋");
#endif
        }

        public void ApplySeed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access) || !TryParseInt(seedInput, out var seed))
            {
                SetStatus("Invalid seed");
                return;
            }

            access.DebugSetSeed(seed);
            SetStatus($"Seed -> {seed}");
            RefreshFromState();
#endif
        }

        public void ForceSelectedEvent()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            if (!TryGetFilteredEvent(out var eventData))
            {
                SetStatus("No event selected");
                return;
            }

            access.DebugForceEvent(eventData);
            SetStatus($"Forced event: {eventData.Id}");
#endif
        }

        public void ForceSelectedEnding()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            if (!TryGetFilteredEnding(out var endingData))
            {
                SetStatus("No ending selected");
                return;
            }

            access.DebugForceEnding(endingData);
            SetStatus($"Forced ending: {endingData.Id}");
#endif
        }

        public void ForceSuccess()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            access.DebugForceSuccess();
            SetStatus("Forced success");
#endif
        }

        public void ForceFailBankruptcy()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ForceSelectedFailure();
#endif
        }

        public void ForceSelectedFailure()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var index = failureDropdown != null ? failureDropdown.value : 0;
            if (index < 0 || index >= FailureOptions.Length)
            {
                index = 0;
            }

            var reason = FailureOptions[index];
            access.DebugForceFailure(reason);
            SetStatus($"Forced fail: {reason}");
#endif
        }

        public void ClearAllFlags()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            access.DebugClearFlags();
            SetStatus("Flags cleared");
            RefreshFromState();
#endif
        }

        public void LogStateDump()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var dump = access.DebugBuildStateDump();
            SetStatus(dump);
#endif
        }

        public void OnEventFilterChanged(string _)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RebuildEventDropdown();
#endif
        }

        public void OnEndingFilterChanged(string _)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RebuildEndingDropdown();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void ApplyCashAbsolute(long cash)
        {
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var state = access.DebugGetState();
            if (state == null)
            {
                return;
            }

            access.DebugSetStats(
                cash,
                state.Stats.Health,
                state.Stats.Stress,
                state.Stats.Happiness,
                state.Stats.CompanyScore);
            SetStatus($"Cash -> {cash:N0}");
            RefreshFromState();
        }

        private void ApplyPresetStats(
            bool keepCash,
            int health,
            int stress,
            int happiness,
            int company,
            string label)
        {
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var state = access.DebugGetState();
            if (state == null)
            {
                return;
            }

            var cash = keepCash ? state.Stats.Cash : 0L;
            access.DebugSetStats(cash, health, stress, happiness, company);
            SetStatus(label);
            RefreshFromState();
        }

        private void WireFilterListeners()
        {
            if (eventFilterInput != null)
            {
                eventFilterInput.onValueChanged.RemoveListener(OnEventFilterChanged);
                eventFilterInput.onValueChanged.AddListener(OnEventFilterChanged);
            }

            if (endingFilterInput != null)
            {
                endingFilterInput.onValueChanged.RemoveListener(OnEndingFilterChanged);
                endingFilterInput.onValueChanged.AddListener(OnEndingFilterChanged);
            }
        }

        private void WireFlagToggleListeners()
        {
            for (var i = 0; i < flagToggles.Count; i++)
            {
                var toggle = flagToggles[i];
                if (toggle == null)
                {
                    continue;
                }

                var flagId = i < flagToggleIds.Count ? flagToggleIds[i] : null;
                toggle.onValueChanged.RemoveAllListeners();
                if (string.IsNullOrEmpty(flagId))
                {
                    continue;
                }

                var capturedId = flagId;
                toggle.onValueChanged.AddListener(isOn => OnFlagToggleChanged(capturedId, isOn));
            }
        }

        private void OnFlagToggleChanged(string flagId, bool isOn)
        {
            if (suppressFlagToggleEvents)
            {
                return;
            }

            if (!TryGetAccess(out var access))
            {
                return;
            }

            access.DebugSetFlag(flagId, isOn);
            SetStatus($"Flag {flagId}={(isOn ? "ON" : "OFF")}");
            RefreshFlagsSummary(access);
        }

        private void RebuildFailureDropdown()
        {
            if (failureDropdown == null)
            {
                return;
            }

            failureDropdown.ClearOptions();
            var options = new List<string>(FailureOptions.Length);
            for (var i = 0; i < FailureOptions.Length; i++)
            {
                options.Add(FailureOptions[i].ToString());
            }

            failureDropdown.AddOptions(options);
        }

        private void RebuildDropdowns()
        {
            RebuildEventDropdown();
            RebuildEndingDropdown();
        }

        private void RebuildEventDropdown()
        {
            filteredEventIndices.Clear();
            if (eventDropdown == null)
            {
                return;
            }

            var filter = eventFilterInput != null ? eventFilterInput.text : string.Empty;
            eventDropdown.ClearOptions();
            var options = new List<string>();
            for (var i = 0; i < events.Count; i++)
            {
                var data = events[i];
                if (data == null)
                {
                    continue;
                }

                var label = $"{data.Title} ({data.Id})";
                if (!PassesFilter(filter, data.Id, data.Title, label))
                {
                    continue;
                }

                filteredEventIndices.Add(i);
                options.Add(label);
            }

            eventDropdown.AddOptions(options);
        }

        private void RebuildEndingDropdown()
        {
            filteredEndingIndices.Clear();
            if (endingDropdown == null)
            {
                return;
            }

            var filter = endingFilterInput != null ? endingFilterInput.text : string.Empty;
            endingDropdown.ClearOptions();
            var options = new List<string>();
            for (var i = 0; i < endings.Count; i++)
            {
                var data = endings[i];
                if (data == null)
                {
                    continue;
                }

                var label = $"{data.Title} ({data.Id})";
                if (!PassesFilter(filter, data.Id, data.Title, label))
                {
                    continue;
                }

                filteredEndingIndices.Add(i);
                options.Add(label);
            }

            endingDropdown.AddOptions(options);
        }

        private static bool PassesFilter(string filter, string id, string title, string label)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return (id != null && id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (title != null && title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (label != null && label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool TryGetFilteredEvent(out EventData eventData)
        {
            eventData = null;
            var index = eventDropdown != null ? eventDropdown.value : -1;
            if (index < 0 || index >= filteredEventIndices.Count)
            {
                return false;
            }

            var catalogIndex = filteredEventIndices[index];
            if (catalogIndex < 0 || catalogIndex >= events.Count)
            {
                return false;
            }

            eventData = events[catalogIndex];
            return eventData != null;
        }

        private bool TryGetFilteredEnding(out EndingData endingData)
        {
            endingData = null;
            var index = endingDropdown != null ? endingDropdown.value : -1;
            if (index < 0 || index >= filteredEndingIndices.Count)
            {
                return false;
            }

            var catalogIndex = filteredEndingIndices[index];
            if (catalogIndex < 0 || catalogIndex >= endings.Count)
            {
                return false;
            }

            endingData = endings[catalogIndex];
            return endingData != null;
        }

        private void RefreshFromState()
        {
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var state = access.DebugGetState();
            if (state == null)
            {
                return;
            }

            SetInput(dayInput, state.CurrentDay.ToString());
            SetInput(cashInput, state.Stats.Cash.ToString());
            SetInput(healthInput, state.Stats.Health.ToString());
            SetInput(stressInput, state.Stats.Stress.ToString());
            SetInput(happinessInput, state.Stats.Happiness.ToString());
            SetInput(companyInput, state.Stats.CompanyScore.ToString());
            SetInput(seedInput, state.RandomSeed.ToString());
            RefreshFlagToggles(access);
            RefreshFlagsSummary(access);
        }

        private void RefreshFlagToggles(IGameDebugAccess access)
        {
            suppressFlagToggleEvents = true;
            try
            {
                for (var i = 0; i < flagToggles.Count; i++)
                {
                    var toggle = flagToggles[i];
                    if (toggle == null || i >= flagToggleIds.Count)
                    {
                        continue;
                    }

                    toggle.isOn = access.DebugGetState() != null
                                  && access.DebugGetState().HasFlag(flagToggleIds[i]);
                }
            }
            finally
            {
                suppressFlagToggleEvents = false;
            }
        }

        private void RefreshFlagsSummary(IGameDebugAccess access)
        {
            if (flagsSummaryLabel == null)
            {
                return;
            }

            var flags = access.DebugGetFlags();
            flagsSummaryLabel.text = flags == null || flags.Count == 0
                ? "Flags: (none)"
                : "Flags: " + string.Join(", ", flags);
        }

        private bool TryGetAccess(out IGameDebugAccess access)
        {
            access = presenter as IGameDebugAccess;
            if (access == null)
            {
                SetStatus("Presenter debug access missing");
                return false;
            }

            return true;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }

            Debug.Log($"[DebugPanel] {message}");
        }

        private static void SetInput(InputField field, string value)
        {
            if (field != null)
            {
                field.text = value;
            }
        }

        private static bool TryParseInt(InputField field, out int value)
        {
            value = 0;
            return field != null && int.TryParse(field.text, out value);
        }

        private static bool TryParseLong(InputField field, out long value)
        {
            value = 0;
            return field != null && long.TryParse(field.text, out value);
        }

        public static IReadOnlyList<string> GetKnownFlagIds() => KnownFlags;

        public List<EventData> GetEventCatalogCopy()
        {
            return events != null ? new List<EventData>(events) : new List<EventData>();
        }

        public List<EndingData> GetEndingCatalogCopy()
        {
            return endings != null ? new List<EndingData>(endings) : new List<EndingData>();
        }
#endif
    }
}
