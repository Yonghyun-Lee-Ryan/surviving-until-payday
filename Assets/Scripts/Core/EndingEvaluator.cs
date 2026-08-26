using System;
using System.Collections.Generic;
using SurviveUntilPayday.Data;
using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 성공/실패 엔딩 중 우선순위가 가장 높은 하나를 선택한다.
    /// </summary>
    public sealed class EndingEvaluator
    {
        private readonly List<EndingData> endings = new List<EndingData>();
        private readonly EndingData fallbackSuccessEnding;

        public EndingEvaluator(IEnumerable<EndingData> endings, EndingData fallbackSuccessEnding)
        {
            if (endings == null)
            {
                throw new ArgumentNullException(nameof(endings));
            }

            this.fallbackSuccessEnding = fallbackSuccessEnding;

            foreach (var ending in endings)
            {
                if (ending == null)
                {
                    Debug.LogWarning("[EndingEvaluator] Null EndingData skipped.");
                    continue;
                }

                this.endings.Add(ending);
            }
        }

        public EndingData Evaluate(GameState state, bool survivedToPayday, FailureReason failureReason)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (failureReason != FailureReason.None)
            {
                var failureEnding = PickBest(ending =>
                    ending.IsFailureEnding && ending.LinkedFailureReason == failureReason);
                if (failureEnding != null)
                {
                    return failureEnding;
                }

                Debug.LogWarning(
                    $"[EndingEvaluator] No failure ending for {failureReason}. Using first failure ending if any.");
                return PickBest(ending => ending.IsFailureEnding) ?? fallbackSuccessEnding;
            }

            if (!survivedToPayday)
            {
                Debug.LogWarning("[EndingEvaluator] Not failed and not survived. Using fallback success ending.");
                return fallbackSuccessEnding;
            }

            var successEnding = PickBest(ending =>
                !ending.IsFailureEnding && EndingConditionMatcher.Matches(ending.Condition, state));

            if (successEnding != null
                && EndingConditionMatcher.IsCashKingId(successEnding.Id)
                && EndingConditionMatcher.IsCloseCallSurvival(state.Stats))
            {
                var alternative = PickBest(ending =>
                    ending != successEnding
                    && !ending.IsFailureEnding
                    && EndingConditionMatcher.Matches(ending.Condition, state));
                successEnding = alternative ?? fallbackSuccessEnding;
            }

            return successEnding ?? fallbackSuccessEnding;
        }

        private EndingData PickBest(Func<EndingData, bool> predicate)
        {
            EndingData best = null;
            var bestPriority = int.MinValue;

            for (var i = 0; i < endings.Count; i++)
            {
                var ending = endings[i];
                if (!predicate(ending))
                {
                    continue;
                }

                if (best == null || ending.Priority > bestPriority)
                {
                    best = ending;
                    bestPriority = ending.Priority;
                }
            }

            return best;
        }
    }

    public static class EndingConditionMatcher
    {
        public static bool Matches(EndingCondition condition, GameState state)
        {
            if (state == null)
            {
                return false;
            }

            if (!Matches(condition, state.Stats))
            {
                return false;
            }

            if (condition == null)
            {
                return true;
            }

            if (condition.RequiredFlags != null)
            {
                for (var i = 0; i < condition.RequiredFlags.Count; i++)
                {
                    var flag = condition.RequiredFlags[i];
                    if (string.IsNullOrWhiteSpace(flag))
                    {
                        continue;
                    }

                    if (!state.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            if (condition.ForbiddenFlags != null)
            {
                for (var i = 0; i < condition.ForbiddenFlags.Count; i++)
                {
                    var flag = condition.ForbiddenFlags[i];
                    if (string.IsNullOrWhiteSpace(flag))
                    {
                        continue;
                    }

                    if (state.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsCashKingId(string endingId)
        {
            return endingId == "ending_cash_king" || endingId == "cash";
        }

        /// <summary>
        /// 월급날은 왔지만 체력·스트레스·회사 평가가 아슬아슬한 구간 (R-QA-04).
        /// cash_king보다 겨우 살아남았다/다른 성공 엔딩을 우선한다.
        /// </summary>
        public static bool IsCloseCallSurvival(PlayerStats stats)
        {
            if (stats == null)
            {
                return false;
            }

            return stats.Cash <= 900_000L
                   || stats.Health <= 32
                   || stats.Stress >= 75
                   || stats.CompanyScore <= 20;
        }

        public static bool Matches(EndingCondition condition, PlayerStats stats)
        {
            if (stats == null)
            {
                return false;
            }

            if (condition == null)
            {
                return true;
            }

            if (condition.RequireMinCash && stats.Cash < condition.MinCash)
            {
                return false;
            }

            if (condition.RequireMaxCash && stats.Cash > condition.MaxCash)
            {
                return false;
            }

            if (condition.RequireMinHealth && stats.Health < condition.MinHealth)
            {
                return false;
            }

            if (condition.RequireMaxHealth && stats.Health > condition.MaxHealth)
            {
                return false;
            }

            if (condition.RequireMinStress && stats.Stress < condition.MinStress)
            {
                return false;
            }

            if (condition.RequireMaxStress && stats.Stress > condition.MaxStress)
            {
                return false;
            }

            if (condition.RequireMinHappiness && stats.Happiness < condition.MinHappiness)
            {
                return false;
            }

            if (condition.RequireMaxHappiness && stats.Happiness > condition.MaxHappiness)
            {
                return false;
            }

            if (condition.RequireMinCompanyScore && stats.CompanyScore < condition.MinCompanyScore)
            {
                return false;
            }

            if (condition.RequireMaxCompanyScore && stats.CompanyScore > condition.MaxCompanyScore)
            {
                return false;
            }

            return true;
        }
    }
}
