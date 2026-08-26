using System;

namespace SurviveUntilPayday.Settings
{
    /// <summary>
    /// Play 콘솔에 제출하는 개인정보처리방침 주소. example.com 등 placeholder는 사용하지 않는다.
    /// GitHub Pages(Settings → Pages → /Docs)를 켜면 Canonical이 바로 열린다.
    /// </summary>
    public static class PrivacyPolicyUrls
    {
        public const string Canonical =
            "https://yonghyun-lee-ryan.github.io/surviving-until-payday/privacy.html";

        public const string RepositoryHtml =
            "https://github.com/Yonghyun-Lee-Ryan/surviving-until-payday/blob/develop/Docs/privacy.html";

        public static bool IsPlaceholder(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            var trimmed = url.Trim();
            return trimmed.IndexOf("example.com", StringComparison.OrdinalIgnoreCase) >= 0
                   || trimmed.IndexOf("example.org", StringComparison.OrdinalIgnoreCase) >= 0
                   || trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHttpsPublicUrl(string url)
        {
            if (IsPlaceholder(url))
            {
                return false;
            }

            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                   && uri.Scheme == Uri.UriSchemeHttps
                   && !string.IsNullOrEmpty(uri.Host);
        }
    }
}
