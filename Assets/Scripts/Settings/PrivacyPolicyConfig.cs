using UnityEngine;

namespace SurviveUntilPayday.Settings
{
    /// <summary>
    /// 개인정보처리방침 URL 및 표시 문구.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PrivacyPolicyConfig",
        menuName = "Survive Until Payday/Config/Privacy Policy",
        order = 110)]
    public sealed class PrivacyPolicyConfig : ScriptableObject
    {
        [SerializeField] private string policyUrl = "https://example.com/privacy";
        [SerializeField] [TextArea(4, 12)] private string summaryText =
            "본 게임은 광고·분석·크래시 수집을 위해 기기의 비식별 정보를 사용할 수 있습니다. " +
            "자세한 내용은 개인정보처리방침을 확인해 주세요.";

        public string PolicyUrl => string.IsNullOrWhiteSpace(policyUrl) ? string.Empty : policyUrl.Trim();
        public string SummaryText => summaryText ?? string.Empty;

#if UNITY_EDITOR
        public void EditorSet(string url, string summary)
        {
            policyUrl = url;
            summaryText = summary;
        }
#endif
    }
}
