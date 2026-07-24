using NUnit.Framework;
using SurviveUntilPayday.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.Tests
{
    public sealed class StatGaugeViewTests
    {
        [Test]
        public void SetValueInstant_ScalesFillRectWithoutSprite()
        {
            var root = new GameObject("GaugeRoot");
            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(root.transform, false);
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);

            var fill = fillObject.GetComponent<Image>();
            fill.sprite = null;
            fill.type = Image.Type.Filled;

            var gauge = root.AddComponent<StatGaugeView>();
            gauge.EditorBind(null, null, fill, null);
            gauge.SetValueInstant(40);

            Assert.AreEqual(40, gauge.DisplayedValue);
            Assert.AreEqual(Image.Type.Simple, fill.type);
            Assert.AreEqual(0.4f, fill.rectTransform.anchorMax.x, 0.001f);

            Object.DestroyImmediate(root);
        }
    }
}
