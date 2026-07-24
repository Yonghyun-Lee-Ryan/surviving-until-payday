using System;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 선택지 버튼 3개. 입력만 Presenter로 전달한다.
    /// </summary>
    public sealed class ChoicePanelView : MonoBehaviour
    {
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [SerializeField] private Text[] choiceLabels = new Text[3];
        [SerializeField] private Button rerollAdButton;
        [SerializeField] private Text rerollAdLabel;

        public event Action<int> ChoiceClicked;
        public event Action RerollAdClicked;

        private bool wired;

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        public void SetChoices(string[] texts)
        {
            for (var i = 0; i < choiceButtons.Length; i++)
            {
                var hasText = texts != null && i < texts.Length && !string.IsNullOrEmpty(texts[i]);
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(hasText);
                }

                if (choiceLabels[i] != null)
                {
                    choiceLabels[i].text = hasText ? texts[i] : string.Empty;
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            for (var i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].interactable = interactable;
                }
            }
        }

        public void SetRerollVisible(bool visible, bool interactable, string label = null)
        {
            EnsureRerollButton();
            if (rerollAdButton == null)
            {
                return;
            }

            rerollAdButton.gameObject.SetActive(visible);
            rerollAdButton.interactable = interactable;
            if (rerollAdLabel != null && label != null)
            {
                rerollAdLabel.text = label;
            }
        }

        public void Bind(Button[] buttons, Text[] labels)
        {
            UnwireButtons();
            choiceButtons = buttons;
            choiceLabels = labels;
            WireButtons();
        }

        public void BindReroll(Button button, Text label)
        {
            UnwireReroll();
            rerollAdButton = button;
            rerollAdLabel = label;
            WireReroll();
        }

        public void EnsureRerollButton()
        {
            if (rerollAdButton != null)
            {
                return;
            }

            var go = new GameObject("RerollAdButton", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(-40f, 56f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.35f, 0.28f, 0.45f, 1f);
            rerollAdButton = go.AddComponent<Button>();
            rerollAdButton.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            rerollAdLabel = labelGo.AddComponent<Text>();
            rerollAdLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            rerollAdLabel.fontSize = 26;
            rerollAdLabel.alignment = TextAnchor.MiddleCenter;
            rerollAdLabel.color = Color.white;
            rerollAdLabel.text = "광고: 선택지 새로고침";
            WireReroll();
            go.SetActive(false);
        }

        private void WireButtons()
        {
            if (wired || choiceButtons == null)
            {
                return;
            }

            for (var i = 0; i < choiceButtons.Length; i++)
            {
                var index = i;
                if (choiceButtons[i] == null)
                {
                    continue;
                }

                choiceButtons[i].onClick.AddListener(() => ChoiceClicked?.Invoke(index));
            }

            WireReroll();
            wired = true;
        }

        private void UnwireButtons()
        {
            if (!wired || choiceButtons == null)
            {
                UnwireReroll();
                wired = false;
                return;
            }

            for (var i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.RemoveAllListeners();
                }
            }

            UnwireReroll();
            wired = false;
        }

        private void WireReroll()
        {
            if (rerollAdButton == null)
            {
                return;
            }

            rerollAdButton.onClick.RemoveListener(HandleRerollClicked);
            rerollAdButton.onClick.AddListener(HandleRerollClicked);
        }

        private void UnwireReroll()
        {
            if (rerollAdButton == null)
            {
                return;
            }

            rerollAdButton.onClick.RemoveListener(HandleRerollClicked);
        }

        private void HandleRerollClicked()
        {
            RerollAdClicked?.Invoke();
        }
    }
}
