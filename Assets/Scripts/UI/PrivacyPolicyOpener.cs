using SurviveUntilPayday.Settings;
using UnityEngine;

namespace SurviveUntilPayday.UI
{
    public static class PrivacyPolicyOpener
    {
        public static void Open(PrivacyPolicyConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.PolicyUrl))
            {
                Debug.LogWarning("[PrivacyPolicy] URL이 비어 있습니다. PrivacyPolicyConfig를 설정하세요.");
                return;
            }

            Application.OpenURL(config.PolicyUrl);
        }
    }
}
