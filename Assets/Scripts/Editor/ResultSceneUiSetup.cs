using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 8: Result Scene UI + GamePlayPresenter 엔딩 연결.
    /// </summary>
    public static class ResultSceneUiSetup
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Surviving Until Payday/Setup Result Scene UI (Unit 8)")]
        public static void Setup()
        {
            if (!File.Exists(ResultScenePath))
            {
                Debug.LogError("[ResultSceneUiSetup] Result.unity missing. Run Foundation setup first.");
                return;
            }

            SampleEndingFactory.CreateSampleEndings();
            BuildResultScene();
            WireEndingsToGamePresenter();

            Debug.Log(
                "[ResultSceneUiSetup] Done.\n" +
                "Play a run to Day 30 or fail, then check Result Scene ending/codex text.");
        }

        private static void BuildResultScene()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            foreach (var old in Object.FindObjectsByType<ResultSceneController>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(old);
            }

            foreach (var old in Object.FindObjectsByType<ResultPresenter>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(old.gameObject);
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                CanvasSetupUtility.ApplyPortraitCanvasScaler(scaler);
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var safeArea = canvas.transform.Find("SafeArea");
            if (safeArea == null)
            {
                var safe = new GameObject("SafeArea", typeof(RectTransform));
                safe.transform.SetParent(canvas.transform, false);
                Stretch(safe.GetComponent<RectTransform>());
                safe.AddComponent<SafeAreaFitter>();
                safeArea = safe.transform;
            }

            for (var i = safeArea.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(safeArea.GetChild(i).gameObject);
            }

            var title = CreateText(safeArea, "Title", "월급날 생존!", 52, new Vector2(0f, 760f));
            var endingTitle = CreateText(safeArea, "EndingTitle", "엔딩", 44, new Vector2(0f, 620f));
            var endingDesc = CreateText(safeArea, "EndingDesc", "설명", 30, new Vector2(0f, 480f));
            endingDesc.rectTransform.sizeDelta = new Vector2(900f, 160f);
            var days = CreateText(safeArea, "Days", "생존 일수", 34, new Vector2(0f, 300f));
            var cash = CreateText(safeArea, "Cash", "현금", 34, new Vector2(0f, 230f));
            var stats = CreateText(safeArea, "Stats", "능력치", 30, new Vector2(0f, 120f));
            stats.rectTransform.sizeDelta = new Vector2(900f, 100f);
            var xp = CreateText(safeArea, "XP", "경험치", 32, new Vector2(0f, 0f));
            var unlock = CreateText(safeArea, "Unlock", "도감", 28, new Vector2(0f, -80f));

            var doubleXpObject = CreatePanel(safeArea, "DoubleXpAdButton", new Vector2(0f, -160f), new Vector2(480f, 90f),
                new Color(0.45f, 0.32f, 0.18f));
            var doubleXpButton = doubleXpObject.AddComponent<Button>();
            doubleXpButton.targetGraphic = doubleXpObject.GetComponent<Image>();
            var doubleXpLabel = CreateText(doubleXpObject.transform, "Label", "광고로 경험치 2배", 30, Vector2.zero);
            doubleXpLabel.color = Color.white;

            var buttonObject = CreatePanel(safeArea, "BackButton", new Vector2(0f, -280f), new Vector2(480f, 110f),
                new Color(0.18f, 0.42f, 0.55f));
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var buttonLabel = CreateText(buttonObject.transform, "Label", "메인 메뉴로", 36, Vector2.zero);
            buttonLabel.color = Color.white;

            var presenterObject = new GameObject("ResultPresenter");
            var presenter = presenterObject.AddComponent<ResultPresenter>();
            presenter.Bind(title, endingTitle, endingDesc, days, cash, stats, xp, unlock, button);
            presenter.BindDoubleXpButton(doubleXpButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireEndingsToGamePresenter()
        {
            if (!File.Exists(GameScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<GamePlayPresenter>();
            if (presenter == null)
            {
                Debug.LogWarning("[ResultSceneUiSetup] GamePlayPresenter not found. Run Game Scene UI setup first.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:EndingData", new[] { "Assets/Data/Endings" });
            var endings = new List<EndingData>();
            EndingData fallback = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ending = AssetDatabase.LoadAssetAtPath<EndingData>(path);
                if (ending == null)
                {
                    continue;
                }

                endings.Add(ending);
                if (ending.Id == "ending_barely_survived")
                {
                    fallback = ending;
                }
            }

            if (fallback == null && endings.Count > 0)
            {
                fallback = endings[0];
            }

            var so = new SerializedObject(presenter);
            var catalog = so.FindProperty("endingCatalog");
            catalog.ClearArray();
            for (var i = 0; i < endings.Count; i++)
            {
                catalog.InsertArrayElementAtIndex(i);
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = endings[i];
            }

            so.FindProperty("fallbackSuccessEnding").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(900f, 70f);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.15f, 0.18f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
