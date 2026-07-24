using SurviveUntilPayday.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 레거시 임시 종료 버튼. Unit 7 이후에는 GamePlayPresenter를 사용한다.
    /// </summary>
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private Button tempEndButton;

        private void Awake()
        {
            if (tempEndButton == null)
            {
                enabled = false;
                return;
            }

            tempEndButton.onClick.AddListener(OnTempEndClicked);
        }

        private void OnDestroy()
        {
            if (tempEndButton != null)
            {
                tempEndButton.onClick.RemoveListener(OnTempEndClicked);
            }
        }

        private void OnTempEndClicked()
        {
            if (AppRoot.Instance == null || AppRoot.Instance.SceneLoader == null)
            {
                Debug.LogError("[GameSceneController] SceneLoader is unavailable. Was Bootstrap skipped?");
                return;
            }

            AppRoot.Instance.SceneLoader.LoadResult();
        }
    }
}
