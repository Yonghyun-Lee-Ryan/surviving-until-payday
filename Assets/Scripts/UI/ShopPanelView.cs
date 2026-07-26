using System;
using System.Collections.Generic;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Purchasing;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 상점: 특성 조각 적립(광고) · 조각으로 특성 조기 해금 · 전면 광고 제거(Mock 인앱).
    /// </summary>
    public sealed class ShopPanelView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        private static readonly Color CardColor = new Color(0.14f, 0.16f, 0.2f, 1f);
        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color Accent = new Color(0.28f, 0.48f, 0.62f, 1f);
        private static readonly Color AccentSoft = new Color(0.32f, 0.55f, 0.42f, 1f);
        private static readonly Color UnlockAccent = new Color(0.55f, 0.42f, 0.28f, 1f);

        [SerializeField] private GameObject root;
        [SerializeField] private List<TraitData> traitCatalog = new List<TraitData>();

        private Text fragmentLabel;
        private Text statusLabel;
        private Text unlockHeaderLabel;
        private Transform unlockList;
        private Button watchAdButton;
        private Button buyNoAdsButton;
        private Button closeButton;
        private bool layoutReady;
        private bool busy;
        private const int LayoutVersion = 2;
        private int builtLayoutVersion;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            EnsureLayout();
            Hide();
        }

        public void SetTraitCatalog(IReadOnlyList<TraitData> traits)
        {
            traitCatalog = traits != null
                ? new List<TraitData>(traits)
                : new List<TraitData>();
        }

        public void Show()
        {
            EnsureLayout();
            SyncQuotaFromSave();
            Refresh();
            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (root != null && root.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void SyncQuotaFromSave()
        {
            var app = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var meta = app.Session?.CachedSave?.meta;
            app.ApplyMonetizationFromMeta(meta);
        }

        private void Refresh()
        {
            var app = AppRoot.Instance;
            var meta = app?.Session?.Meta;
            var quota = app?.AdQuota;
            var remaining = quota != null
                ? quota.GetRemaining(RewardedAdPlacement.TraitFragment)
                : AdQuotaTracker.TraitFragmentLimitPerDay;
            var fragments = meta != null ? meta.TraitFragmentCount : 0;
            var hasNoAds = meta != null && meta.HasNoAds;
            var cost = MetaProgressionManager.TraitUnlockFragmentCost;

            if (fragmentLabel != null)
            {
                fragmentLabel.text = $"보유 특성 조각  {fragments}";
                UiFont.Apply(fragmentLabel, bold: true);
            }

            if (statusLabel != null)
            {
                statusLabel.text = hasNoAds
                    ? $"전면 광고 제거됨 · 조각 광고 {remaining}/{AdQuotaTracker.TraitFragmentLimitPerDay} · 특성 해금 {cost}조각"
                    : $"조각 광고 {remaining}/{AdQuotaTracker.TraitFragmentLimitPerDay} · 미해금 특성 {cost}조각으로 조기 해금";
                UiFont.Apply(statusLabel);
            }

            if (watchAdButton != null)
            {
                watchAdButton.interactable = !busy && remaining > 0;
                var label = watchAdButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = remaining > 0 ? "광고 보고 조각 받기" : "오늘 횟수 소진";
                    UiFont.Apply(label, bold: true);
                }
            }

            if (buyNoAdsButton != null)
            {
                buyNoAdsButton.interactable = !busy && !hasNoAds;
                var label = buyNoAdsButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = hasNoAds ? "전면 광고 제거 완료" : "전면 광고 제거 · 인앱(Mock)";
                    UiFont.Apply(label, bold: true);
                }
            }

            RebuildUnlockRows(meta, fragments, cost);
        }

        private void RebuildUnlockRows(MetaProgressionManager meta, int fragments, int cost)
        {
            if (unlockList == null)
            {
                return;
            }

            for (var i = unlockList.childCount - 1; i >= 0; i--)
            {
                Destroy(unlockList.GetChild(i).gameObject);
            }

            var locked = CollectLockedTraits(meta);
            if (unlockHeaderLabel != null)
            {
                unlockHeaderLabel.text = locked.Count == 0
                    ? "조기 해금 가능한 특성 없음"
                    : $"특성 조기 해금 ({cost}조각)";
                UiFont.Apply(unlockHeaderLabel, bold: true);
            }

            for (var i = 0; i < locked.Count; i++)
            {
                CreateUnlockRow(locked[i], fragments, cost);
            }
        }

        private List<TraitData> CollectLockedTraits(MetaProgressionManager meta)
        {
            var list = new List<TraitData>();
            if (traitCatalog == null)
            {
                return list;
            }

            for (var i = 0; i < traitCatalog.Count; i++)
            {
                var trait = traitCatalog[i];
                if (trait == null || string.IsNullOrWhiteSpace(trait.Id))
                {
                    continue;
                }

                if (meta == null || !meta.IsTraitUnlocked(trait))
                {
                    list.Add(trait);
                }
            }

            return list;
        }

        private void CreateUnlockRow(TraitData trait, int fragments, int cost)
        {
            var row = new GameObject(
                "Unlock_" + trait.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            row.transform.SetParent(unlockList, false);
            row.GetComponent<Image>().color = RowColor;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 72f;
            element.preferredHeight = 72f;

            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(14, 10, 8, 8);
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var info = new GameObject("Info", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            info.transform.SetParent(row.transform, false);
            var infoText = info.GetComponent<Text>();
            var name = string.IsNullOrWhiteSpace(trait.DisplayName) ? trait.Id : trait.DisplayName;
            infoText.text = $"{name}  (Lv{trait.UnlockLevel})";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoText.fontSize = 22;
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.color = Color.white;
            UiFont.Apply(infoText, bold: true);
            var infoElement = info.GetComponent<LayoutElement>();
            infoElement.flexibleWidth = 1f;
            infoElement.minWidth = 200f;

            var canAfford = !busy && fragments >= cost;
            var button = CreateActionButton(
                row.transform,
                "UnlockButton",
                canAfford ? $"{cost}조각 해금" : $"조각 부족({cost})",
                UnlockAccent);
            var btnElement = button.GetComponent<LayoutElement>();
            btnElement.flexibleWidth = 0f;
            btnElement.minWidth = 200f;
            btnElement.preferredWidth = 220f;
            btnElement.minHeight = 56f;
            btnElement.preferredHeight = 56f;
            button.interactable = canAfford;
            var captured = trait;
            button.onClick.AddListener(() => OnUnlockTraitClicked(captured));
        }

        private void OnUnlockTraitClicked(TraitData trait)
        {
            if (busy || trait == null)
            {
                return;
            }

            var app = AppRoot.Instance ?? AppRoot.EnsureCreated();
            var meta = app.Session?.Meta;
            if (meta == null)
            {
                SetStatus("세션을 찾을 수 없습니다.");
                return;
            }

            if (!meta.TryUnlockTraitWithFragments(trait, out var reason))
            {
                SetStatus(reason);
                Refresh();
                return;
            }

            app.Session.SyncTraitFragmentsFromMeta();
            app.PersistSession(
                includeActiveRun: app.Session != null && app.Session.HasActiveRun,
                runOverride: app.Session?.CachedSave?.run);
            var name = string.IsNullOrWhiteSpace(trait.DisplayName) ? trait.Id : trait.DisplayName;
            SetStatus($"{name} 해금! (조각 -{MetaProgressionManager.TraitUnlockFragmentCost})");
            app.Settings?.TryVibrate();
            app.Audio?.PlaySfx(Audio.SfxId.Success);
            Refresh();
        }

        private void OnWatchAdClicked()
        {
            if (busy)
            {
                return;
            }

            var app = AppRoot.Instance ?? AppRoot.EnsureCreated();
            if (app.RewardedAds == null)
            {
                SetStatus("광고 서비스를 사용할 수 없습니다.");
                return;
            }

            busy = true;
            Refresh();
            app.RewardedAds.Request(RewardedAdPlacement.TraitFragment, result =>
            {
                busy = false;
                if (result.RewardGranted && result.Reward.HasValue)
                {
                    var grant = result.Reward.Value;
                    if (grant.TraitFragments > 0)
                    {
                        app.Session?.Meta?.AddTraitFragments(grant.TraitFragments);
                        app.Session?.SyncTraitFragmentsFromMeta();
                    }

                    app.PersistSession(
                        includeActiveRun: app.Session != null && app.Session.HasActiveRun,
                        runOverride: app.Session?.CachedSave?.run);
                    SetStatus($"특성 조각 +{Math.Max(1, grant.TraitFragments)}");
                    app.Settings?.TryVibrate();
                    app.Audio?.PlaySfx(Audio.SfxId.Success);
                }
                else
                {
                    SetStatus(result.ShowResult.Message ?? "광고를 완료하지 못했습니다.");
                }

                Refresh();
            });
        }

        private void OnBuyNoAdsClicked()
        {
            if (busy)
            {
                return;
            }

            var app = AppRoot.Instance ?? AppRoot.EnsureCreated();
            busy = true;
            Refresh();
            app.PurchaseRemoveInterstitial(result =>
            {
                busy = false;
                if (result.IsSuccess)
                {
                    SetStatus("전면 광고가 제거되었습니다. 보상형 광고는 계속 이용할 수 있습니다.");
                    app.Settings?.TryVibrate();
                    app.Audio?.PlaySfx(Audio.SfxId.Success);
                }
                else
                {
                    SetStatus(result.Message ?? "구매에 실패했습니다.");
                }

                Refresh();
            });
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
                UiFont.Apply(statusLabel);
            }
        }

        private void EnsureLayout()
        {
            if (layoutReady && builtLayoutVersion == LayoutVersion && fragmentLabel != null && unlockList != null)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            var existing = root.transform.Find("ShopCard");
            if (existing != null)
            {
                existing.name = "ShopCard_PendingDestroy";
                Destroy(existing.gameObject);
            }

            var rootRect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            rootImage.color = OverlayColor;
            rootImage.raycastTarget = true;

            var card = new GameObject("ShopCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 1100f);
            card.GetComponent<Image>().color = CardColor;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(card.transform, "Title", "상점", 34, 40f, bold: true);
            fragmentLabel = CreateLabel(card.transform, "Fragments", "보유 특성 조각  0", 24, 32f, bold: true);
            statusLabel = CreateWrappedLabel(
                card.transform,
                "Status",
                "광고로 조각을 모으고, 미해금 특성을 조각으로 조기 해금할 수 있습니다.",
                72f);

            watchAdButton = CreateActionButton(card.transform, "WatchAdButton", "광고 보고 조각 받기", AccentSoft);
            buyNoAdsButton = CreateActionButton(card.transform, "BuyNoAdsButton", "전면 광고 제거 · 인앱(Mock)", Accent);

            unlockHeaderLabel = CreateLabel(card.transform, "UnlockHeader", "특성 조기 해금", 22, 30f, bold: true);
            var listGo = new GameObject(
                "UnlockList",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            listGo.transform.SetParent(card.transform, false);
            unlockList = listGo.transform;
            var listLayout = listGo.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            var listElement = listGo.GetComponent<LayoutElement>();
            listElement.minHeight = 80f;
            listElement.flexibleHeight = 1f;
            listElement.preferredHeight = 280f;

            closeButton = CreateActionButton(card.transform, "CloseButton", "닫기", RowColor);

            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            buyNoAdsButton.onClick.RemoveAllListeners();
            buyNoAdsButton.onClick.AddListener(OnBuyNoAdsClicked);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);

            layoutReady = true;
            builtLayoutVersion = LayoutVersion;
        }

        private static Text CreateLabel(Transform parent, string name, string text, int size, float height, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            UiFont.Apply(label, bold);
            return label;
        }

        private static Text CreateWrappedLabel(Transform parent, string name, string text, float height)
        {
            var label = CreateLabel(parent, name, text, 18, height, bold: false);
            label.alignment = TextAnchor.UpperCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var element = label.GetComponent<LayoutElement>();
            element.flexibleHeight = 0f;
            return label;
        }

        private static Button CreateActionButton(Transform parent, string name, string caption, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = 58f;
            element.preferredHeight = 58f;
            element.flexibleWidth = 1f;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }
    }
}
