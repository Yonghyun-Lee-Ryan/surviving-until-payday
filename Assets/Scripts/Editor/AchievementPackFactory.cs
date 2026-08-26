using System.IO;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-05: 업적 표시 SO 20개를 Resources/Achievements에 생성한다.
    /// </summary>
    public static class AchievementPackFactory
    {
        private const string Folder = "Assets/Resources/Achievements";

        [MenuItem("Tools/Surviving Until Payday/Create Achievement Pack (R-QA-05)")]
        public static void CreatePack()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(Folder);

            var catalog = AchievementIds.Catalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                Create(def.Id, def.Title, def.Description);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AchievementCatalog.InvalidateCache();
            Debug.Log($"[R-QA-05] 업적 SO {catalog.Count}개 생성: {Folder}");
        }

        private static void Create(string id, string title, string description)
        {
            var fileName = ToFileName(id);
            var path = $"{Folder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AchievementData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(id, title, description);
            EditorUtility.SetDirty(asset);
        }

        private static string ToFileName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "Achievement_Unknown";
            }

            var chars = id.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '-')
                {
                    chars[i] = '_';
                }
            }

            return "Achievement_" + new string(chars);
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
