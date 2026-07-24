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

        public event Action<int> ChoiceClicked;

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

        public void Bind(Button[] buttons, Text[] labels)
        {
            UnwireButtons();
            choiceButtons = buttons;
            choiceLabels = labels;
            WireButtons();
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

            wired = true;
        }

        private void UnwireButtons()
        {
            if (!wired || choiceButtons == null)
            {
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

            wired = false;
        }
    }
}
