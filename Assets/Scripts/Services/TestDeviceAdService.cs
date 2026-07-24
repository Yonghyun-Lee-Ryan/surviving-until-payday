using System;
using System.Collections;
using SurviveUntilPayday.Ads;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// 테스트 광고 구현. Editor/기기에서 짧은 지연 후 성공하며, 실패 모드도 지원한다.
    /// 실제 AdMob 없이도 "테스트 광고" 완료 콜백을 검증할 수 있다.
    /// </summary>
    public sealed class TestDeviceAdService : IAdService
    {
        private readonly MonoBehaviour runner;
        private readonly float delaySeconds;
        private bool rewardedReady = true;
        private bool interstitialReady = true;
        private bool forceFailure;

        public int RewardedShowCount { get; private set; }
        public int InterstitialShowCount { get; private set; }

        public TestDeviceAdService(MonoBehaviour runner, float delaySeconds = 0.15f)
        {
            this.runner = runner;
            this.delaySeconds = Mathf.Max(0f, delaySeconds);
        }

        public void SetForceFailure(bool force) => forceFailure = force;

        public void SetRewardedReady(bool ready) => rewardedReady = ready;

        public void SetInterstitialReady(bool ready) => interstitialReady = ready;

        public bool IsRewardedReady(RewardedAdPlacement placement) => rewardedReady;

        public bool IsInterstitialReady() => interstitialReady;

        public void ShowRewardedAd(RewardedAdPlacement placement, Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            RewardedShowCount++;
            Debug.Log($"[TestDeviceAdService] Rewarded show: {placement}");
            RunShow(rewardedReady, onFinished);
        }

        public void ShowInterstitial(Action<AdShowResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            InterstitialShowCount++;
            Debug.Log("[TestDeviceAdService] Interstitial show");
            RunShow(interstitialReady, onFinished);
        }

        private void RunShow(bool ready, Action<AdShowResult> onFinished)
        {
            if (!ready)
            {
                onFinished(AdShowResult.NotReady("Test ad not ready."));
                return;
            }

            if (runner == null)
            {
                FinishImmediate(onFinished);
                return;
            }

            runner.StartCoroutine(ShowRoutine(onFinished));
        }

        private IEnumerator ShowRoutine(Action<AdShowResult> onFinished)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            FinishImmediate(onFinished);
        }

        private void FinishImmediate(Action<AdShowResult> onFinished)
        {
            if (forceFailure)
            {
                onFinished(AdShowResult.Failed("Test ad failed (forced)."));
                return;
            }

            onFinished(AdShowResult.Completed());
        }
    }
}
