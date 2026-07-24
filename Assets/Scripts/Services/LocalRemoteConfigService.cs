using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Services
{
    /// <summary>
    /// Editor/오프라인용 로컬 Remote Config. 기본값 + 런타임 오버라이드를 지원한다.
    /// </summary>
    public sealed class LocalRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public bool IsFetched { get; private set; }

        public LocalRemoteConfigService()
        {
            SetDefault(RemoteConfigKeys.InterstitialEveryNRuns, "3");
            SetDefault(RemoteConfigKeys.RewardedCooldownSeconds, "2");
            SetDefault(RemoteConfigKeys.UseRealAds, "false");
        }

        public LocalRemoteConfigService(SdkIntegrationConfig config)
            : this()
        {
            if (config == null)
            {
                return;
            }

            SetOverride(RemoteConfigKeys.InterstitialEveryNRuns, config.InterstitialEveryNRuns.ToString());
            SetOverride(RemoteConfigKeys.RewardedCooldownSeconds, config.RewardedCooldownSeconds.ToString("R"));
            SetOverride(RemoteConfigKeys.UseRealAds, config.PreferRealAds.ToString());
        }

        public void SetOverride(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            values[key] = value ?? string.Empty;
        }

        public void FetchAndActivate(Action<bool> onCompleted)
        {
            IsFetched = true;
            Debug.Log("[LocalRemoteConfig] FetchAndActivate completed (local defaults).");
            onCompleted?.Invoke(true);
        }

        public int GetInt(string key, int defaultValue)
        {
            if (!TryGet(key, out var raw) || !int.TryParse(raw, out var parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public float GetFloat(string key, float defaultValue)
        {
            if (!TryGet(key, out var raw) || !float.TryParse(raw, out var parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (!TryGet(key, out var raw))
            {
                return defaultValue;
            }

            if (bool.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            if (raw == "1")
            {
                return true;
            }

            if (raw == "0")
            {
                return false;
            }

            return defaultValue;
        }

        public string GetString(string key, string defaultValue)
        {
            return TryGet(key, out var raw) ? raw : defaultValue;
        }

        private void SetDefault(string key, string value)
        {
            if (!values.ContainsKey(key))
            {
                values[key] = value;
            }
        }

        private bool TryGet(string key, out string value)
        {
            return values.TryGetValue(key, out value);
        }
    }
}
