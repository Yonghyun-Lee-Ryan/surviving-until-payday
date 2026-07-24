using UnityEngine;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// RectTransform을 Screen.safeArea에 맞춰 노치/홈 인디케이터 영역을 피한다.
    /// Canvas 하위 SafeArea 패널에 붙인다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool applyOnUpdate = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("[SafeAreaFitter] RectTransform is required.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            ApplySafeArea(force: true);
        }

        private void Update()
        {
            if (!applyOnUpdate)
            {
                return;
            }

            ApplySafeArea(force: false);
        }

        public void ApplySafeArea(bool force)
        {
            if (rectTransform == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var orientation = Screen.orientation;

            if (!force
                && safeArea == lastSafeArea
                && screenSize == lastScreenSize
                && orientation == lastOrientation)
            {
                return;
            }

            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastOrientation = orientation;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
