using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// MainMenu 회차 시작 패널. 스크롤 중앙 + 상·하단 여백에 텍스트/버튼 배치.
    /// </summary>
    public static class MainMenuRunStartSetup
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Surviving Until Payday/Setup MainMenu Run Start Panel (Unit 18)")]
        public static void Setup()
        {
            if (!File.Exists(MainMenuPath))
            {
                Debug.LogError("[MainMenuRunStartSetup] MainMenu.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogError("[MainMenuRunStartSetup] MainMenuController/Canvas missing.");
                return;
            }

            var existing = canvas.transform.Find("RunStartPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panelRoot = new GameObject("RunStartPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);
            panelRoot.SetActive(false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var jobTitle = CreateTopText(panelRoot.transform, "JobTitle", "직업", 42, 40f, 48f);
            var jobDesc = CreateTopText(panelRoot.transform, "JobDescription", "설명", 26, 96f, 96f);
            jobDesc.alignment = TextAnchor.MiddleCenter;
            var hint = CreateTopText(panelRoot.transform, "TraitHint", "특성 선택", 28, 200f, 36f);
            var selected = CreateTopText(panelRoot.transform, "SelectedTrait", "선택: 특성 없음", 26, 244f, 100f);
            selected.alignment = TextAnchor.UpperCenter;

            var scrollGo = new GameObject("TraitScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panelRoot.transform, false);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.05f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.95f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = Vector2.zero;
            scrollRectTransform.sizeDelta = new Vector2(0f, 720f);
            scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.75f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 10f);
            viewportRect.offsetMax = new Vector2(-10f, -10f);
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("TraitButtonRoot", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(8, 8, 8, 8);
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var confirmButton = CreateBottomButton(panelRoot.transform, "ConfirmButton", "시작",
                120f, 520f, 88f, new Color(0.2f, 0.55f, 0.35f, 1f));
            var cancelButton = CreateBottomButton(panelRoot.transform, "CancelButton", "취소",
                28f, 520f, 88f, new Color(0.45f, 0.25f, 0.25f, 1f));

            var view = panelRoot.AddComponent<RunStartPanelView>();
            view.Bind(
                panelRoot,
                jobTitle,
                jobDesc,
                hint,
                contentGo.transform,
                null,
                confirmButton,
                cancelButton,
                selected,
                scroll);

            var jobs = LoadAll<JobData>("Assets/Data/Jobs");
            var traits = LoadAll<TraitData>("Assets/Data/Traits");
            jobs.RemoveAll(j => j == null);
            traits.RemoveAll(t => t == null);
            jobs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            traits.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var job = jobs.Count > 0 ? jobs[0] : null;
            for (var i = 0; i < jobs.Count; i++)
            {
                if (jobs[i] != null && jobs[i].Id == "job_junior_office")
                {
                    job = jobs[i];
                    break;
                }
            }

            var so = new SerializedObject(controller);
            so.FindProperty("runStartPanel").objectReferenceValue = view;
            so.FindProperty("defaultJob").objectReferenceValue = job;
            so.FindProperty("totalEventCount").intValue = Mathf.Max(40, CountPlayableEvents());
            so.FindProperty("totalJobCount").intValue = jobs.Count;
            so.FindProperty("totalTraitCount").intValue = traits.Count;
            var jobCatalogProp = so.FindProperty("jobCatalog");
            jobCatalogProp.ClearArray();
            for (var i = 0; i < jobs.Count; i++)
            {
                jobCatalogProp.InsertArrayElementAtIndex(i);
                jobCatalogProp.GetArrayElementAtIndex(i).objectReferenceValue = jobs[i];
            }

            var catalogProp = so.FindProperty("traitCatalog");
            catalogProp.ClearArray();
            for (var i = 0; i < traits.Count; i++)
            {
                catalogProp.InsertArrayElementAtIndex(i);
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = traits[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[MainMenuRunStartSetup] RunStartPanel + jobCatalog({jobs.Count}) traitCatalog({traits.Count}) 적용 완료.");
        }

        private static int CountPlayableEvents()
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/Data/Events" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData != null && eventData.Id != "event_rest_fallback")
                {
                    count++;
                }
            }

            return count;
        }

        private static List<T> LoadAll<T>(string folder) where T : ScriptableObject
        {
            var list = new List<T>();
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        private static Text CreateTopText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            float topInset,
            float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 1f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topInset);
            rect.sizeDelta = new Vector2(0f, height);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                         ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button CreateBottomButton(
            Transform parent,
            string name,
            string label,
            float bottom,
            float width,
            float height,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return button;
        }
    }
}
