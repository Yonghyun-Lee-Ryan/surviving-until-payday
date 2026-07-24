using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Result Scene의 임시 UI. 이후 엔딩/결과 표시로 확장한다.
    /// </summary>
    public sealed class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private Button backToMenuButton;

        private void Awake()
        {
            if (backToMenuButton == null)
            {
                Debug.LogError("[ResultSceneController] backToMenuButton is not assigned.", this);
                return;
            }

            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        private void OnDestroy()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            }
        }

        private void OnBackToMenuClicked()
        {
            if (AppRoot.Instance == null || AppRoot.Instance.SceneLoader == null)
            {
                Debug.LogError("[ResultSceneController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            AppRoot.Instance.SceneLoader.LoadMainMenu();
        }
    }
}
