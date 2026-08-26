using System.Collections;
using NUnit.Framework;
using SurviveUntilPayday.DebugTools;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurviveUntilPayday.Tests.PlayMode
{
    public sealed class Rqa06PlayModeLayerTests
    {
        [UnityTest]
        public IEnumerator ResultAndWeeklyPopups_StayInFrontOfHud()
        {
            var canvas = new GameObject("Rqa06PlayCanvas", typeof(RectTransform), typeof(Canvas));
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            var hud = hudGo.AddComponent<GameHudView>();
            var resultGo = new GameObject("ResultPopup", typeof(RectTransform));
            resultGo.transform.SetParent(canvas.transform, false);
            var result = resultGo.AddComponent<ResultPopupView>();
            var weeklyGo = new GameObject("WeeklySummaryPopup", typeof(RectTransform));
            weeklyGo.transform.SetParent(canvas.transform, false);
            var weekly = weeklyGo.AddComponent<WeeklySummaryPopupView>();

            hudGo.transform.SetAsLastSibling();
            result.Show("결과", "선택 결과", "현금 -80,000\n통장이 한 꺼풀 얇아졌습니다. 다음 고정비를 남겨 두세요.", "다음 날");
            GameplayLayoutApplier.Apply(hud, null, null);
            yield return null;

            Assert.IsTrue(
                UiModalLayer.IsInFrontOf(resultGo.transform, hudGo.transform),
                "PlayMode: 결과 팝업이 HUD에 가려집니다.");

            weekly.Show("1주차 결산", "7일까지의 상태를 점검합니다.", "큰 위험 신호는 없습니다.");
            UiModalLayer.RestackModalsAboveHud(hudGo.transform, result, weekly);
            yield return null;

            Assert.IsTrue(
                UiModalLayer.IsInFrontOf(weeklyGo.transform, hudGo.transform),
                "PlayMode: 주간결산이 HUD에 가려집니다.");
            Assert.IsTrue(DebugPanel.IsIncludedInThisBuild);

            Object.Destroy(canvas);
            yield return null;
        }
    }
}
