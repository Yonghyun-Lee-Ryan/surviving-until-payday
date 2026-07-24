using System.IO;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 21: ArtCatalog 생성 + EventPanel 배경/표정 슬롯 보강.
    /// </summary>
    public static class ArtPipelineSetup
    {
        private const string CatalogFolder = "Assets/Data/Art";
        private const string ResourcesFolder = "Assets/Resources/Art";
        private const string CatalogAssetPath = CatalogFolder + "/ArtCatalog.asset";
        private const string ResourcesCatalogPath = ResourcesFolder + "/ArtCatalog.asset";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Surviving Until Payday/Setup Art Pipeline (Unit 21)")]
        public static void Setup()
        {
            EnsureCatalog();
            UpgradeEventPanelInGameScene();
            Debug.Log(
                "[ArtPipelineSetup] ArtCatalog + EventPanel 배경/표정 슬롯 준비 완료.\n" +
                "실에셋은 Docs/ArtPipeline.md 경로에 넣고 ArtCatalog 슬롯에 할당하세요.");
        }

        private static void EnsureCatalog()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(CatalogFolder);
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResourcesFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                catalog.EditorEnsureSlotSizes();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }
            else
            {
                catalog.EditorEnsureSlotSizes();
                EditorUtility.SetDirty(catalog);
            }

            // Resources 로드용 복제(동일 참조가 아니면 복사)
            var resourcesCatalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(ResourcesCatalogPath);
            if (resourcesCatalog == null)
            {
                AssetDatabase.CopyAsset(CatalogAssetPath, ResourcesCatalogPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void UpgradeEventPanelInGameScene()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogWarning("[ArtPipelineSetup] Game.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<GamePlayPresenter>();
            var eventPanel = Object.FindAnyObjectByType<EventPanelView>();
            if (presenter == null || eventPanel == null)
            {
                Debug.LogWarning("[ArtPipelineSetup] GamePlayPresenter/EventPanelView missing. Run Unit 7 setup first.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(CatalogAssetPath);
            var so = new SerializedObject(presenter);
            so.FindProperty("artCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);

            var root = eventPanel.transform;
            var background = root.Find("Background")?.GetComponent<Image>()
                             ?? root.Find("Illustration")?.GetComponent<Image>();
            var expression = EnsureExpressionImage(root);

            // 플레이스홀더 텍스트/네모는 제거한다.
            DestroyChild(root, "BgPlaceholder");
            DestroyChild(root, "FacePlaceholder");
            DestroyChild(root, "Placeholder");
            if (background != null)
            {
                DestroyChild(background.transform, "BgPlaceholder");
                DestroyChild(background.transform, "Placeholder");
            }

            if (expression != null)
            {
                DestroyChild(expression.transform, "FacePlaceholder");
                expression.preserveAspect = true;
                expression.raycastTarget = false;
                if (expression.sprite == null)
                {
                    expression.enabled = false;
                    expression.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            if (background != null)
            {
                background.preserveAspect = true;
                background.raycastTarget = false;
            }

            var title = root.Find("Title")?.GetComponent<Text>();
            if (title == null)
            {
                title = CreateChildLabel(root, "Title", "사건 제목");
                var tr = title.rectTransform;
                tr.anchorMin = new Vector2(0f, 0.5f);
                tr.anchorMax = new Vector2(1f, 0.5f);
                tr.anchoredPosition = new Vector2(0f, -40f);
                tr.sizeDelta = new Vector2(-48f, 52f);
            }

            var description = root.Find("Description")?.GetComponent<Text>();

            if (background != null && expression != null && title != null && description != null)
            {
                eventPanel.Bind(title, description, background, null, expression, null);
                EditorUtility.SetDirty(eventPanel);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            var child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Image EnsureExpressionImage(Transform eventRoot)
        {
            var existing = eventRoot.Find("Expression");
            if (existing != null)
            {
                var rect = existing.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-28f, -200f);
                rect.sizeDelta = new Vector2(150f, 150f);
                return existing.GetComponent<Image>();
            }

            var go = new GameObject("Expression", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(eventRoot, false);
            var newRect = go.GetComponent<RectTransform>();
            newRect.anchorMin = newRect.anchorMax = new Vector2(1f, 1f);
            newRect.pivot = new Vector2(1f, 1f);
            newRect.anchoredPosition = new Vector2(-28f, -200f);
            newRect.sizeDelta = new Vector2(150f, 150f);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.enabled = false;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateChildLabel(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.Load<Font>("Fonts/NotoSansKR-Regular")
                         ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            return label;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
