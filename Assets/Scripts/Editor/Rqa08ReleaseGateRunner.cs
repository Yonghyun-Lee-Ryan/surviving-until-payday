using System;
using System.IO;
using System.Text;
using SurviveUntilPayday.Services;
using SurviveUntilPayday.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-08: placeholder URL·아이콘·AAB·SDK Define 게이트. versionCode는 올리지 않는다.
    /// batch: -executeMethod SurviveUntilPayday.EditorTools.Rqa08ReleaseGateRunner.RunFromBatch
    /// </summary>
    public static class Rqa08ReleaseGateRunner
    {
        private const string PrivacyAsset = "Assets/Data/Config/PrivacyPolicyConfig.asset";
        private const string AndroidId = "com.surviveuntilpayday.game";

        [MenuItem("Tools/Surviving Until Payday/Validate Release Gate (R-QA-08)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[R-QA-08] 게이트 리포트: {path}");
        }

        public static void RunFromBatch()
        {
            try
            {
                ReleasePrepSetup.ApplyAndroidReleasePlayerSettings(bumpVersionCode: false);
                AndroidAdaptiveIconSetup.Assign();
                var privacy = AssetDatabase.LoadAssetAtPath<PrivacyPolicyConfig>(PrivacyAsset);
                if (privacy != null && privacy.HasPlaceholderUrl)
                {
                    privacy.EditorSet(PrivacyPolicyUrls.Canonical, privacy.SummaryText);
                    EditorUtility.SetDirty(privacy);
                    AssetDatabase.SaveAssets();
                }

                var path = RunAndSaveReport();
                Debug.Log($"[R-QA-08] batch OK: {path}");
                EditorApplication.Exit(reportFailed ? 1 : 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-08] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static bool reportFailed;

        public static string RunAndSaveReport()
        {
            reportFailed = false;
            var sb = new StringBuilder();
            sb.AppendLine("=== R-QA-08 Release Gate ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity: {Application.unityVersion}");
            sb.AppendLine();

            Check("개인정보 URL placeholder 아님", CheckPrivacyUrl(sb), sb);
            Check("Adaptive Icon 할당", AndroidAdaptiveIconSetup.HasAssignedAdaptiveIcon(), sb);
            Check("Application Id", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == AndroidId, sb);
            Check("Min SDK 26+", PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel26, sb);
            Check("IL2CPP", PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, sb);
            Check("ARM64", (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0, sb);
            Check("Build App Bundle", EditorUserBuildSettings.buildAppBundle, sb);
            Check("versionCode >= 1", PlayerSettings.Android.bundleVersionCode >= 1, sb);
            Check("광고 실패 시 진행 (Mock Failed)", CheckAdFailureDoesNotThrow(), sb);

            sb.AppendLine();
            sb.AppendLine("## SDK Define 경로");
            sb.AppendLine($"GOOGLE_MOBILE_ADS={SdkDefines.HasGoogleMobileAds} (UPM com.google.ads.mobile 또는 Scripting Define)");
            sb.AppendLine($"FIREBASE_ANALYTICS={SdkDefines.HasFirebaseAnalytics}");
            sb.AppendLine($"FIREBASE_CRASHLYTICS={SdkDefines.HasFirebaseCrashlytics}");
            sb.AppendLine("Editor는 TestDevice/Mock. 기기+패키지이면 AdMob 래퍼. AllowRealAdsInEditor는 기본 꺼짐.");
            sb.AppendLine("테스트 광고 유닛: SdkIntegrationConfig Google Test IDs.");
            sb.AppendLine("google-services.json → Assets/ (gitignore). 예: Assets/google-services.json.example");
            sb.AppendLine("상점/IAP 경로 없음.");
            sb.AppendLine();
            sb.AppendLine($"AAB 모듈: {BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)}");
            sb.AppendLine($"Development Build: {EditorUserBuildSettings.development}");
            sb.AppendLine();
            sb.AppendLine(reportFailed ? "RESULT: FAIL" : "RESULT: PASS");

            var logs = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logs);
            var file = Path.Combine(logs, $"rqa08_release_gate_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            return file;
        }

        private static void Check(string name, bool ok, StringBuilder sb)
        {
            if (!ok)
            {
                reportFailed = true;
            }

            sb.AppendLine($"- {(ok ? "PASS" : "FAIL")} {name}");
        }

        private static bool CheckPrivacyUrl(StringBuilder sb)
        {
            var config = AssetDatabase.LoadAssetAtPath<PrivacyPolicyConfig>(PrivacyAsset);
            if (config == null)
            {
                sb.AppendLine("  (PrivacyPolicyConfig missing)");
                return false;
            }

            sb.AppendLine($"  url={config.PolicyUrl}");
            return PrivacyPolicyUrls.IsHttpsPublicUrl(config.PolicyUrl);
        }

        private static bool CheckAdFailureDoesNotThrow()
        {
            try
            {
                var mock = new SurviveUntilPayday.Ads.MockAdService();
                mock.SetForceRewardedFailure(true, "gate");
                var wrapped = new AdMobAdService(mock);
                SurviveUntilPayday.Ads.AdShowResult? result = null;
                wrapped.ShowRewardedAd(SurviveUntilPayday.Ads.RewardedAdPlacement.ChoiceReroll, r => result = r);
                return result.HasValue && !result.Value.IsSuccess;
            }
            catch
            {
                return false;
            }
        }
    }
}
