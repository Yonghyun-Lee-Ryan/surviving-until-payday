using NUnit.Framework;
using SurviveUntilPayday.UI;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class ResultScreenLayoutTests
    {
        [Test]
        public void ButtonStack_DoesNotOverlap_WhenDoubleXpVisible()
        {
            var stack = ResultScreenLayout.ComputeButtonStack(1920f, showDoubleXp: true);

            Assert.Greater(
                Mathf.Abs(stack.ShareCenterY - stack.BackCenterY),
                (ResultScreenLayout.ShareHeight + ResultScreenLayout.BackHeight) * 0.5f + 1f);
            Assert.Greater(
                Mathf.Abs(stack.DoubleXpCenterY - stack.ShareCenterY),
                (ResultScreenLayout.DoubleXpHeight + ResultScreenLayout.ShareHeight) * 0.5f + 1f);
            Assert.Greater(stack.StackTopY, stack.DoubleXpCenterY);
            Assert.Greater(stack.DoubleXpCenterY, stack.ShareCenterY);
            Assert.Greater(stack.ShareCenterY, stack.BackCenterY);
        }

        [Test]
        public void ButtonStack_StaysAboveScreenBottom()
        {
            var stack = ResultScreenLayout.ComputeButtonStack(1920f, showDoubleXp: true);
            var screenBottom = -960f;
            Assert.Greater(stack.BackCenterY - ResultScreenLayout.BackHeight * 0.5f, screenBottom);
        }

        [Test]
        public void BodyViewport_LeavesGapAboveButtons()
        {
            var stack = ResultScreenLayout.ComputeButtonStack(1920f, showDoubleXp: true);
            var statsBottom = 65f;
            var bodyHeight = ResultScreenLayout.BodyViewportHeight(statsBottom, stack.StackTopY);
            var bodyBottom = statsBottom - ResultScreenLayout.BodyButtonGap - bodyHeight;
            Assert.Greater(bodyHeight, 200f);
            Assert.Greater(bodyBottom, stack.StackTopY);
        }
    }
}
