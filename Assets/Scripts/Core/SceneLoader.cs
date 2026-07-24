using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// Scene 전환만 담당한다. 게임 상태는 다루지 않는다.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public bool IsLoading { get; private set; }

        public event Action<string> SceneLoadStarted;
        public event Action<string> SceneLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SceneLoader] Duplicate instance detected. Destroying this component.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneLoader] sceneName is null or empty.");
                return;
            }

            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignored request: {sceneName}");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoader] Scene '{sceneName}' is not in Build Settings or cannot be loaded.");
                return;
            }

            IsLoading = true;
            SceneLoadStarted?.Invoke(sceneName);
            SceneManager.LoadScene(sceneName);
            IsLoading = false;
            SceneLoadCompleted?.Invoke(sceneName);
        }

        public void LoadMainMenu()
        {
            LoadScene(SceneNames.MainMenu);
        }

        public void LoadGame()
        {
            LoadScene(SceneNames.Game);
        }

        public void LoadResult()
        {
            LoadScene(SceneNames.Result);
        }
    }
}
