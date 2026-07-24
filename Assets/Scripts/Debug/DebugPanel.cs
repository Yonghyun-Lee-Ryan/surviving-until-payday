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
        [SerializeField] private Dropdown eventDropdown;
        [SerializeField] private Dropdown endingDropdown;
        [SerializeField] private Text statusLabel;

        [Header("Catalog")]
        [SerializeField] private List<EventData> events = new List<EventData>();
        [SerializeField] private List<EndingData> endings = new List<EndingData>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Key toggleKey = Key.F1;
#endif

        private void Awake()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            Destroy(gameObject);
            return;
#else
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            RebuildDropdowns();
#endif
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
            Dropdown eventDd,
            Dropdown endingDd,
            Text status,
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
            eventDropdown = eventDd;
            endingDropdown = endingDd;
            statusLabel = status;
            events = eventList ?? new List<EventData>();
            endings = endingList ?? new List<EndingData>();
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
            if (!TryGetAccess(out var access))
            {
                return;
            }

            if (!TryParseInt(dayInput, out var day))
            {
                SetStatus("Invalid day");
                return;
            }

            access.DebugSetDay(day);
            SetStatus($"Day -> {day}");
#endif
        }

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
#endif
        }

        public void ApplySeed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            if (!TryParseInt(seedInput, out var seed))
            {
                SetStatus("Invalid seed");
                return;
            }

            access.DebugSetSeed(seed);
            SetStatus($"Seed -> {seed}");
#endif
        }

        public void ForceSelectedEvent()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var index = eventDropdown != null ? eventDropdown.value : -1;
            if (index < 0 || index >= events.Count || events[index] == null)
            {
                SetStatus("No event selected");
                return;
            }

            access.DebugForceEvent(events[index]);
            SetStatus($"Forced event: {events[index].Id}");
#endif
        }

        public void ForceSelectedEnding()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGetAccess(out var access))
            {
                return;
            }

            var index = endingDropdown != null ? endingDropdown.value : -1;
            if (index < 0 || index >= endings.Count || endings[index] == null)
            {
                SetStatus("No ending selected");
                return;
            }

            access.DebugForceEnding(endings[index]);
            SetStatus($"Forced ending: {endings[index].Id}");
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
            if (!TryGetAccess(out var access))
            {
                return;
            }

            access.DebugForceFailure(FailureReason.Bankruptcy);
            SetStatus("Forced bankruptcy");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RebuildDropdowns()
        {
            if (eventDropdown != null)
            {
                eventDropdown.ClearOptions();
                var options = new List<string>();
                for (var i = 0; i < events.Count; i++)
                {
                    options.Add(events[i] != null ? events[i].Id : $"null_{i}");
                }

                eventDropdown.AddOptions(options);
            }

            if (endingDropdown != null)
            {
                endingDropdown.ClearOptions();
                var options = new List<string>();
                for (var i = 0; i < endings.Count; i++)
                {
                    options.Add(endings[i] != null ? endings[i].Id : $"null_{i}");
                }

                endingDropdown.AddOptions(options);
            }
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
                statusLabel.text = message;
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
#endif
    }
}
