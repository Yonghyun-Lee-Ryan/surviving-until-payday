using SurviveUntilPayday.Core;
using SurviveUntilPayday.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 설정: 사운드·배경음/효과음, 진동, 선택 미리보기, 개인정보, 크레딧, 초기화, (게임 중) 메인 메뉴.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        private static readonly Color CardColor = new Color(0.14f, 0.16f, 0.2f, 1f);
        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color Accent = new Color(0.28f, 0.48f, 0.62f, 1f);
        private static readonly Color CheckOn = new Color(0.35f, 0.72f, 0.48f, 1f);
        private static readonly Color Danger = new Color(0.55f, 0.28f, 0.28f, 1f);

        [SerializeField] private GameObject root;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button resetSaveButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text versionLabel;
        [SerializeField] private PrivacyPolicyConfig privacyConfig;

        private bool layoutReady;
        private bool listenersWired;
        private Text bgmValueLabel;
        private Text sfxValueLabel;
        private Toggle previewToggle;
        private GameObject creditsOverlay;
        private const int LayoutVersion = 11;
        private const float CheckBoxSize = 25f;
        private int builtLayoutVersion;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            EnsureCleanLayout();
            WireListeners();
            Hide();
        }

        private void OnDestroy()
        {
            UnwireListeners();
        }

        public void Bind(
            GameObject panelRoot,
            Toggle sound,
            Toggle vibration,
            Slider bgm,
            Slider sfx,
            Button privacy,
            Button resetSave,
            Button mainMenu,
            Button close,
            Text version,
            PrivacyPolicyConfig config)
        {
            root = panelRoot;
            soundToggle = sound;
            vibrationToggle = vibration;
            bgmSlider = bgm;
            sfxSlider = sfx;
            privacyButton = privacy;
            resetSaveButton = resetSave;
            mainMenuButton = mainMenu;
            closeButton = close;
            versionLabel = version;
            privacyConfig = config;
            layoutReady = false;
        }

        public void SetPrivacyConfig(PrivacyPolicyConfig config)
        {
            privacyConfig = config;
        }

        public void Show()
        {
            EnsureCleanLayout();
            RefreshFromSettings();
            if (root != null)
            {
                root.SetActive(true);
                UiModalLayer.BringToFront(root.transform);
            }
        }

        public void Hide()
        {
            HideCredits();
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

        private void EnsureCleanLayout()
        {
            if (layoutReady && builtLayoutVersion == LayoutVersion)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            HideLegacyChildren(root.transform);
            DestroyChildNamed(root.transform, "SettingsCard");
            DestroyChildNamed(root.transform, "CreditsOverlay");
            creditsOverlay = null;

            layoutReady = true;
            builtLayoutVersion = LayoutVersion;
            listenersWired = false;

            var rootRect = root.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
            }

            var rootImage = root.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = root.AddComponent<Image>();
            }

            rootImage.color = OverlayColor;
            rootImage.raycastTarget = true;

            var card = CreateFreshChild(root.transform, "SettingsCard", typeof(Image), typeof(VerticalLayoutGroup));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(820f, 1080f);
            card.GetComponent<Image>().color = CardColor;
            card.GetComponent<Image>().raycastTarget = true;
            card.transform.SetAsLastSibling();

            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(28, 28, 20, 16);
            cardLayout.spacing = 8f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateTitle(card.transform, "설정");

            var body = CreateChild(card.transform, "BodyStack", typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var bodyElement = body.GetComponent<LayoutElement>();
            bodyElement.flexibleHeight = 1f;
            bodyElement.minHeight = 560f;
            var bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 8f;
            bodyLayout.padding = new RectOffset(0, 0, 0, 0);
            bodyLayout.childAlignment = TextAnchor.UpperCenter;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            soundToggle = CreateCheckRow(body.transform, "SoundToggle", "사운드", flex: 1.5f);
            bgmSlider = CreateVolumeRow(body.transform, "BgmVolume", AccessibilityCopy.BgmLabel, flex: 2f, out bgmValueLabel);
            sfxSlider = CreateVolumeRow(body.transform, "SfxVolume", AccessibilityCopy.SfxLabel, flex: 2f, out sfxValueLabel);
            vibrationToggle = CreateCheckRow(body.transform, "VibrationToggle", "진동", flex: 1.5f);
            previewToggle = CreateCheckRow(body.transform, "PreviewToggle", AccessibilityCopy.ChoicePreviewToggle, flex: 1.5f);
            privacyButton = CreateActionButton(body.transform, "PrivacyButton", "개인정보처리방침", flex: 2f, Accent);
            creditsButton = CreateActionButton(body.transform, "CreditsButton", AccessibilityCopy.CreditsButton, flex: 2f, Accent);
            resetSaveButton = CreateActionButton(body.transform, "ResetSaveButton", "저장 데이터 초기화", flex: 2f, Accent);
            mainMenuButton = CreateActionButton(body.transform, "MainMenuButton", "메인 메뉴로", flex: 2f, Danger);
            closeButton = CreateActionButton(body.transform, "CloseButton", "닫기", flex: 2f, Accent);

            var offlineNote = CreateLabel(card.transform, "OfflineNote", null, AccessibilityCopy.MinBodyFontSize, 72f);
            offlineNote.text = AccessibilityCopy.OfflineNote;
            offlineNote.alignment = TextAnchor.UpperCenter;
            offlineNote.color = new Color(1f, 1f, 1f, 0.72f);
            offlineNote.horizontalOverflow = HorizontalWrapMode.Wrap;
            offlineNote.verticalOverflow = VerticalWrapMode.Overflow;
            UiFont.Apply(offlineNote);

            versionLabel = CreateLabel(card.transform, "Version", null, 20, 26f);
            versionLabel.color = new Color(1f, 1f, 1f, 0.55f);

            WireListeners();
        }

        private Slider CreateVolumeRow(Transform parent, string name, string caption, float flex, out Text valueLabel)
        {
            var row = CreateChild(parent, name + "Row", typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            row.GetComponent<Image>().color = RowColor;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 80f;
            element.preferredHeight = 0f;
            element.flexibleHeight = flex;

            var layout = row.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var top = CreateChild(row.transform, "Top", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var topElement = top.GetComponent<LayoutElement>();
            topElement.minHeight = 32f;
            topElement.preferredHeight = 32f;
            topElement.flexibleHeight = 1f;
            var topLayout = top.GetComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 12f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = false;
            topLayout.childControlHeight = false;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;

            var captionLabel = CreateChild(top.transform, "Caption", typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
            captionLabel.text = caption;
            captionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            captionLabel.fontSize = 22;
            captionLabel.alignment = TextAnchor.MiddleLeft;
            captionLabel.color = Color.white;
            UiFont.Apply(captionLabel, bold: true);
            var captionElement = captionLabel.GetComponent<LayoutElement>();
            captionElement.minWidth = 72f;
            captionElement.preferredHeight = 32f;

            valueLabel = CreateChild(top.transform, "Value", typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
            valueLabel.text = "100%";
            valueLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                              ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueLabel.fontSize = 22;
            valueLabel.alignment = TextAnchor.MiddleRight;
            valueLabel.color = new Color(0.85f, 0.9f, 1f, 1f);
            var valueElement = valueLabel.GetComponent<LayoutElement>();
            valueElement.flexibleWidth = 1f;
            valueElement.minWidth = 72f;
            valueElement.preferredHeight = 32f;
            UiFont.Apply(valueLabel);

            var sliderSlot = CreateChild(row.transform, "SliderSlot", typeof(LayoutElement));
            var slotElement = sliderSlot.GetComponent<LayoutElement>();
            slotElement.minHeight = 36f;
            slotElement.preferredHeight = 36f;
            slotElement.flexibleHeight = 1f;

            var slider = BuildSlider(sliderSlot.transform, name + "Slider", 32f);
            var sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 32f);
            var sliderLayout = slider.GetComponent<LayoutElement>();
            if (sliderLayout != null)
            {
                sliderLayout.ignoreLayout = true;
            }

            return slider;
        }

        private static void HideLegacyChildren(Transform parent)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == "SettingsCard")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private static void DestroyChildNamed(Transform parent, string name)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name != name)
                {
                    continue;
                }

                child.name = name + "_PendingDestroy";
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
        }

        private static GameObject CreateFreshChild(Transform parent, string name, params System.Type[] components)
        {
            DestroyChildNamed(parent, name);
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            for (var i = 0; i < components.Length; i++)
            {
                if (go.GetComponent(components[i]) == null)
                {
                    go.AddComponent(components[i]);
                }
            }

            return go;
        }

        private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.gameObject != null)
            {
                existing.gameObject.SetActive(true);
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            for (var i = 0; i < components.Length; i++)
            {
                if (go.GetComponent(components[i]) == null)
                {
                    go.AddComponent(components[i]);
                }
            }

            return go;
        }

        private static Text CreateTitle(Transform parent, string text)
        {
            var label = CreateLabel(parent, "Title", null, 34, 44f);
            label.text = text;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);
            return label;
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            Text existing,
            int fontSize,
            float height)
        {
            Text label = existing;
            if (label != null)
            {
                label.transform.SetParent(parent, false);
                label.gameObject.SetActive(true);
                label.gameObject.name = name;
            }
            else
            {
                var go = CreateChild(parent, name, typeof(Text), typeof(LayoutElement));
                label = go.GetComponent<Text>();
            }

            var element = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label);
            return label;
        }

        private Toggle CreateCheckRow(Transform parent, string name, string caption, float flex)
        {
            var row = CreateChild(parent, name, typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.GetComponent<Image>().color = RowColor;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = AccessibilityCopy.MinTapHeight;
            element.preferredHeight = 0f;
            element.flexibleHeight = flex;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return BuildToggleOn(row.transform, name + "_Toggle", caption, CheckBoxSize);
        }

        private Toggle BuildToggleOn(Transform parent, string name, string caption, float boxSize)
        {
            var host = CreateChild(parent, name, typeof(Toggle), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var hostLayout = host.GetComponent<HorizontalLayoutGroup>();
            hostLayout.spacing = 10f;
            hostLayout.childAlignment = TextAnchor.MiddleLeft;
            hostLayout.childControlWidth = false;
            hostLayout.childControlHeight = false;
            hostLayout.childForceExpandWidth = false;
            hostLayout.childForceExpandHeight = false;
            var hostElement = host.GetComponent<LayoutElement>();
            hostElement.flexibleWidth = 0f;
            hostElement.flexibleHeight = 0f;
            hostElement.minHeight = boxSize;
            hostElement.preferredHeight = boxSize;

            var box = CreateChild(host.transform, "CheckBox", typeof(Image), typeof(LayoutElement));
            var boxImage = box.GetComponent<Image>();
            boxImage.color = new Color(0.12f, 0.13f, 0.16f, 1f);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(boxSize, boxSize);
            var boxElement = box.GetComponent<LayoutElement>();
            boxElement.minWidth = boxElement.preferredWidth = boxSize;
            boxElement.minHeight = boxElement.preferredHeight = boxSize;
            boxElement.flexibleWidth = 0f;
            boxElement.flexibleHeight = 0f;

            var check = CreateChild(box.transform, "Checkmark", typeof(Image));
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.18f, 0.18f);
            checkRect.anchorMax = new Vector2(0.82f, 0.82f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImage = check.GetComponent<Image>();
            checkImage.color = CheckOn;

            var labelGo = CreateChild(host.transform, "Label", typeof(Text), typeof(LayoutElement));
            var label = labelGo.GetComponent<Text>();
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);
            var labelElement = labelGo.GetComponent<LayoutElement>();
            labelElement.minWidth = 72f;
            labelElement.preferredHeight = boxSize;
            labelElement.flexibleWidth = 0f;
            labelElement.flexibleHeight = 0f;

            var toggle = host.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private Slider BuildSlider(Transform parent, string name, float height)
        {
            var sliderGo = CreateChild(parent, name, typeof(Slider), typeof(LayoutElement));
            var layoutElement = sliderGo.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 0f;
            var slider = sliderGo.GetComponent<Slider>();

            var background = CreateChild(sliderGo.transform, "Background", typeof(Image));
            Stretch(background.GetComponent<RectTransform>(), 0f, 0.35f, 1f, 0.65f);
            background.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 1f);

            var fillArea = CreateChild(sliderGo.transform, "Fill Area", typeof(RectTransform));
            Stretch(fillArea.GetComponent<RectTransform>(), 0.02f, 0.35f, 0.98f, 0.65f);
            var fill = CreateChild(fillArea.transform, "Fill", typeof(Image));
            Stretch(fill.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            fill.GetComponent<Image>().color = Accent;

            var handleArea = CreateChild(sliderGo.transform, "Handle Slide Area", typeof(RectTransform));
            Stretch(handleArea.GetComponent<RectTransform>(), 0.02f, 0f, 0.98f, 1f);
            var handle = CreateChild(handleArea.transform, "Handle", typeof(Image));
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(26f, 26f);
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
            return slider;
        }

        private static Button CreateActionButton(Transform parent, string name, string caption, float flex, Color color)
        {
            var go = CreateChild(parent, name, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = color;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = AccessibilityCopy.MinTapHeight;
            element.preferredHeight = 0f;
            element.flexibleHeight = flex;
            element.flexibleWidth = 1f;

            var label = CreateChild(go.transform, "Label", typeof(Text)).GetComponent<Text>();
            Stretch(label.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static void Stretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void WireListeners()
        {
            if (listenersWired)
            {
                return;
            }

            UnwireListeners();
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.AddListener(OnSoundChanged);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            }

            if (previewToggle != null)
            {
                previewToggle.onValueChanged.AddListener(OnPreviewChanged);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (privacyButton != null)
            {
                privacyButton.onClick.AddListener(OnPrivacyClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.AddListener(OnResetSaveClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            listenersWired = true;
        }

        private void UnwireListeners()
        {
            soundToggle?.onValueChanged.RemoveAllListeners();
            vibrationToggle?.onValueChanged.RemoveAllListeners();
            previewToggle?.onValueChanged.RemoveAllListeners();
            bgmSlider?.onValueChanged.RemoveAllListeners();
            sfxSlider?.onValueChanged.RemoveAllListeners();
            privacyButton?.onClick.RemoveAllListeners();
            creditsButton?.onClick.RemoveAllListeners();
            resetSaveButton?.onClick.RemoveAllListeners();
            mainMenuButton?.onClick.RemoveAllListeners();
            closeButton?.onClick.RemoveAllListeners();
            listenersWired = false;
        }

        private void RefreshFromSettings()
        {
            var settings = AppRoot.Instance != null ? AppRoot.Instance.Settings : null;
            if (settings == null)
            {
                return;
            }

            if (soundToggle != null)
            {
                soundToggle.SetIsOnWithoutNotify(settings.SoundEnabled);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(settings.VibrationEnabled);
            }

            if (previewToggle != null)
            {
                previewToggle.SetIsOnWithoutNotify(settings.ShowChoicePreview);
            }

            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
                UpdateVolumeLabel(bgmValueLabel, settings.BgmVolume);
                bgmSlider.interactable = settings.SoundEnabled;
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
                UpdateVolumeLabel(sfxValueLabel, settings.SfxVolume);
                sfxSlider.interactable = settings.SoundEnabled;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(IsInGameScene());
            }

            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version} ({Application.platform})";
            }
        }

        private static bool IsInGameScene()
        {
            return SceneManager.GetActiveScene().name == SceneNames.Game;
        }

        private static void UpdateVolumeLabel(Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }

        private void OnSoundChanged(bool enabled)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.SoundEnabled = enabled;
            if (bgmSlider != null)
            {
                bgmSlider.interactable = enabled;
            }

            if (sfxSlider != null)
            {
                sfxSlider.interactable = enabled;
            }
        }

        private void OnVibrationChanged(bool enabled)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.VibrationEnabled = enabled;
            if (enabled)
            {
                settings.TryVibrate();
            }
        }

        private void OnPreviewChanged(bool enabled)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.ShowChoicePreview = enabled;
        }

        private void OnBgmVolumeChanged(float value)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.BgmVolume = value;
            UpdateVolumeLabel(bgmValueLabel, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            var settings = AppRoot.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            settings.SfxVolume = value;
            UpdateVolumeLabel(sfxValueLabel, value);
        }

        private void OnPrivacyClicked()
        {
            PrivacyPolicyOpener.Open(privacyConfig);
        }

        private void OnCreditsClicked()
        {
            ShowCredits();
        }

        private void ShowCredits()
        {
            EnsureCreditsOverlay();
            if (creditsOverlay != null)
            {
                creditsOverlay.SetActive(true);
                creditsOverlay.transform.SetAsLastSibling();
            }
        }

        private void HideCredits()
        {
            if (creditsOverlay != null)
            {
                creditsOverlay.SetActive(false);
            }
        }

        private void EnsureCreditsOverlay()
        {
            if (creditsOverlay != null)
            {
                return;
            }

            if (root == null)
            {
                return;
            }

            creditsOverlay = new GameObject("CreditsOverlay", typeof(RectTransform), typeof(Image));
            creditsOverlay.transform.SetParent(root.transform, false);
            var overlayRect = creditsOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            creditsOverlay.GetComponent<Image>().color = OverlayColor;
            creditsOverlay.GetComponent<Image>().raycastTarget = true;

            var card = CreateFreshChild(creditsOverlay.transform, "CreditsCard", typeof(Image), typeof(VerticalLayoutGroup));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(720f, 780f);
            card.GetComponent<Image>().color = CardColor;
            card.GetComponent<Image>().raycastTarget = true;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateLabel(card.transform, "CreditsTitle", null, 28, 40f);
            title.text = CreditsCopy.Title;
            title.color = Color.white;
            UiFont.Apply(title, bold: true);

            var body = CreateLabel(card.transform, "CreditsBody", null, AccessibilityCopy.MinBodyFontSize, 560f);
            body.text = CreditsCopy.Body;
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.color = new Color(0.92f, 0.93f, 0.95f, 1f);
            UiFont.Apply(body);

            var close = CreateActionButton(card.transform, "CreditsClose", "닫기", flex: 0f, Accent);
            close.onClick.AddListener(HideCredits);
            creditsOverlay.SetActive(false);
        }

        private void OnResetSaveClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.ResetAllSaveData();
            RefreshFromSettings();
            appRoot.Settings?.TryVibrate();
            Debug.Log("[Settings] 저장 데이터를 초기화했습니다.");
        }

        private void OnMainMenuClicked()
        {
            var appRoot = AppRoot.Instance ?? AppRoot.EnsureCreated();
            appRoot.ReturnToMainMenuFromGame();
        }
    }
}
