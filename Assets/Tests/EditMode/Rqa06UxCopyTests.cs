using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using SurviveUntilPayday.Events;
using SurviveUntilPayday.Settings;
using SurviveUntilPayday.UI;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa06UxCopyTests
    {
        [Test]
        public void TutorialCopy_TeachesFailureAndSafeOnlyTrap()
        {
            Assert.IsTrue(TutorialCopy.TeachesFailureIsOk());
            Assert.IsTrue(TutorialCopy.WarnsSafeOnlyPath());
            Assert.AreEqual(5, TutorialCopy.Titles.Length);
            Assert.AreEqual(TutorialCopy.Titles.Length, TutorialCopy.Bodies.Length);
        }

        [Test]
        public void FailureTip_SuccessCashKing_WarnsSafeOnly()
        {
            var tip = FailureTipCatalog.GetTip(FailureReason.None, true, "ending_cash_king");
            Assert.IsTrue(tip.Contains("안전만"), tip);
            Assert.IsTrue(tip.Contains("실패해도"), tip);
        }

        [Test]
        public void FailureTip_Bankruptcy_MentionsRetryLearning()
        {
            var tip = FailureTipCatalog.GetTip(FailureReason.Bankruptcy, false);
            Assert.IsTrue(tip.Contains("실패해도"), tip);
        }

        [Test]
        public void AdBlockReasonCopy_QuotaAndCooldownAreKorean()
        {
            var quota = AdBlockReasonCopy.QuotaExhausted(RewardedAdPlacement.ChoiceReroll);
            Assert.IsTrue(quota.Contains("한도"), quota);
            var cooldown = AdBlockReasonCopy.Cooldown(1.2d);
            Assert.IsTrue(cooldown.Contains("쿨다운"), cooldown);
            var fromGateway = AdBlockReasonCopy.FromGatewayReason(
                "Quota exceeded for ChoiceReroll.",
                RewardedAdPlacement.DoubleExperience);
            Assert.IsTrue(fromGateway.Contains("한도"), fromGateway);
        }

        [Test]
        public void EndingShareCopy_IncludesDaysAndHashtag()
        {
            var ending = ScriptableObject.CreateInstance<EndingData>();
            ending.EditorSet(
                "ending_cash_king",
                "현금왕",
                "d",
                1,
                false,
                FailureReason.None,
                new EndingCondition());
            var result = new ResultData(
                30,
                true,
                FailureReason.None,
                new PlayerStats(500_000, 50, 50, 50, 50),
                ending,
                10,
                false);
            var text = EndingShareCopy.Build(result);
            Assert.IsTrue(text.Contains("현금왕"), text);
            Assert.IsTrue(text.Contains("30일"), text);
            Assert.IsTrue(text.Contains("#월급날까지살아남기"), text);
        }

        [Test]
        public void ChoicePreviewCopy_ShowsUpDownAndUncertainty()
        {
            var choice = new EventChoiceData(
                "c1",
                "야근한다",
                new List<StatEffect>
                {
                    new StatEffect(StatType.Cash, 40_000),
                    new StatEffect(StatType.Stress, 12)
                },
                new List<RandomOutcome>
                {
                    new RandomOutcome("r1", "운", 100, new StatEffect(StatType.Health, -5))
                });
            var trend = ChoicePreviewCopy.FormatTrend(choice);
            Assert.IsTrue(trend.Contains("현금↑"), trend);
            Assert.IsTrue(trend.Contains("스트레스↑"), trend);
            Assert.IsTrue(trend.Contains("운"), trend);
            var hidden = ChoicePreviewCopy.CombineLabel("야근한다", trend, false);
            Assert.AreEqual("야근한다", hidden);
            var shown = ChoicePreviewCopy.CombineLabel("야근한다", trend, true);
            Assert.IsTrue(shown.Contains("현금↑"), shown);
            Assert.IsTrue(shown.Contains("\n("), shown);
        }

        [Test]
        public void ChoiceFeedbackCopy_CommentsLargeCashLoss()
        {
            var result = new ChoiceResult(
                3,
                "e",
                "사건",
                0,
                "c",
                "쓴다",
                "msg",
                null,
                null,
                new PlayerStats(200_000, 50, 20, 50, 50),
                new PlayerStats(50_000, 50, 20, 50, 50),
                new[]
                {
                    new StatChangeResult(StatType.Cash, 200_000, 50_000, -150_000)
                },
                FailureReason.None);
            var line = ChoiceFeedbackCopy.BuildDramaLine(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(line));
            Assert.IsTrue(line.Contains("통장") || line.Contains("고정비"), line);
        }

        [Test]
        public void WeeklySummary_EmptyWarnings_HintsSafeOnlyTrap()
        {
            var state = new GameState { CurrentDay = 7 };
            state.Stats.CopyFrom(new PlayerStats(800_000L, 70, 20, 60, 70));
            var info = new WeeklySummaryInfo(1, 7, state);
            var warnings = WeeklySummaryFormatter.BuildWarnings(info);
            Assert.IsTrue(warnings.Contains("안전만") || warnings.Contains("위험한"), warnings);
        }

        [Test]
        public void UiModalLayer_ResultPopupStaysInFrontOfHud()
        {
            var canvas = new GameObject("Rqa06Canvas", typeof(RectTransform), typeof(Canvas));
            var hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(canvas.transform, false);
            hud.AddComponent<GameHudView>();
            var popupGo = new GameObject("ResultPopup", typeof(RectTransform));
            popupGo.transform.SetParent(canvas.transform, false);
            var popup = popupGo.AddComponent<ResultPopupView>();
            hud.transform.SetAsLastSibling();
            popupGo.SetActive(true);
            UiModalLayer.RestackModalsAboveHud(hud.transform, popup);
            Assert.IsTrue(UiModalLayer.IsInFrontOf(popupGo.transform, hud.transform));
            Object.DestroyImmediate(canvas);
        }

        [Test]
        public void GameplayLayoutApplier_RestacksPopupAboveHud()
        {
            var canvas = new GameObject("Rqa06LayoutCanvas", typeof(RectTransform), typeof(Canvas));
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            var hud = hudGo.AddComponent<GameHudView>();
            var popupGo = new GameObject("ResultPopup", typeof(RectTransform));
            popupGo.transform.SetParent(canvas.transform, false);
            popupGo.AddComponent<ResultPopupView>();
            popupGo.SetActive(true);
            hudGo.transform.SetAsLastSibling();
            GameplayLayoutApplier.Apply(hud, null, null);
            Assert.IsTrue(UiModalLayer.IsInFrontOf(popupGo.transform, hudGo.transform));
            Object.DestroyImmediate(canvas);
        }

        [Test]
        public void AdBlockReasonCopy_ButtonLabel_ShowsReasonWhenBlocked()
        {
            var blocked = AdBlockReasonCopy.ButtonLabel(
                "광고: 다른 사건 보기 (0)",
                false,
                AdBlockReasonCopy.QuotaExhausted(RewardedAdPlacement.ChoiceReroll));
            Assert.IsTrue(blocked.Contains("한도"), blocked);
            Assert.AreEqual("광고: 재시도", AdBlockReasonCopy.ButtonLabel("광고: 재시도", true, "한도 소진"));
        }

        [Test]
        public void DebugPanel_IsIncludedInEditorBuild()
        {
            Assert.IsTrue(DebugPanel.IsIncludedInThisBuild);
        }

        [Test]
        public void AppSettings_ShowChoicePreview_DefaultsOffUntilUserEnables()
        {
            var settings = new AppSettingsService(new MemoryStore());
            Assert.IsFalse(settings.ShowChoicePreview);
            settings.ShowChoicePreview = true;
            Assert.IsTrue(settings.ShowChoicePreview);
        }

        [Test]
        public void AppSettings_ShowChoicePreview_NotifiesListeners()
        {
            var settings = new AppSettingsService(new MemoryStore());
            var seen = new List<bool>();
            settings.ChoicePreviewChanged += v => seen.Add(v);

            settings.ShowChoicePreview = false;
            Assert.AreEqual(0, seen.Count);

            settings.ShowChoicePreview = true;
            Assert.AreEqual(1, seen.Count);
            Assert.IsTrue(seen[0]);

            settings.ShowChoicePreview = false;
            Assert.AreEqual(2, seen.Count);
            Assert.IsFalse(seen[1]);
        }

        private sealed class MemoryStore : IAppSettingsStore
        {
            public AppSettingsData Load()
            {
                return new AppSettingsData();
            }

            public void Save(AppSettingsData data)
            {
            }
        }
    }
}
