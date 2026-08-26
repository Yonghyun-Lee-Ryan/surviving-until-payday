using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-08: Adaptive Icon PNG 생성 후 Android Player Settings에 할당.
    /// </summary>
    public static class AndroidAdaptiveIconSetup
    {
        public const string IconsFolder = "Assets/Art/Icons";
        public const string BackgroundPath = IconsFolder + "/adaptive_background.png";
        public const string ForegroundPath = IconsFolder + "/adaptive_foreground.png";
        public const string LegacyPath = IconsFolder + "/legacy_icon.png";

        [MenuItem("Tools/Surviving Until Payday/Assign Android Adaptive Icons (R-QA-08)")]
        public static void Assign()
        {
            EnsureFolder();
            WritePng(BackgroundPath, 432, DrawBackground);
            WritePng(ForegroundPath, 432, DrawForeground);
            WritePng(LegacyPath, 192, DrawLegacy);

            AssetDatabase.ImportAsset(BackgroundPath);
            AssetDatabase.ImportAsset(ForegroundPath);
            AssetDatabase.ImportAsset(LegacyPath);
            ConfigureImporter(BackgroundPath);
            ConfigureImporter(ForegroundPath);
            ConfigureImporter(LegacyPath);
            AssetDatabase.ImportAsset(BackgroundPath);
            AssetDatabase.ImportAsset(ForegroundPath);
            AssetDatabase.ImportAsset(LegacyPath);

            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
            var legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyPath);
            if (background == null || foreground == null || legacy == null)
            {
                throw new FileNotFoundException("[R-QA-08] Adaptive Icon 텍스처를 불러오지 못했습니다.");
            }

            AssignAdaptive(background, foreground);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { legacy }, IconKind.Application);

            AssetDatabase.SaveAssets();
            Debug.Log("[R-QA-08] Android Adaptive Icon 할당 완료.");
        }

        public static bool HasAssignedAdaptiveIcon()
        {
            var icons = PlayerSettings.GetPlatformIcons(
                NamedBuildTarget.Android,
                UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
            for (var i = 0; i < icons.Length; i++)
            {
                var textures = icons[i].GetTextures();
                if (textures == null)
                {
                    continue;
                }

                for (var t = 0; t < textures.Length; t++)
                {
                    if (textures[t] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AssignAdaptive(Texture2D background, Texture2D foreground)
        {
            var kind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i].SetTexture(background, 0);
                icons[i].SetTexture(foreground, 1);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
            {
                AssetDatabase.CreateFolder("Assets", "Art");
            }

            if (!AssetDatabase.IsValidFolder(IconsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Icons");
            }
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
            importer.sRGBTexture = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private delegate void DrawPixels(Color32[] pixels, int size);

        private static void WritePng(string path, int size, DrawPixels draw)
        {
            var pixels = new Color32[size * size];
            draw(pixels, size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void DrawBackground(Color32[] pixels, int size)
        {
            var color = new Color32(27, 42, 65, 255);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        private static void DrawForeground(Color32[] pixels, int size)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            var cx = size / 2;
            var cy = size / 2;
            var radius = Mathf.RoundToInt(size * 0.28f);
            var gold = new Color32(232, 196, 104, 255);
            var ink = new Color32(27, 42, 65, 255);
            FillCircle(pixels, size, cx, cy, radius, gold);
            FillRect(pixels, size, cx - radius / 2, cy - radius / 6, radius, radius / 3, ink);
        }

        private static void DrawLegacy(Color32[] pixels, int size)
        {
            DrawBackground(pixels, size);
            DrawForeground(pixels, size);
        }

        private static void FillCircle(Color32[] pixels, int size, int cx, int cy, int radius, Color32 color)
        {
            var r2 = radius * radius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                if (y < 0 || y >= size)
                {
                    continue;
                }

                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= size)
                    {
                        continue;
                    }

                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        private static void FillRect(Color32[] pixels, int size, int x0, int y0, int w, int h, Color32 color)
        {
            for (var y = y0; y < y0 + h; y++)
            {
                if (y < 0 || y >= size)
                {
                    continue;
                }

                for (var x = x0; x < x0 + w; x++)
                {
                    if (x < 0 || x >= size)
                    {
                        continue;
                    }

                    pixels[y * size + x] = color;
                }
            }
        }
    }
}
