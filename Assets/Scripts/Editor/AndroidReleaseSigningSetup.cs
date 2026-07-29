using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Google Play 업로드용 Release Keystore 생성·Player Settings 연결.
    /// </summary>
    public static class AndroidReleaseSigningSetup
    {
        private const string MenuPath = "Tools/Surviving Until Payday/Setup Android Release Signing";
        private const string KeystoreFolder = "Keystore";
        private const string KeystoreFileName = "release.keystore";
        private const string PropertiesFileName = "android-signing.properties";
        private const string DefaultAlias = "release";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[AndroidReleaseSigningSetup] Project root not found.");
                return;
            }

            var keystoreDir = Path.Combine(projectRoot, KeystoreFolder);
            Directory.CreateDirectory(keystoreDir);

            var keystorePath = Path.Combine(keystoreDir, KeystoreFileName);
            var propsPath = Path.Combine(keystoreDir, PropertiesFileName);
            var props = LoadOrCreateProperties(propsPath);

            if (!File.Exists(keystorePath))
            {
                if (!TryCreateKeystore(keystorePath, props))
                {
                    return;
                }

                Debug.Log($"[AndroidReleaseSigningSetup] Keystore created: {keystorePath}");
            }
            else
            {
                Debug.Log($"[AndroidReleaseSigningSetup] Using existing keystore: {keystorePath}");
            }

            ApplyPlayerSettings(keystorePath, props);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[AndroidReleaseSigningSetup] Release signing applied.\n" +
                "1) File > Build Settings > Development Build OFF\n" +
                "2) Build App Bundle로 AAB를 다시 만든 뒤 Play Console에 업로드하세요.\n" +
                $"3) Keystore 백업: {keystorePath} (분실 시 업데이트 불가)");
        }

        private static AndroidSigningProperties LoadOrCreateProperties(string propsPath)
        {
            if (File.Exists(propsPath))
            {
                return AndroidSigningProperties.Parse(File.ReadAllText(propsPath));
            }

            var generated = AndroidSigningProperties.CreateDefault();
            File.WriteAllText(propsPath, generated.ToFileText(), Encoding.UTF8);
            Debug.LogWarning(
                $"[AndroidReleaseSigningSetup] Created {propsPath}. " +
                "storePassword/keyPassword를 변경한 뒤 Keystore를 재생성하거나 기존 파일을 유지하세요.");
            return generated;
        }

        private static bool TryCreateKeystore(string keystorePath, AndroidSigningProperties props)
        {
            var keytool = ResolveKeytoolPath();
            if (string.IsNullOrEmpty(keytool))
            {
                Debug.LogError(
                    "[AndroidReleaseSigningSetup] keytool not found. " +
                    "Unity Android Build Support(JDK) 또는 Android Studio JBR을 설치하세요.");
                return false;
            }

            var dname = props.DistinguishedName;
            if (string.IsNullOrWhiteSpace(dname))
            {
                dname = "CN=Survive Until Payday, OU=Mobile, O=SurviveUntilPayday, L=Seoul, ST=Seoul, C=KR";
            }

            var args =
                $"-genkeypair -v " +
                $"-keystore \"{keystorePath}\" " +
                $"-alias {props.KeyAlias} " +
                "-keyalg RSA -keysize 2048 -validity 10000 " +
                $"-storepass {props.StorePassword} " +
                $"-keypass {props.KeyPassword} " +
                $"-dname \"{dname}\"";

            var exitCode = RunProcess(keytool, args, out var stdout, out var stderr);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Debug.Log(stdout);
            }

            if (exitCode != 0)
            {
                Debug.LogError($"[AndroidReleaseSigningSetup] keytool failed ({exitCode}): {stderr}");
                return false;
            }

            return File.Exists(keystorePath);
        }

        private static void ApplyPlayerSettings(string keystorePath, AndroidSigningProperties props)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keyaliasName = props.KeyAlias;
            PlayerSettings.Android.keystorePass = props.StorePassword;
            PlayerSettings.Android.keyaliasPass = props.KeyPassword;

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

            var previousCode = Mathf.Max(0, PlayerSettings.Android.bundleVersionCode);
            var nextCode = ReleasePrepSetup.BumpAndroidBundleVersionCode();

            Debug.Log(
                "[AndroidReleaseSigningSetup] PlayerSettings Android signing configured. " +
                $"alias={props.KeyAlias}, development={EditorUserBuildSettings.development}, " +
                $"versionCode={previousCode}→{nextCode}");
        }

        private static string ResolveKeytoolPath()
        {
            var envJavaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(envJavaHome))
            {
                var fromJavaHome = Path.Combine(envJavaHome, "bin", "keytool.exe");
                if (File.Exists(fromJavaHome))
                {
                    return fromJavaHome;
                }
            }

            var hub = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Unity",
                "Hub",
                "Editor");
            if (Directory.Exists(hub))
            {
                var editors = Directory.GetDirectories(hub);
                Array.Sort(editors, StringComparer.OrdinalIgnoreCase);
                Array.Reverse(editors);
                for (var i = 0; i < editors.Length; i++)
                {
                    var candidate = Path.Combine(
                        editors[i],
                        "Editor",
                        "Data",
                        "PlaybackEngines",
                        "AndroidPlayer",
                        "OpenJDK",
                        "bin",
                        "keytool.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            var androidJbr = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Android",
                "Android Studio",
                "jbr",
                "bin",
                "keytool.exe");
            return File.Exists(androidJbr) ? androidJbr : null;
        }

        private static int RunProcess(string fileName, string arguments, out string stdout, out string stderr)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                stdout = string.Empty;
                stderr = "Process start failed.";
                return -1;
            }

            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode;
        }

        private sealed class AndroidSigningProperties
        {
            public string KeyAlias { get; set; } = DefaultAlias;
            public string StorePassword { get; set; } = string.Empty;
            public string KeyPassword { get; set; } = string.Empty;
            public string DistinguishedName { get; set; } = string.Empty;

            public static AndroidSigningProperties CreateDefault()
            {
                // 내부 테스트용 기본값. Keystore/는 gitignore — 배포 전 비밀번호 변경 권장.
                var password = "SurviveUntilPayday2026!";
                return new AndroidSigningProperties
                {
                    KeyAlias = DefaultAlias,
                    StorePassword = password,
                    KeyPassword = password,
                    DistinguishedName =
                        "CN=Survive Until Payday, OU=Mobile, O=SurviveUntilPayday, L=Seoul, ST=Seoul, C=KR"
                };
            }

            public static AndroidSigningProperties Parse(string text)
            {
                var result = CreateDefault();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return result;
                }

                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var sep = line.IndexOf('=');
                    if (sep <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, sep).Trim();
                    var value = line.Substring(sep + 1).Trim();
                    switch (key)
                    {
                        case "keyAlias":
                            result.KeyAlias = value;
                            break;
                        case "storePassword":
                            result.StorePassword = value;
                            break;
                        case "keyPassword":
                            result.KeyPassword = value;
                            break;
                        case "distinguishedName":
                            result.DistinguishedName = value;
                            break;
                    }
                }

                return result;
            }

            public string ToFileText()
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Google Play Release signing (gitignore). 배포 전 비밀번호 변경 권장.");
                sb.AppendLine($"keyAlias={KeyAlias}");
                sb.AppendLine($"storePassword={StorePassword}");
                sb.AppendLine($"keyPassword={KeyPassword}");
                sb.AppendLine($"distinguishedName={DistinguishedName}");
                return sb.ToString();
            }
        }
    }
}
