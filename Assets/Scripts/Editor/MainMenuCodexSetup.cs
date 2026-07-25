using System.IO;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Unit 10/24: MainMenu 도감 패널을 정리된 카드 레이아웃으로 재생성.
    /// </summary>
    public static class MainMenuCodexSetup
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string EndingsFolder = "Assets/Data/Endings";

        [MenuItem("Tools/Surviving Until Payday/Setup MainMenu Codex Panel (Unit 10/24)")]
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

            // 실제 배치는 CodexPanelView.EnsureCleanLayout이 Play 시 재구성한다.
            // 여기선 빈 패널 + 필수 바인딩용 더미만 둔다.
            var panel = new GameObject("CodexPanel", typeof(RectTransform), typeof(Image), typeof(CodexPanelView));
            panel.transform.SetParent(safeArea.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.02f);
            rect.anchorMax = new Vector2(0.96f, 0.48f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0f);
            panel.GetComponent<Image>().color = new Color(0.96f, 0.95f, 0.92f, 0.98f);

            var codex = panel.GetComponent<CodexPanelView>();
            var endings = LoadEndings();
            var events = LoadEvents();
            var so = new SerializedObject(controller);
            so.FindProperty("codexPanel").objectReferenceValue = codex;
            so.FindProperty("totalEndingCount").intValue = Mathf.Max(12, endings.Count);
            so.FindProperty("totalEventCount").intValue = Mathf.Max(55, CountPlayableEvents(events));
            so.FindProperty("totalTraitCount").intValue = 4;
            so.FindProperty("totalAchievementCount").intValue = AchievementIds.CatalogCount;
            WireList(so.FindProperty("endingCatalog"), endings);
            WireList(so.FindProperty("eventCatalog"), events);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[MainMenuCodexSetup] Codex panel 정리 완료. endings={endings.Count}, events={events.Count}");
        }

        private static void WireList<T>(SerializedProperty property, System.Collections.Generic.List<T> values)
            where T : Object
        {
            property.ClearArray();
            for (var i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static int CountPlayableEvents(System.Collections.Generic.List<EventData> events)
        {
            var count = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Id != "event_rest_fallback")
                {
                    count++;
                }
            }

            return count;
        }

        private static System.Collections.Generic.List<EndingData> LoadEndings()
        {
            var list = new System.Collections.Generic.List<EndingData>();
            var guids = AssetDatabase.FindAssets("t:EndingData", new[] { EndingsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ending = AssetDatabase.LoadAssetAtPath<EndingData>(path);
                if (ending != null)
                {
                    list.Add(ending);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        private static System.Collections.Generic.List<EventData> LoadEvents()
        {
            var list = new System.Collections.Generic.List<EventData>();
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/Data/Events" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData != null)
                {
                    list.Add(eventData);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }
    }
}
