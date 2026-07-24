using System.IO;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 10: MainMenu 도감/레벨 패널.
    /// </summary>
    public static class MainMenuCodexSetup
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Surviving Until Payday/Setup MainMenu Codex Panel (Unit 10)")]
        public static void Setup()
        {
            if (!File.Exists(MainMenuPath))
            {
                Debug.LogError("[MainMenuCodexSetup] MainMenu.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            var safeArea = GameObject.Find("SafeArea");
            if (controller == null || safeArea == null)
            {
                Debug.LogError("[MainMenuCodexSetup] MainMenuController/SafeArea missing.");
                return;
            }

            var existing = safeArea.transform.Find("CodexPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panel = CreatePanel(safeArea.transform, "CodexPanel", new Vector2(0f, -520f), new Vector2(920f, 280f),
                new Color(0.92f, 0.93f, 0.94f, 0.95f));
            var level = CreateText(panel.transform, "Level", "Lv.1", 36, new Vector2(0f, 100f));
            var xp = CreateText(panel.transform, "XP", "인생 경험치 0", 28, new Vector2(0f, 55f));
            var ending = CreateText(panel.transform, "EndingRate", "엔딩 0/0", 26, new Vector2(-220f, 0f));
            var events = CreateText(panel.transform, "EventRate", "사건 0/0", 26, new Vector2(220f, 0f));
            var traits = CreateText(panel.transform, "TraitRate", "특성 0/0", 26, new Vector2(-220f, -50f));
            var ach = CreateText(panel.transform, "AchievementRate", "업적 0/0", 26, new Vector2(220f, -50f));
            var toast = CreateText(panel.transform, "UnlockToast", "", 24, new Vector2(0f, -100f));

            var codex = panel.AddComponent<CodexPanelView>();
            codex.Bind(level, xp, ending, events, traits, ach, toast);

            var so = new SerializedObject(controller);
            so.FindProperty("codexPanel").objectReferenceValue = codex;
            so.FindProperty("totalEndingCount").intValue = 9;
            so.FindProperty("totalEventCount").intValue = 3;
            so.FindProperty("totalTraitCount").intValue = 4;
            so.FindProperty("totalAchievementCount").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MainMenuCodexSetup] Codex panel added to MainMenu.");
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(420f, 40f);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.15f, 0.18f);
            return text;
        }
    }
}
