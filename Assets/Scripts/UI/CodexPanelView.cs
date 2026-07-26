using System;
using System.Collections.Generic;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    public enum CodexTab
    {
        Ending = 0,
        Event = 1,
        Achievement = 2
    }

    public enum CodexFilter
    {
        All = 0,
        Unlocked = 1,
        Locked = 2
    }

    /// <summary>
    /// 메인 메뉴 도감: 해금률 + 탭/필터 + 미해금 실루엣 목록.
    /// </summary>
    public sealed class CodexPanelView : MonoBehaviour
    {
        private static readonly Color PanelBg = new Color(0.96f, 0.95f, 0.92f, 0.98f);
        private static readonly Color TextDark = new Color(0.16f, 0.17f, 0.2f, 1f);
        private static readonly Color TextMuted = new Color(0.35f, 0.37f, 0.4f, 1f);
        private static readonly Color TabIdle = new Color(0.78f, 0.8f, 0.84f, 1f);
        private static readonly Color TabActive = new Color(0.32f, 0.52f, 0.72f, 1f);
        private static readonly Color RowUnlocked = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color RowLocked = new Color(0.62f, 0.64f, 0.68f, 0.95f);

        [SerializeField] private Text levelLabel;
        [SerializeField] private Text experienceLabel;
        [SerializeField] private Text endingRateLabel;
        [SerializeField] private Text eventRateLabel;
        [SerializeField] private Text traitRateLabel;
        [SerializeField] private Text achievementRateLabel;
        [SerializeField] private Text unlockToastLabel;
        [SerializeField] private Text fragmentLabel;
        [SerializeField] private Text listTitleLabel;
        [SerializeField] private Transform listContentRoot;
        [SerializeField] private Button endingTabButton;
        [SerializeField] private Button eventTabButton;
        [SerializeField] private Button achievementTabButton;
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterUnlockedButton;
        [SerializeField] private Button filterLockedButton;

        private CodexTab activeTab = CodexTab.Ending;
        private CodexFilter activeFilter = CodexFilter.All;
        private MetaProgressionManager cachedMeta;
        private int totalEndings;
        private int totalEvents;
        private int totalTraits;
        private int totalAchievements;
        private IReadOnlyList<EndingData> endingCatalog = Array.Empty<EndingData>();
        private IReadOnlyList<EventData> eventCatalog = Array.Empty<EventData>();
        private readonly List<GameObject> listRows = new List<GameObject>();
        private bool layoutReady;
        private bool buttonsWired;
        private GameObject detailOverlay;
        private Text detailTitleLabel;
        private Text detailBodyLabel;

        public void Bind(
            Text level,
            Text experience,
            Text endingRate,
            Text eventRate,
            Text traitRate,
            Text achievementRate,
            Text unlockToast)
        {
            levelLabel = level;
            experienceLabel = experience;
            endingRateLabel = endingRate;
            eventRateLabel = eventRate;
            traitRateLabel = traitRate;
            achievementRateLabel = achievementRate;
            unlockToastLabel = unlockToast;
        }

        public void BindExtended(
            Text fragment,
            Text listTitle,
            Transform listRoot,
            Button endingTab,
            Button eventTab,
            Button achievementTab,
            Button filterAll,
            Button filterUnlocked,
            Button filterLocked)
        {
            fragmentLabel = fragment;
            listTitleLabel = listTitle;
            listContentRoot = listRoot;
            endingTabButton = endingTab;
            eventTabButton = eventTab;
            achievementTabButton = achievementTab;
            filterAllButton = filterAll;
            filterUnlockedButton = filterUnlocked;
            filterLockedButton = filterLocked;
            WireButtons();
        }

        private void Awake()
        {
            EnsureCleanLayout();
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        public void Refresh(
            MetaProgressionManager meta,
            int endingsTotal,
            int eventsTotal,
            int traitsTotal,
            int achievementsTotal,
            IReadOnlyList<EndingData> endings = null,
            IReadOnlyList<EventData> events = null)
        {
            EnsureCleanLayout();
            cachedMeta = meta;
            totalEndings = endingsTotal;
            totalEvents = eventsTotal;
            totalTraits = traitsTotal;
            totalAchievements = achievementsTotal;
            endingCatalog = endings ?? Array.Empty<EndingData>();
            eventCatalog = events ?? Array.Empty<EventData>();

            if (meta == null)
            {
                return;
            }

            var into = PlayerLevel.GetXpIntoCurrentLevel(meta.TotalExperience, out var level, out var toNext);
            SetText(levelLabel, $"Lv.{level}");
            SetText(
                experienceLabel,
                toNext > 0
                    ? $"인생 경험치 {meta.TotalExperience}  ({into}/{toNext})"
                    : $"인생 경험치 {meta.TotalExperience}  (MAX)");
            SetText(fragmentLabel, $"특성 조각  {meta.TraitFragmentCount}");

            SetRate(endingRateLabel, "엔딩", meta.Endings.UnlockedCount, totalEndings);
            SetRate(eventRateLabel, "사건", meta.Events.UnlockedCount, totalEvents);
            SetRate(traitRateLabel, "특성", meta.Traits.UnlockedCount, totalTraits);
            SetRate(achievementRateLabel, "업적", meta.Achievements.UnlockedCount, totalAchievements);
            RefreshList();
            HighlightChrome();
        }

        public void ShowUnlockToast(string message)
        {
            SetText(unlockToastLabel, message ?? string.Empty);
        }

        private void EnsureCleanLayout()
        {
            if (layoutReady)
            {
                return;
            }

            layoutReady = true;
            HideLegacyChildren();
            FitPanelFrame();

            var root = GetOrCreate("CodexLayout");
            StretchFull(root);
            var rootLayout = GetOrAdd<VerticalLayoutGroup>(root.gameObject);
            rootLayout.padding = new RectOffset(28, 28, 20, 18);
            rootLayout.spacing = 14f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            levelLabel = EnsureLabel(root, "Level", levelLabel, 34, true, 40f);
            experienceLabel = EnsureLabel(root, "XP", experienceLabel, 22, false, 30f);
            fragmentLabel = EnsureLabel(root, "Fragments", fragmentLabel, 20, false, 28f);

            var rateBlock = new GameObject("RateBlock", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            rateBlock.transform.SetParent(root, false);
            var rateBlockLayout = rateBlock.GetComponent<VerticalLayoutGroup>();
            rateBlockLayout.spacing = 6f;
            rateBlockLayout.childControlWidth = true;
            rateBlockLayout.childControlHeight = true;
            rateBlockLayout.childForceExpandWidth = true;
            rateBlockLayout.childForceExpandHeight = false;
            var rateBlockElement = rateBlock.GetComponent<LayoutElement>();
            rateBlockElement.minHeight = 84f;
            rateBlockElement.preferredHeight = 88f;

            var ratesTop = CreateRow(rateBlock.transform, "RateRowTop", 34f, 12f);
            endingRateLabel = EnsureLabel(ratesTop, "EndingRate", endingRateLabel, 20, false, 32f);
            eventRateLabel = EnsureLabel(ratesTop, "EventRate", eventRateLabel, 20, false, 32f);
            var ratesBottom = CreateRow(rateBlock.transform, "RateRowBottom", 34f, 12f);
            traitRateLabel = EnsureLabel(ratesBottom, "TraitRate", traitRateLabel, 20, false, 32f);
            achievementRateLabel = EnsureLabel(ratesBottom, "AchievementRate", achievementRateLabel, 20, false, 32f);

            var tabs = CreateRow(root, "TabRow", 44f, 10f);
            endingTabButton = EnsureButton(tabs, "TabEnding", endingTabButton, "엔딩");
            eventTabButton = EnsureButton(tabs, "TabEvent", eventTabButton, "사건");
            achievementTabButton = EnsureButton(tabs, "TabAchievement", achievementTabButton, "업적");

            var filters = CreateRow(root, "FilterRow", 40f, 10f);
            filterAllButton = EnsureButton(filters, "FilterAll", filterAllButton, "전체");
            filterUnlockedButton = EnsureButton(filters, "FilterUnlocked", filterUnlockedButton, "해금");
            filterLockedButton = EnsureButton(filters, "FilterLocked", filterLockedButton, "미해금");

            listTitleLabel = EnsureLabel(root, "ListTitle", listTitleLabel, 20, true, 28f);
            listTitleLabel.alignment = TextAnchor.MiddleLeft;

            listContentRoot = CreateScrollList(root);
            unlockToastLabel = EnsureLabel(root, "UnlockToast", unlockToastLabel, 18, false, 28f);
            unlockToastLabel.color = new Color(0.25f, 0.4f, 0.55f, 1f);
            unlockToastLabel.raycastTarget = false;

            WireButtons();
            HighlightChrome();
        }

        private void HideLegacyChildren()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "CodexLayout")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void FitPanelFrame()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.04f, 0.02f);
            rect.anchorMax = new Vector2(0.96f, 0.48f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0f);

            var image = GetComponent<Image>();
            if (image != null)
            {
                image.color = PanelBg;
            }
        }

        private Transform GetOrCreate(string name)
        {
            var existing = transform.Find(name);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        private static void StretchFull(Transform target)
        {
            var rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Transform CreateRow(Transform parent, string name, float height, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            return go.transform;
        }

        private Transform CreateScrollList(Transform parent)
        {
            var scrollGo = new GameObject("ListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(parent, false);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = Vector2.zero;
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = new Color(0.9f, 0.9f, 0.88f, 0.55f);
            var scrollElement = scrollGo.GetComponent<LayoutElement>();
            scrollElement.minHeight = 240f;
            scrollElement.preferredHeight = 300f;
            scrollElement.flexibleHeight = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.transform);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            viewportImage.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return content.transform;
        }

        private Text EnsureLabel(
            Transform parent,
            string name,
            Text existing,
            int fontSize,
            bool bold,
            float height)
        {
            Text label = null;
            if (existing != null)
            {
                existing.transform.SetParent(parent, false);
                existing.gameObject.SetActive(true);
                existing.gameObject.name = name;
                label = existing;
            }
            else
            {
                var found = parent.Find(name);
                if (found != null)
                {
                    label = found.GetComponent<Text>();
                }
            }

            if (label == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                label = go.GetComponent<Text>();
                label.text = name;
            }

            var element = label.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = label.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = height;
            element.preferredHeight = height;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = TextDark;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UiFont.Apply(label, bold);
            return label;
        }

        private Button EnsureButton(Transform parent, string name, Button existing, string caption)
        {
            Button button = null;
            if (existing != null)
            {
                existing.transform.SetParent(parent, false);
                existing.gameObject.SetActive(true);
                existing.gameObject.name = name;
                button = existing;
            }
            else
            {
                var found = parent.Find(name);
                if (found != null)
                {
                    button = found.GetComponent<Button>();
                }
            }

            if (button == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                button = go.GetComponent<Button>();
                var image = go.GetComponent<Image>();
                image.color = TabIdle;

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                StretchFull(textGo.transform);
                var text = textGo.GetComponent<Text>();
                text.text = caption;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 20;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                UiFont.Apply(text, bold: true);
            }

            var element = button.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = button.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = 36f;
            element.preferredHeight = 40f;
            element.flexibleWidth = 1f;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = caption;
                UiFont.Apply(label, bold: true);
            }

            return button;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private void WireButtons()
        {
            if (buttonsWired)
            {
                return;
            }

            UnwireButtons();
            if (endingTabButton != null)
            {
                endingTabButton.onClick.AddListener(() => SetTab(CodexTab.Ending));
            }

            if (eventTabButton != null)
            {
                eventTabButton.onClick.AddListener(() => SetTab(CodexTab.Event));
            }

            if (achievementTabButton != null)
            {
                achievementTabButton.onClick.AddListener(() => SetTab(CodexTab.Achievement));
            }

            if (filterAllButton != null)
            {
                filterAllButton.onClick.AddListener(() => SetFilter(CodexFilter.All));
            }

            if (filterUnlockedButton != null)
            {
                filterUnlockedButton.onClick.AddListener(() => SetFilter(CodexFilter.Unlocked));
            }

            if (filterLockedButton != null)
            {
                filterLockedButton.onClick.AddListener(() => SetFilter(CodexFilter.Locked));
            }

            buttonsWired = true;
        }

        private void UnwireButtons()
        {
            endingTabButton?.onClick.RemoveAllListeners();
            eventTabButton?.onClick.RemoveAllListeners();
            achievementTabButton?.onClick.RemoveAllListeners();
            filterAllButton?.onClick.RemoveAllListeners();
            filterUnlockedButton?.onClick.RemoveAllListeners();
            filterLockedButton?.onClick.RemoveAllListeners();
            buttonsWired = false;
        }

        private void SetTab(CodexTab tab)
        {
            activeTab = tab;
            HideDetail();
            RefreshList();
            HighlightChrome();
        }

        private void SetFilter(CodexFilter filter)
        {
            activeFilter = filter;
            HideDetail();
            RefreshList();
            HighlightChrome();
        }

        private void HighlightChrome()
        {
            TintTab(endingTabButton, activeTab == CodexTab.Ending);
            TintTab(eventTabButton, activeTab == CodexTab.Event);
            TintTab(achievementTabButton, activeTab == CodexTab.Achievement);
            TintTab(filterAllButton, activeFilter == CodexFilter.All);
            TintTab(filterUnlockedButton, activeFilter == CodexFilter.Unlocked);
            TintTab(filterLockedButton, activeFilter == CodexFilter.Locked);
        }

        private static void TintTab(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? TabActive : TabIdle;
            }
        }

        private void RefreshList()
        {
            ClearRows();
            if (listContentRoot == null || cachedMeta == null)
            {
                return;
            }

            SetText(listTitleLabel, $"{TabTitle(activeTab)}  ·  {FilterTitle(activeFilter)}");
            var entries = BuildEntries();
            if (entries.Count == 0)
            {
                listRows.Add(CreateEmptyRow("아직 표시할 항목이 없습니다.\n플레이로 도감을 채워 보세요."));
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                listRows.Add(CreateRow(entries[i]));
            }
        }

        private List<CodexListEntry> BuildEntries()
        {
            var result = new List<CodexListEntry>();
            switch (activeTab)
            {
                case CodexTab.Event:
                    BuildEventEntries(result);
                    break;
                case CodexTab.Achievement:
                    BuildAchievementEntries(result);
                    break;
                default:
                    BuildEndingEntries(result);
                    break;
            }

            return result;
        }

        private void BuildEndingEntries(List<CodexListEntry> result)
        {
            var unlocked = new HashSet<string>(cachedMeta.Endings.UnlockedIds);
            if (endingCatalog != null && endingCatalog.Count > 0)
            {
                for (var i = 0; i < endingCatalog.Count; i++)
                {
                    var ending = endingCatalog[i];
                    if (ending == null || string.IsNullOrWhiteSpace(ending.Id))
                    {
                        continue;
                    }

                    var isUnlocked = unlocked.Contains(ending.Id);
                    if (!PassesFilter(isUnlocked))
                    {
                        continue;
                    }

                    result.Add(isUnlocked
                        ? new CodexListEntry(ending.Title, ending.Description, true)
                        : new CodexListEntry("???", "아직 해금되지 않은 엔딩", false));
                }

                return;
            }

            foreach (var id in unlocked)
            {
                if (!PassesFilter(true))
                {
                    continue;
                }

                result.Add(new CodexListEntry(id, string.Empty, true));
            }

            var lockedCount = Mathf.Max(0, totalEndings - unlocked.Count);
            for (var i = 0; i < lockedCount && PassesFilter(false); i++)
            {
                result.Add(new CodexListEntry("???", "아직 해금되지 않은 엔딩", false));
            }
        }

        private void BuildEventEntries(List<CodexListEntry> result)
        {
            var unlocked = new HashSet<string>(cachedMeta.Events.UnlockedIds);
            if (eventCatalog != null && eventCatalog.Count > 0)
            {
                for (var i = 0; i < eventCatalog.Count; i++)
                {
                    var eventData = eventCatalog[i];
                    if (eventData == null || string.IsNullOrWhiteSpace(eventData.Id))
                    {
                        continue;
                    }

                    if (eventData.Id == "event_rest_fallback")
                    {
                        continue;
                    }

                    var isUnlocked = unlocked.Contains(eventData.Id);
                    if (!PassesFilter(isUnlocked))
                    {
                        continue;
                    }

                    result.Add(isUnlocked
                        ? new CodexListEntry(
                            eventData.Title,
                            string.IsNullOrWhiteSpace(eventData.Description)
                                ? "해금된 사건"
                                : eventData.Description,
                            true)
                        : new CodexListEntry("???", "아직 해금되지 않은 사건", false));
                }

                return;
            }

            var unlockedIds = new List<string>(unlocked);
            unlockedIds.Sort(StringComparer.Ordinal);
            for (var i = 0; i < unlockedIds.Count; i++)
            {
                if (!PassesFilter(true))
                {
                    continue;
                }

                result.Add(new CodexListEntry(unlockedIds[i], "해금된 사건", true));
            }

            var lockedCount = Mathf.Max(0, totalEvents - unlocked.Count);
            for (var i = 0; i < lockedCount; i++)
            {
                if (!PassesFilter(false))
                {
                    break;
                }

                result.Add(new CodexListEntry("???", "아직 해금되지 않은 사건", false));
            }
        }

        private void BuildAchievementEntries(List<CodexListEntry> result)
        {
            var catalog = AchievementIds.Catalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                var isUnlocked = cachedMeta.Achievements.IsUnlocked(def.Id);
                if (!PassesFilter(isUnlocked))
                {
                    continue;
                }

                result.Add(isUnlocked
                    ? new CodexListEntry(def.Title, def.Description, true)
                    : new CodexListEntry("???", "조건 달성 시 해금", false));
            }
        }

        private bool PassesFilter(bool isUnlocked)
        {
            switch (activeFilter)
            {
                case CodexFilter.Unlocked:
                    return isUnlocked;
                case CodexFilter.Locked:
                    return !isUnlocked;
                default:
                    return true;
            }
        }

        private GameObject CreateEmptyRow(string message)
        {
            var go = new GameObject("EmptyRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(listContentRoot, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 72f;
            layout.preferredHeight = 72f;
            go.GetComponent<Image>().color = RowLocked;

            var labelGo = new GameObject("Message", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 8f);
            rect.offsetMax = new Vector2(-14f, -8f);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = message ?? string.Empty;
            UiFont.Apply(label);
            return go;
        }

        private GameObject CreateRow(CodexListEntry entry)
        {
            var go = new GameObject(
                entry.Unlocked ? "UnlockedRow" : "LockedRow",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(listContentRoot, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 72f;
            layout.preferredHeight = 72f;
            var image = go.GetComponent<Image>();
            image.color = entry.Unlocked ? RowUnlocked : RowLocked;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = entry.Unlocked
                ? new Color(0.92f, 0.95f, 1f, 1f)
                : new Color(0.7f, 0.72f, 0.76f, 1f);
            colors.pressedColor = new Color(0.85f, 0.88f, 0.92f, 1f);
            button.colors = colors;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(go.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.48f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(14f, 0f);
            titleRect.offsetMax = new Vector2(-14f, -4f);
            var title = titleGo.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 22;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = entry.Unlocked ? TextDark : Color.white;
            title.text = entry.Title;
            title.raycastTarget = false;
            UiFont.Apply(title, bold: true);

            var descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(go.transform, false);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.52f);
            descRect.offsetMin = new Vector2(14f, 4f);
            descRect.offsetMax = new Vector2(-14f, 0f);
            var desc = descGo.AddComponent<Text>();
            desc.font = title.font;
            desc.fontSize = 16;
            desc.alignment = TextAnchor.UpperLeft;
            desc.color = entry.Unlocked ? TextMuted : new Color(0.92f, 0.92f, 0.94f, 1f);
            desc.text = Truncate(entry.Description, 48);
            desc.raycastTarget = false;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Truncate;
            UiFont.Apply(desc);

            var captured = entry;
            button.onClick.AddListener(() => OnCodexRowClicked(captured));
            return go;
        }

        private void OnCodexRowClicked(CodexListEntry entry)
        {
            if (!entry.Unlocked)
            {
                ShowUnlockToast("아직 해금되지 않은 항목입니다.");
                return;
            }

            ShowDetail(entry.Title, entry.Description);
        }

        private void ShowDetail(string title, string body)
        {
            EnsureDetailOverlay();
            if (detailOverlay == null)
            {
                return;
            }

            if (detailTitleLabel != null)
            {
                detailTitleLabel.text = title ?? string.Empty;
            }

            if (detailBodyLabel != null)
            {
                detailBodyLabel.text = string.IsNullOrWhiteSpace(body)
                    ? "설명이 없습니다."
                    : body;
            }

            detailOverlay.SetActive(true);
            detailOverlay.transform.SetAsLastSibling();
        }

        private void HideDetail()
        {
            if (detailOverlay != null)
            {
                detailOverlay.SetActive(false);
            }
        }

        private void EnsureDetailOverlay()
        {
            if (detailOverlay != null)
            {
                return;
            }

            detailOverlay = new GameObject(
                "CodexDetailOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            detailOverlay.transform.SetParent(transform, false);
            var overlayRt = detailOverlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImage = detailOverlay.GetComponent<Image>();
            overlayImage.color = new Color(0.08f, 0.09f, 0.11f, 0.55f);
            var overlayButton = detailOverlay.GetComponent<Button>();
            overlayButton.targetGraphic = overlayImage;
            overlayButton.onClick.AddListener(HideDetail);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(detailOverlay.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.08f, 0.22f);
            cardRt.anchorMax = new Vector2(0.92f, 0.78f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.98f, 0.97f, 0.94f, 1f);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(card.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.72f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20f, 8f);
            titleRt.offsetMax = new Vector2(-20f, -12f);
            detailTitleLabel = titleGo.AddComponent<Text>();
            detailTitleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            detailTitleLabel.fontSize = 28;
            detailTitleLabel.alignment = TextAnchor.MiddleCenter;
            detailTitleLabel.color = TextDark;
            detailTitleLabel.raycastTarget = false;
            UiFont.Apply(detailTitleLabel, bold: true);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(card.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0.18f);
            bodyRt.anchorMax = new Vector2(1f, 0.72f);
            bodyRt.offsetMin = new Vector2(24f, 8f);
            bodyRt.offsetMax = new Vector2(-24f, -8f);
            detailBodyLabel = bodyGo.AddComponent<Text>();
            detailBodyLabel.font = detailTitleLabel.font;
            detailBodyLabel.fontSize = 22;
            detailBodyLabel.alignment = TextAnchor.UpperCenter;
            detailBodyLabel.color = TextMuted;
            detailBodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailBodyLabel.verticalOverflow = VerticalWrapMode.Overflow;
            detailBodyLabel.raycastTarget = false;
            UiFont.Apply(detailBodyLabel);

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(card.transform, false);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.2f, 0.04f);
            closeRt.anchorMax = new Vector2(0.8f, 0.16f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            closeGo.GetComponent<Image>().color = new Color(0.32f, 0.52f, 0.72f, 1f);
            var closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeGo.GetComponent<Image>();
            closeButton.onClick.AddListener(HideDetail);
            var closeLabelGo = new GameObject("Label", typeof(RectTransform));
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            var closeLabelRt = closeLabelGo.GetComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            var closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.font = detailTitleLabel.font;
            closeLabel.fontSize = 22;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;
            closeLabel.text = "닫기";
            closeLabel.raycastTarget = false;
            UiFont.Apply(closeLabel);

            detailOverlay.SetActive(false);
        }

        private void ClearRows()
        {
            for (var i = 0; i < listRows.Count; i++)
            {
                if (listRows[i] != null)
                {
                    Destroy(listRows[i]);
                }
            }

            listRows.Clear();
            if (listContentRoot == null)
            {
                return;
            }

            for (var i = listContentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(listContentRoot.GetChild(i).gameObject);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static void SetRate(Text label, string title, int unlocked, int total)
        {
            if (label == null)
            {
                return;
            }

            var safeTotal = Mathf.Max(total, unlocked);
            var percent = safeTotal <= 0 ? 0 : Mathf.RoundToInt(100f * unlocked / safeTotal);
            label.text = $"{title}  {unlocked}/{safeTotal}  ({percent}%)";
            label.alignment = TextAnchor.MiddleCenter;
            UiFont.Apply(label);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, max - 1) + "…";
        }

        private static string TabTitle(CodexTab tab)
        {
            switch (tab)
            {
                case CodexTab.Event:
                    return "사건";
                case CodexTab.Achievement:
                    return "업적";
                default:
                    return "엔딩";
            }
        }

        private static string FilterTitle(CodexFilter filter)
        {
            switch (filter)
            {
                case CodexFilter.Unlocked:
                    return "해금만";
                case CodexFilter.Locked:
                    return "미해금만";
                default:
                    return "전체";
            }
        }

        private readonly struct CodexListEntry
        {
            public string Title { get; }
            public string Description { get; }
            public bool Unlocked { get; }

            public CodexListEntry(string title, string description, bool unlocked)
            {
                Title = title;
                Description = description;
                Unlocked = unlocked;
            }
        }
    }
}
