using System.Collections;
using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurviveUntilPayday.Tests.PlayMode
{
    public sealed class Rqa09PlayModeCopyTests
    {
        [UnityTest]
        public IEnumerator SettingsPanel_ShowsCreditsButtonAndOfflineNote()
        {
            var canvas = new GameObject("Rqa09PlayCanvas", typeof(RectTransform), typeof(Canvas));
            var go = new GameObject("SettingsPanel", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var view = go.AddComponent<SettingsPanelView>();
            yield return null;

            view.Show();
            yield return null;

            Assert.IsTrue(FindChild(go.transform, "CreditsButton"), "설정에 크레딧 버튼이 없습니다.");
            Assert.IsTrue(FindChild(go.transform, "OfflineNote"), "설정에 오프라인 안내가 없습니다.");
            Assert.IsTrue(FindChild(go.transform, "PreviewToggle"), "설정에 선택 미리보기가 없습니다.");

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
