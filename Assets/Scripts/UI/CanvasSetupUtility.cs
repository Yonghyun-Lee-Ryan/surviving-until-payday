using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 공통 Canvas 기준(1080x1920, Scale With Screen Size)을 적용한다.
    /// </summary>
    public static class CanvasSetupUtility
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        public static void ApplyPortraitCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                Debug.LogError("[CanvasSetupUtility] CanvasScaler is null.");
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Portrait 기준: 세로 길이 변화에 더 민감하게 맞춤
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        /// <summary>
        /// Input System 전용 프로젝트용 EventSystem. Bootstrap 등 씬에 빠져 있으면 UI 클릭이 동작하지 않는다.
        /// </summary>
        public static EventSystem EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null
                    && existing.GetComponent<BaseInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return existing;
            }

            var go = new GameObject("EventSystem");
            var eventSystem = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return eventSystem;
        }
    }
}
