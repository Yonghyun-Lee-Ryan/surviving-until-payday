using SurviveUntilPayday.Services;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 14: SDK Integration Config 에셋 생성.
    /// </summary>
    public static class SdkIntegrationSetup
    {
        private const string Folder = "Assets/Data/Config";
        private const string AssetPath = Folder + "/SdkIntegrationConfig.asset";
        private const string MenuPath = "Tools/Surviving Until Payday/Setup SDK Integration Config (Unit 14)";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Config");
            }

            var existing = AssetDatabase.LoadAssetAtPath<SdkIntegrationConfig>(AssetPath);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<SdkIntegrationConfig>();
                existing.EditorSet(
                    realAds: false,
                    everyN: 3,
                    cooldown: 2f,
                    testInEditor: true,
                    mirrorDebug: true,
                    crashCapture: true);
                existing.EditorSetAdUnits(
                    googleTestUnits: true,
                    appId: SdkIntegrationConfig.GoogleTestAppId,
                    rewardedUnit: SdkIntegrationConfig.GoogleTestRewardedUnitId,
                    interstitialUnit: SdkIntegrationConfig.GoogleTestInterstitialUnitId,
                    testDevices: System.Array.Empty<string>(),
                    umpForceEea: false,
                    allowRealInEditor: false);
                AssetDatabase.CreateAsset(existing, AssetPath);
            }

            var bootstrap = Object.FindAnyObjectByType<SurviveUntilPayday.Core.AppRoot>();
            if (bootstrap != null)
            {
                bootstrap.BindSdkConfig(existing);
                EditorUtility.SetDirty(bootstrap);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = existing;
            Debug.Log(
                "[SdkIntegrationSetup] SdkIntegrationConfig ready at " + AssetPath + "\n" +
                "1) Bootstrap AppRoot Inspector에서 sdkConfig를 할당하세요.\n" +
                "2) interstitialEveryNRuns로 전면 광고 빈도를 조절할 수 있습니다.\n" +
                "3) 실제 AdMob/Firebase: com.google.ads.mobile / Firebase UPM 설치 시 asmdef versionDefines가 심볼을 켭니다. " +
                ".unitypackage면 Player Settings Scripting Define에 GOOGLE_MOBILE_ADS / FIREBASE_ANALYTICS / FIREBASE_CRASHLYTICS. " +
                "테스트 기기 hashed ID는 SdkIntegrationConfig.testDeviceHashedIds.");
        }
    }
}
