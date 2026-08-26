using System;
using System.Collections.Generic;
using SurviveUntilPayday.Core;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 한 회차의 런타임 상태. UI를 모르며, 능력치 변경은 ApplyEffect(s)로만 수행한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const int MinDay = 1;
        public const int MaxDay = 30;

        [SerializeField] private int currentDay = MinDay;
        [SerializeField] private string jobId = string.Empty;
        [SerializeField] private string traitId = string.Empty;
        [SerializeField] private long salary;
        [SerializeField] private PlayerStats stats = new PlayerStats();
        [SerializeField] private int randomSeed;
        [SerializeField] private List<string> runFlags = new List<string>();
        [SerializeField] private List<string> queuedFollowUpEventIds = new List<string>();
        [SerializeField] private int sideJobCount;

        public int CurrentDay
        {
            get => currentDay;
            set => currentDay = value;
        }

        public string JobId
        {
            get => jobId;
            set => jobId = value ?? string.Empty;
        }

        public string TraitId
        {
            get => traitId;
            set => traitId = value ?? string.Empty;
        }

        public long Salary
        {
            get => salary;
            set => salary = value;
        }

        public PlayerStats Stats => stats ??= new PlayerStats();

        public int RandomSeed
        {
            get => randomSeed;
            set => randomSeed = value;
        }

        public int SideJobCount
        {
            get => sideJobCount;
            set => sideJobCount = Math.Max(0, value);
        }

        public IReadOnlyList<string> RunFlags => runFlags ??= new List<string>();

        public IReadOnlyList<string> QueuedFollowUpEventIds =>
            queuedFollowUpEventIds ??= new List<string>();

        /// <summary>
        /// 능력치가 적용된 뒤 발행한다. UI는 이 이벤트를 구독한다.
        /// </summary>
        public event Action<GameState, IReadOnlyList<StatChangeResult>> StatsChanged;

        /// <summary>
        /// Apply 이후 실패 상태가 None이 아닐 때 발행한다.
        /// </summary>
        public event Action<GameState, FailureReason> FailureDetected;

        public GameState()
        {
        }

        public static GameState CreateFromJob(JobData job, TraitData trait, int seed)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            var state = new GameState
            {
                currentDay = MinDay,
                jobId = job.Id,
                traitId = trait != null ? trait.Id : string.Empty,
                salary = job.Salary,
                randomSeed = seed,
                stats = job.CreateStartingStats()
            };

            if (trait != null)
            {
                trait.ApplyStartingModifiers(state.Stats);
            }

            state.ClampAllGauges();
            return state;
        }

        /// <summary>
        /// 기존 인스턴스를 직업/특성 기준으로 초기화한다.
        /// </summary>
        public void Initialize(JobData job, TraitData trait, int seed)
        {
            var created = CreateFromJob(job, trait, seed);
            currentDay = created.currentDay;
            jobId = created.jobId;
            traitId = created.traitId;
            salary = created.salary;
            randomSeed = created.randomSeed;
            Stats.CopyFrom(created.Stats);
            ClearRunFlags();
            ClearFollowUpQueue();
            sideJobCount = 0;
        }

        public void RegisterSideJobCompletion()
        {
            sideJobCount = Math.Max(0, sideJobCount) + 1;
        }

        public bool HasFlag(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId) || runFlags == null)
            {
                return false;
            }

            return runFlags.Contains(flagId);
        }

        public void SetFlag(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            runFlags ??= new List<string>();
            if (!runFlags.Contains(flagId))
            {
                runFlags.Add(flagId);
            }
        }

        public void ClearFlag(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId) || runFlags == null)
            {
                return;
            }

            runFlags.Remove(flagId);
        }

        public void ClearRunFlags()
        {
            runFlags ??= new List<string>();
            runFlags.Clear();
        }

        public void LoadRunFlags(IEnumerable<string> flags)
        {
            ClearRunFlags();
            if (flags == null)
            {
                return;
            }

            foreach (var flag in flags)
            {
                SetFlag(flag);
            }
        }

        public void EnqueueFollowUp(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            queuedFollowUpEventIds ??= new List<string>();
            if (!queuedFollowUpEventIds.Contains(eventId))
            {
                queuedFollowUpEventIds.Add(eventId);
            }
        }

        public bool TryDequeueFollowUp(out string eventId)
        {
            eventId = null;
            if (queuedFollowUpEventIds == null || queuedFollowUpEventIds.Count == 0)
            {
                return false;
            }

            eventId = queuedFollowUpEventIds[0];
            queuedFollowUpEventIds.RemoveAt(0);
            return !string.IsNullOrEmpty(eventId);
        }

        public void ClearFollowUpQueue()
        {
            queuedFollowUpEventIds ??= new List<string>();
            queuedFollowUpEventIds.Clear();
        }

        public void LoadFollowUpQueue(IEnumerable<string> eventIds)
        {
            ClearFollowUpQueue();
            if (eventIds == null)
            {
                return;
            }

            foreach (var id in eventIds)
            {
                EnqueueFollowUp(id);
            }
        }

        public GameState Clone()
        {
            var clone = new GameState
            {
                currentDay = currentDay,
                jobId = jobId,
                traitId = traitId,
                salary = salary,
                randomSeed = randomSeed,
                sideJobCount = sideJobCount,
                stats = Stats.Clone()
            };
            clone.LoadRunFlags(runFlags);
            clone.LoadFollowUpQueue(queuedFollowUpEventIds);
            return clone;
        }

        public GameStateSnapshot CreateSnapshot()
        {
            return new GameStateSnapshot(
                currentDay,
                jobId,
                traitId,
                salary,
                randomSeed,
                Stats);
        }

        public void RestoreSnapshot(GameStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            currentDay = snapshot.CurrentDay;
            jobId = snapshot.JobId;
            traitId = snapshot.TraitId;
            salary = snapshot.Salary;
            randomSeed = snapshot.RandomSeed;
            Stats.CopyFrom(snapshot.Stats);
        }

        public StatChangeResult ApplyEffect(StatEffect effect)
        {
            if (effect == null)
            {
                Debug.LogError("[GameState] ApplyEffect effect is null.");
                return default;
            }

            var results = ApplyEffects(new[] { effect });
            return results.Count > 0 ? results[0] : default;
        }

        public IReadOnlyList<StatChangeResult> ApplyEffects(IEnumerable<StatEffect> effects)
        {
            if (effects == null)
            {
                Debug.LogError("[GameState] ApplyEffects effects is null.");
                return Array.Empty<StatChangeResult>();
            }

            var changes = new List<StatChangeResult>();
            foreach (var effect in effects)
            {
                if (effect == null)
                {
                    Debug.LogWarning("[GameState] ApplyEffects skipped null StatEffect.");
                    continue;
                }

                changes.Add(ApplySingleEffect(effect));
            }

            if (changes.Count > 0)
            {
                StatsChanged?.Invoke(this, changes);

                var failure = EvaluateFailure();
                if (failure != FailureReason.None)
                {
                    FailureDetected?.Invoke(this, failure);
                }
            }

            return changes;
        }

        /// <summary>
        /// 실패 우선순위: 파산 → 입원 → 번아웃 → 해고.
        /// </summary>
        public FailureReason EvaluateFailure()
        {
            return FailureEvaluator.Evaluate(this);
        }

        public bool HasFailed => EvaluateFailure() != FailureReason.None;

        public List<FailureReason> GetAllFailureReasons()
        {
            return FailureEvaluator.GetAll(this);
        }

        private StatChangeResult ApplySingleEffect(StatEffect effect)
        {
            var before = Stats.GetStat(effect.StatType);
            var requested = before + effect.Value;
            var after = requested;

            if (StatLimits.IsGaugeStat(effect.StatType))
            {
                after = StatLimits.ClampGauge((int)requested);
            }

            Stats.SetStat(effect.StatType, after);
            return new StatChangeResult(effect.StatType, before, after, effect.Value);
        }

        private void ClampAllGauges()
        {
            Stats.Health = StatLimits.ClampGauge(Stats.Health);
            Stats.Stress = StatLimits.ClampGauge(Stats.Stress);
            Stats.Happiness = StatLimits.ClampGauge(Stats.Happiness);
            Stats.CompanyScore = StatLimits.ClampGauge(Stats.CompanyScore);
        }
    }
}
