using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// Bootstrap Scene 진입 시 AppRoot를 초기화하고 MainMenu로 이동한다.
    /// </summary>
    public sealed class BootstrapInitializer : MonoBehaviour
    {
        [SerializeField] private float delaySeconds;

        private void Start()
        {
            var appRoot = AppRoot.EnsureCreated();
            if (appRoot == null || appRoot.SceneLoader == null)
            {
                Debug.LogError("[BootstrapInitializer] Failed to create AppRoot or SceneLoader.");
                return;
            }

            if (delaySeconds > 0f)
            {
                Invoke(nameof(GoToMainMenu), delaySeconds);
            }
            else
            {
                GoToMainMenu();
            }
        }

        private void GoToMainMenu()
        {
            if (AppRoot.Instance == null || AppRoot.Instance.SceneLoader == null)
            {
                Debug.LogError("[BootstrapInitializer] AppRoot.SceneLoader is missing.");
                return;
            }

            AppRoot.Instance.SceneLoader.LoadMainMenu();
        }
    }
}
