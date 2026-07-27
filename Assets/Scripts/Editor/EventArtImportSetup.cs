using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Resources/Art/Events 아래 PNG를 Sprite(2D UI)로 임포트 설정한다.
    /// </summary>
    public static class EventArtImportSetup
    {
        private const string EventsFolder = "Assets/Resources/Art/Events";
        private const string MenuPath = "Tools/Surviving Until Payday/Import Event Illustrations";

        [MenuItem(MenuPath)]
        public static void Import()
        {
            EnsureFolder();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { EventsFolder });
            var count = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".png"))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EventArtImportSetup] Sprite 설정 완료: {count}개 ({EventsFolder})");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Art"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Art");
            }

            if (!AssetDatabase.IsValidFolder(EventsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Art", "Events");
            }

            if (!Directory.Exists(EventsFolder))
            {
                Directory.CreateDirectory(EventsFolder);
            }
        }
    }
}
