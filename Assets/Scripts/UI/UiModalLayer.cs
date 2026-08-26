using UnityEngine;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 결과/주간결산 등 모달이 HUD보다 앞에 오도록 형제 순서를 맞춘다 (R-QA-06).
    /// </summary>
    public static class UiModalLayer
    {
        public static void BringToFront(Component modal)
        {
            if (modal == null)
            {
                return;
            }

            BringToFront(modal.transform);
        }

        public static void BringToFront(Transform modal)
        {
            if (modal == null || !modal.gameObject.activeInHierarchy)
            {
                return;
            }

            modal.SetAsLastSibling();
        }

        /// <summary>
        /// HUD를 앞으로 올린 뒤, 활성 모달을 그 위에 다시 올린다.
        /// </summary>
        public static void RestackModalsAboveHud(Transform hud, params Component[] modals)
        {
            if (hud != null)
            {
                hud.SetAsLastSibling();
            }

            if (modals == null)
            {
                return;
            }

            for (var i = 0; i < modals.Length; i++)
            {
                var modal = modals[i];
                if (modal == null)
                {
                    continue;
                }

                var target = ResolveSiblingUnderSameParent(modal, hud);
                if (target != null && target.gameObject.activeSelf)
                {
                    target.SetAsLastSibling();
                }
            }
        }

        public static bool IsInFrontOf(Transform front, Transform back)
        {
            if (front == null || back == null || front.parent != back.parent)
            {
                return false;
            }

            return front.GetSiblingIndex() > back.GetSiblingIndex();
        }

        private static Transform ResolveSiblingUnderSameParent(Component modal, Transform hud)
        {
            var start = ResolveRoot(modal);
            if (start == null)
            {
                return null;
            }

            if (hud == null || hud.parent == null)
            {
                return start;
            }

            var current = start;
            while (current != null && current.parent != hud.parent)
            {
                current = current.parent;
            }

            return current != null ? current : start;
        }

        private static Transform ResolveRoot(Component modal)
        {
            if (modal is ResultPopupView result && result.RootTransform != null)
            {
                return result.RootTransform;
            }

            if (modal is WeeklySummaryPopupView weekly && weekly.RootTransform != null)
            {
                return weekly.RootTransform;
            }

            return modal.transform;
        }
    }
}
