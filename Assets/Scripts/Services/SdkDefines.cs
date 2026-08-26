namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// 실 SDK 전환 심볼. UPM 패키지가 있으면 asmdef versionDefines가 자동으로 켠다.
    /// .unitypackage로 설치한 경우 Player Settings Scripting Define에 동일 이름을 넣는다.
    /// </summary>
    public static class SdkDefines
    {
        public const string GoogleMobileAds = "GOOGLE_MOBILE_ADS";
        public const string FirebaseAnalytics = "FIREBASE_ANALYTICS";
        public const string FirebaseCrashlytics = "FIREBASE_CRASHLYTICS";

        public static bool HasGoogleMobileAds
        {
            get
            {
#if GOOGLE_MOBILE_ADS
                return true;
#else
                return false;
#endif
            }
        }

        public static bool HasFirebaseAnalytics
        {
            get
            {
#if FIREBASE_ANALYTICS
                return true;
#else
                return false;
#endif
            }
        }

        public static bool HasFirebaseCrashlytics
        {
            get
            {
#if FIREBASE_CRASHLYTICS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
