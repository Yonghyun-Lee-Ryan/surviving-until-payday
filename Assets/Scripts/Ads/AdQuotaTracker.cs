using System;
using System.Collections.Generic;
using System.Globalization;

namespace SurviveUntilPayday.Ads
{
    /// <summary>
    /// 회차/일별 광고 횟수 제한과 글로벌 쿨다운.
    /// </summary>
    public sealed class AdQuotaTracker
    {
        public const int ChoiceRerollLimitPerRun = 2;
        public const int RetryOutcomeLimitPerRun = 1;
        public const int EmergencyLoanLimitPerRun = 1;
        public const int DailySideJobLimitPerDay = 1;
        public const int DoubleExperienceLimitPerRun = 1;
        public const int TraitFragmentLimitPerDay = 3;
        public const double DefaultCooldownSeconds = 2.0;

        private readonly Dictionary<RewardedAdPlacement, int> runUsage =
            new Dictionary<RewardedAdPlacement, int>();

        private readonly IAdClock clock;
        private readonly double cooldownSeconds;

        private int currentGameDay = 1;
        private int sideJobUsesToday;
        private int traitFragmentUsesToday;
        private string traitFragmentDateKey = string.Empty;
        private double lastRewardedUtcSeconds = double.NegativeInfinity;
        private RewardedAdPlacement? lastRewardedPlacement;
        private bool rewardedRecentlyForInterstitial;

        public AdQuotaTracker(IAdClock clock = null, double cooldownSeconds = DefaultCooldownSeconds)
        {
            this.clock = clock ?? new SystemAdClock();
            if (cooldownSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            }

            this.cooldownSeconds = cooldownSeconds;
        }

        public double CooldownSeconds => cooldownSeconds;

        public bool HasRewardedRecentlyForInterstitial => rewardedRecentlyForInterstitial;

        /// <summary>무료 상점 특성 조각 — 캘린더 일자 키.</summary>
        public string TraitFragmentDateKey => traitFragmentDateKey;

        public int TraitFragmentUsedToday => traitFragmentUsesToday;

        public void BeginRun()
        {
            runUsage.Clear();
            currentGameDay = 1;
            sideJobUsesToday = 0;
            // TraitFragment는 캘린더 일 기준이므로 회차 시작으로 초기화하지 않는다.
            lastRewardedPlacement = null;
            rewardedRecentlyForInterstitial = false;
        }

        public void SetGameDay(int gameDay)
        {
            if (gameDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(gameDay));
            }

            if (gameDay == currentGameDay)
            {
                return;
            }

            currentGameDay = gameDay;
            sideJobUsesToday = 0;
        }

        /// <summary>
        /// 로컬 캘린더 일자로 특성 조각 쿼터를 맞춘다. 날짜가 바뀌면 사용 횟수를 0으로 한다.
        /// </summary>
        public void SyncTraitFragmentCalendar(string todayKey, string savedKey, int savedUsed)
        {
            var today = todayKey ?? string.Empty;
            if (string.Equals(today, savedKey, StringComparison.Ordinal))
            {
                traitFragmentDateKey = today;
                traitFragmentUsesToday = Math.Max(0, savedUsed);
                return;
            }

            traitFragmentDateKey = today;
            traitFragmentUsesToday = 0;
        }

        public void ClearInterstitialSkipFlag()
        {
            rewardedRecentlyForInterstitial = false;
        }

        public int GetRemaining(RewardedAdPlacement placement)
        {
            var limit = GetLimit(placement);
            var used = GetUsed(placement);
            return Math.Max(0, limit - used);
        }

        public bool IsOnCooldown(out double remainingSeconds)
        {
            var elapsed = clock.UtcSeconds - lastRewardedUtcSeconds;
            if (elapsed >= cooldownSeconds)
            {
                remainingSeconds = 0;
                return false;
            }

            remainingSeconds = cooldownSeconds - elapsed;
            return true;
        }

        public bool CanConsume(RewardedAdPlacement placement, out string reason)
        {
            // TraitFragment는 캘린더 일 한도만 적용(연속 시청 가능).
            // 그 외 보상형은 같은 placement 연타만 쿨다운(부업 후 재시도 등 다른 종류는 연속 가능).
            if (placement != RewardedAdPlacement.TraitFragment
                && lastRewardedPlacement == placement
                && IsOnCooldown(out var remaining))
            {
                reason = $"Ad cooldown {remaining:F1}s remaining.";
                return false;
            }

            if (GetRemaining(placement) <= 0)
            {
                reason = $"Quota exceeded for {placement}.";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// 광고 시청 성공 후 호출한다. 실패/취소 시에는 호출하지 않는다.
        /// </summary>
        public void ConsumeOnSuccess(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.DailySideJob:
                    sideJobUsesToday++;
                    break;
                case RewardedAdPlacement.TraitFragment:
                    if (string.IsNullOrEmpty(traitFragmentDateKey))
                    {
                        traitFragmentDateKey = DateTime.Now.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture);
                    }

                    traitFragmentUsesToday++;
                    break;
                default:
                    runUsage[placement] = GetUsed(placement) + 1;
                    break;
            }

            lastRewardedPlacement = placement;
            lastRewardedUtcSeconds = clock.UtcSeconds;
            rewardedRecentlyForInterstitial = true;
        }

        private int GetUsed(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.DailySideJob:
                    return sideJobUsesToday;
                case RewardedAdPlacement.TraitFragment:
                    return traitFragmentUsesToday;
                default:
                    return runUsage.TryGetValue(placement, out var used) ? used : 0;
            }
        }

        private static int GetLimit(RewardedAdPlacement placement)
        {
            switch (placement)
            {
                case RewardedAdPlacement.ChoiceReroll:
                    return ChoiceRerollLimitPerRun;
                case RewardedAdPlacement.RetryOutcome:
                    return RetryOutcomeLimitPerRun;
                case RewardedAdPlacement.EmergencyLoan:
                    return EmergencyLoanLimitPerRun;
                case RewardedAdPlacement.DailySideJob:
                    return DailySideJobLimitPerDay;
                case RewardedAdPlacement.DoubleExperience:
                    return DoubleExperienceLimitPerRun;
                case RewardedAdPlacement.TraitFragment:
                    return TraitFragmentLimitPerDay;
                default:
                    return 0;
            }
        }
    }
}
