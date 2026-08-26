using System.Collections;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurviveUntilPayday.Tests.PlayMode
{
    public sealed class Rqa10PlayModeFlowTests
    {
        [UnityTest]
        public IEnumerator ConsentDailySettings_StayUsable()
        {
            var canvas = new GameObject("Rqa10PlayCanvas", typeof(RectTransform), typeof(Canvas));

            var consentGo = new GameObject("ConsentPanel", typeof(RectTransform));
            consentGo.transform.SetParent(canvas.transform, false);
            consentGo.SetActive(false);
            var consent = consentGo.AddComponent<ConsentPanelView>();
            consent.Bind(consentGo, null, null, null, null);
            consent.Show(() => { });
            yield return null;
            Assert.IsTrue(consentGo.activeSelf, "동의 패널이 Show 직후 꺼집니다.");

            var dailyGo = new GameObject("DailyPanel", typeof(RectTransform));
            dailyGo.transform.SetParent(canvas.transform, false);
            var dailyView = dailyGo.AddComponent<DailyPanelView>();
            var daily = new DailyContentState();
            daily.Load("2026-08-25", 0, false, 999, 0, 0, false, null);
            dailyView.Show(daily, () => { });
            yield return null;
            Assert.IsTrue(FindChild(dailyGo.transform, "Missions"));
            Assert.IsTrue(FindChild(dailyGo.transform, "PlayButton") || FindChild(dailyGo.transform, "CloseButton"));

            var settingsGo = new GameObject("SettingsPanel", typeof(RectTransform));
            settingsGo.transform.SetParent(canvas.transform, false);
            var settings = settingsGo.AddComponent<SettingsPanelView>();
            settings.Show();
            yield return null;
            Assert.IsTrue(FindChild(settingsGo.transform, "ResetSaveButton"));
            Assert.IsTrue(FindChild(settingsGo.transform, "CreditsButton"));

            Object.Destroy(canvas);
            yield return null;
        }

        private static bool FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == name)
            {
                return true;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                if (FindChild(root.GetChild(i), name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
