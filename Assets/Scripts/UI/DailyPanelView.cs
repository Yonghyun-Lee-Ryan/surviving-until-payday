using System;
using System.Collections.Generic;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 오늘의 직장인·일일 미션·로컬 베스트 패널 (Unit 25).
    /// </summary>
    public sealed class DailyPanelView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        private static readonly Color CardColor = new Color(0.14f, 0.16f, 0.2f, 1f);
        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color Accent = new Color(0.28f, 0.48f, 0.62f, 1f);

        [SerializeField] private GameObject root;
        private Text dateLabel;
        private Text seedLabel;
        private Text streakLabel;
        private Text bestLabel;
        private Text missionsLabel;
        private Button playButton;
        private Button closeButton;
        private Action onPlay;
        private bool layoutReady;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            EnsureLayout();
            Hide();
        }

        public void Show(DailyContentState daily, Action playHandler)
        {
            EnsureLayout();
            onPlay = playHandler;
            Refresh(daily);
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

        public void Toggle(DailyContentState daily, Action playHandler)
        {
            if (root != null && root.activeSelf)
            {
                Hide();
            }
            else
            {
                Show(daily, playHandler);
            }
        }

        private void Refresh(DailyContentState daily)
        {
            if (daily == null)
            {
                return;
            }

            if (dateLabel != null)
            {
                dateLabel.text = $"오늘 {daily.DateKey}";
                UiFont.Apply(dateLabel, bold: true);
            }

            if (seedLabel != null)
            {
                seedLabel.text = $"시드 {DailyChallenge.SeedFromDateKey(daily.DateKey)}";
                UiFont.Apply(seedLabel);
            }

            if (bestLabel != null)
            {
                if (!daily.HasBestRecord)
                {
                    bestLabel.text = EmptyStateCopy.NoDailyBest;
                }
                else
                {
                    var survive = daily.BestSurvived ? "생존" : "실패";
                    bestLabel.text =
                        $"베스트 {survive} · {KoreanWonFormatter.Format(daily.BestCash)} · " +
                        $"{daily.BestDaysSurvived}일 · 스트레스 {daily.BestStress} · 회사 {daily.BestCompanyScore}";
                }

                UiFont.Apply(bestLabel);
            }

            if (streakLabel != null)
            {
                var bonus = daily.LastVisitBonusExperience > 0
                    ? $" · 오늘 출석 +{daily.LastVisitBonusExperience} XP"
                    : string.Empty;
                streakLabel.text = $"연속 접속 {daily.LoginStreak}일{bonus}";
                UiFont.Apply(streakLabel, bold: true);
            }

            if (missionsLabel != null)
            {
                missionsLabel.text = BuildMissionsText(daily.Missions);
                UiFont.Apply(missionsLabel);
            }
        }

        private static string BuildMissionsText(IReadOnlyList<DailyMissionRuntime> missions)
        {
            if (missions == null || missions.Count == 0)
            {
                return EmptyStateCopy.NoDailyMissions;
            }

            var lines = new List<string> { "오늘의 미션" };
            for (var i = 0; i < missions.Count; i++)
            {
                lines.Add(DailyMissionCopy.FormatLine(missions[i]));
            }

            return string.Join("\n", lines);
        }

        private void EnsureLayout()
        {
            if (layoutReady && bestLabel != null && missionsLabel != null && streakLabel != null)
            {
                return;
            }

            layoutReady = false;
            if (root == null)
            {
                root = gameObject;
            }

            var existingCard = root.transform.Find("DailyCard");
            if (existingCard != null)
            {
                existingCard.name = "DailyCard_PendingDestroy";
                UnityEngine.Object.Destroy(existingCard.gameObject);
            }

            var rootRect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            rootImage.color = OverlayColor;
            rootImage.raycastTarget = true;

            var card = new GameObject("DailyCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 860f);
            card.GetComponent<Image>().color = CardColor;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(card.transform, "Title", "오늘의 직장인", 34, 44f, bold: true);
            dateLabel = CreateLabel(card.transform, "Date", "오늘", 24, 32f, bold: true);
            seedLabel = CreateLabel(card.transform, "Seed", "시드", 20, 28f, bold: false);
            streakLabel = CreateLabel(card.transform, "Streak", "연속 접속", 22, 30f, bold: true);

            bestLabel = CreateWrappedLabel(card.transform, "Best", "베스트", 120f);
            missionsLabel = CreateWrappedLabel(card.transform, "Missions", "미션", 220f);

            playButton = CreateButton(card.transform, "PlayButton", "도전하기");
            closeButton = CreateButton(card.transform, "CloseButton", "닫기");
            playButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => onPlay?.Invoke());
            closeButton.onClick.AddListener(Hide);
            layoutReady = true;
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            string text,
            int fontSize,
            float height,
            bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold);
            return label;
        }

        private static Text CreateWrappedLabel(Transform parent, string name, string text, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = RowColor;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 12f);
            labelRect.offsetMax = new Vector2(-16f, -12f);
            var label = labelGo.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            UiFont.Apply(label);
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Accent;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = 56f;
            element.preferredHeight = 56f;

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
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UiFont.Apply(label, bold: true);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }
    }
}
