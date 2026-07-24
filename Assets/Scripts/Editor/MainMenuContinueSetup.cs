using System.IO;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 9: MainMenu에 이어하기 버튼을 추가한다.
    /// </summary>
    public static class MainMenuContinueSetup
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Surviving Until Payday/Setup MainMenu Continue Button (Unit 9)")]
        public static void Setup()
        {
            if (!File.Exists(MainMenuPath))
            {
                Debug.LogError("[MainMenuContinueSetup] MainMenu.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[MainMenuContinueSetup] MainMenuController not found. Run Foundation setup.");
                return;
            }

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null)
            {
                Debug.LogError("[MainMenuContinueSetup] SafeArea not found.");
                return;
            }

            var existing = safeArea.transform.Find("ContinueButton");
            Button continueButton;
            if (existing != null)
            {
                continueButton = existing.GetComponent<Button>();
            }
            else
            {
                var go = new GameObject("ContinueButton", typeof(RectTransform));
                go.transform.SetParent(safeArea.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -260f);
                rect.sizeDelta = new Vector2(520f, 100f);
                var image = go.AddComponent<Image>();
                image.color = new Color(0.25f, 0.5f, 0.4f, 1f);
                continueButton = go.AddComponent<Button>();
                continueButton.targetGraphic = image;

                var labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(go.transform, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelObject.AddComponent<Text>();
                label.text = "이어하기";
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                             ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 40;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MainMenuContinueSetup] Continue button wired. It shows only when a run save exists.");
        }
    }
}
