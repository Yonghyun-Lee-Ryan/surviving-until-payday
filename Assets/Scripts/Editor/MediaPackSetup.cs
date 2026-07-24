using System.IO;
using SurviveUntilPayday.Art;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 미디어 팩: 스프라이트 임포트 + ArtCatalog 슬롯 + 한글 폰트 확인.
    /// </summary>
    public static class MediaPackSetup
    {
        private const string CatalogPath = "Assets/Data/Art/ArtCatalog.asset";
        private const string ResourcesCatalogPath = "Assets/Resources/Art/ArtCatalog.asset";

        private static readonly string[] BackgroundPaths =
        {
            "Assets/Art/Backgrounds/bg_home.png",
            "Assets/Art/Backgrounds/bg_office.png",
            "Assets/Art/Backgrounds/bg_subway.png",
            "Assets/Art/Backgrounds/bg_restaurant.png",
            "Assets/Art/Backgrounds/bg_hospital.png",
            "Assets/Art/Backgrounds/bg_extra_night.png",
            "Assets/Art/Backgrounds/bg_extra_desk.png",
            "Assets/Art/Backgrounds/bg_extra_city.png"
        };

        private static readonly string[] ExpressionPaths =
        {
            "Assets/Art/Expressions/face_default.png",
            "Assets/Art/Expressions/face_happy.png",
            "Assets/Art/Expressions/face_surprised.png",
            "Assets/Art/Expressions/face_angry.png",
            "Assets/Art/Expressions/face_tired.png",
            "Assets/Art/Expressions/face_despair.png"
        };

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Result.unity"
        };

        [MenuItem("Tools/Surviving Until Payday/Import Media Pack (Art·Audio·Font)")]
        public static void Setup()
        {
            EnsureSpriteImports(BackgroundPaths);
            EnsureSpriteImports(ExpressionPaths);
            EnsureSpriteImports(new[]
            {
                "Assets/Art/UI/ui_panel.png",
                "Assets/Art/UI/ui_menu_bg.png"
            });

            AssetDatabase.Refresh();

            BindArtCatalog(CatalogPath);
            if (AssetDatabase.LoadAssetAtPath<ArtCatalog>(ResourcesCatalogPath) != null)
            {
                BindArtCatalog(ResourcesCatalogPath);
            }
            else if (File.Exists(CatalogPath))
            {
                AssetDatabase.CopyAsset(CatalogPath, ResourcesCatalogPath);
                BindArtCatalog(ResourcesCatalogPath);
            }

            EnsureFontBootstraps();

            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR/NotoSansKR-Regular.otf")
                       ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotoSansKR-Regular.otf");
            var audioOk = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/bgm_main.ogg") != null
                          || AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/bgm_main.wav") != null;

            Debug.Log(
                "[MediaPackSetup] 완료.\n" +
                "· ArtCatalog 배경/표정 슬롯 연결\n" +
                $"· 한글 폰트: {(font != null ? font.name : "미검출 — Unity 재임포트 후 재실행")}\n" +
                $"· Audio Resources: {(audioOk ? "OK" : "확인 필요")}\n" +
                "· MainMenu/Game/Result에 UiFontBootstrap 부착\n" +
                "라이선스: Docs/AssetCredits.md");
        }

        private static void EnsureFontBootstraps()
        {
            foreach (var scenePath in ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    continue;
                }

                if (canvas.GetComponent<UiFontBootstrap>() == null)
                {
                    canvas.gameObject.AddComponent<UiFontBootstrap>();
                    EditorUtility.SetDirty(canvas.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void EnsureSpriteImports(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[MediaPackSetup] Missing: {path}");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(path);
                    importer = AssetImporter.GetAtPath(path) as TextureImporter;
                }

                if (importer == null)
                {
                    continue;
                }

                var dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void BindArtCatalog(string catalogPath)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.EditorEnsureSlotSizes();
            var so = new SerializedObject(catalog);
            var backgrounds = so.FindProperty("backgrounds");
            var expressions = so.FindProperty("expressions");
            backgrounds.arraySize = 8;
            expressions.arraySize = 6;

            for (var i = 0; i < BackgroundPaths.Length && i < 8; i++)
            {
                backgrounds.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPaths[i]);
            }

            for (var i = 0; i < ExpressionPaths.Length && i < 6; i++)
            {
                expressions.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ExpressionPaths[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
