using SurviveUntilPayday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// Game 씬 HUD/사건/선택지 레이아웃을 런타임에 강제 적용한다.
    /// </summary>
    public static class GameplayLayoutApplier
    {
        public static void Apply(GameHudView hud, EventPanelView eventPanel, ChoicePanelView choicePanel)
        {
            ApplyHud(hud);
            ApplyEventPanel(eventPanel);
            ApplyChoicePanel(choicePanel);
            // HUD를 마지막에 한 번 더 앞으로 (날짜·게이지 한글이 가려지지 않게)
            if (hud != null)
            {
                hud.transform.SetAsLastSibling();
            }
        }

        private static void ApplyHud(GameHudView hud)
        {
            if (hud == null)
            {
                return;
            }

            hud.transform.SetAsLastSibling();

            var hudRect = hud.transform as RectTransform;
            if (hudRect != null)
            {
                hudRect.anchorMin = new Vector2(0f, 1f);
                hudRect.anchorMax = new Vector2(1f, 1f);
                hudRect.pivot = new Vector2(0.5f, 1f);
                hudRect.anchoredPosition = new Vector2(0f, -4f);
                hudRect.sizeDelta = new Vector2(-24f, 340f);
            }

            // HUD Image가 자식을 가리지 않도록 — 자식이 항상 위. raycast만 끔.
            var hudImage = hud.GetComponent<Image>();
            if (hudImage != null)
            {
                hudImage.raycastTarget = false;
            }

            var day = ForceTopLabel(hud, "DayLabel", isCash: false, "1일");
            var cash = ForceTopLabel(hud, "CashLabel", isCash: true, "0원");
            hud.BindLabels(day, cash, hud.transform.Find("CrisisBanner")?.gameObject,
                hud.transform.Find("CrisisBanner/CrisisLabel")?.GetComponent<Text>());

            FixGauge(hud.HealthGauge, "건강", 0);
            FixGauge(hud.StressGauge, "스트레스", 1);
            FixGauge(hud.HappinessGauge, "행복도", 2);
            FixGauge(hud.CompanyGauge, "회사 평가", 3);

            day?.transform.SetAsLastSibling();
            cash?.transform.SetAsLastSibling();
            hud.RefreshTopLabelBindings();
            hud.EnsureInGameSettingsButton();
        }

        private static Text ForceTopLabel(GameHudView hud, string name, bool isCash, string fallback)
        {
            var existing = hud.transform.Find(name)?.GetComponent<Text>();
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(hud.transform, false);
                existing = go.AddComponent<Text>();
                existing.text = fallback;
            }

            var rect = existing.rectTransform;
            rect.anchorMin = isCash ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = isCash ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
            rect.pivot = isCash ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = isCash ? new Vector2(-20f, -10f) : new Vector2(20f, -10f);
            rect.sizeDelta = new Vector2(-12f, 48f);
            rect.SetAsLastSibling();

            existing.fontSize = 36;
            existing.alignment = isCash ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            existing.color = new Color(0.1f, 0.12f, 0.16f, 1f);
            existing.raycastTarget = false;
            existing.horizontalOverflow = HorizontalWrapMode.Overflow;
            existing.verticalOverflow = VerticalWrapMode.Overflow;
            existing.enabled = true;
            existing.gameObject.SetActive(true);
            UiFont.Apply(existing, bold: true);
            return existing;
        }

        private static void FixGauge(StatGaugeView gauge, string displayName, int index)
        {
            if (gauge == null)
            {
                return;
            }

            var rect = gauge.transform as RectTransform;
            if (rect != null)
            {
                const int count = 4;
                const float pad = 0.015f;
                var slot = (1f - pad * 2f) / count;
                rect.anchorMin = new Vector2(pad + slot * index, 0f);
                rect.anchorMax = new Vector2(pad + slot * (index + 1), 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 8f);
                rect.sizeDelta = new Vector2(-8f, 160f);
            }

            var gaugeImage = gauge.GetComponent<Image>();
            if (gaugeImage != null)
            {
                gaugeImage.raycastTarget = false;
            }

            // Track을 먼저 배치(뒤)
            var track = gauge.transform.Find("Track") as RectTransform;
            if (track != null)
            {
                track.SetAsFirstSibling();
                track.anchorMin = new Vector2(0.08f, 0.38f);
                track.anchorMax = new Vector2(0.92f, 0.52f);
                track.offsetMin = Vector2.zero;
                track.offsetMax = Vector2.zero;
            }

            // Name — 게이지 박스 안 최상단, Overflow로 한글이 잘리지 않게
            var nameTf = gauge.transform.Find("Name") as RectTransform;
            if (nameTf == null)
            {
                var go = new GameObject("Name", typeof(RectTransform));
                go.transform.SetParent(gauge.transform, false);
                go.AddComponent<Text>();
                nameTf = go.GetComponent<RectTransform>();
            }

            nameTf.anchorMin = new Vector2(0f, 1f);
            nameTf.anchorMax = new Vector2(1f, 1f);
            nameTf.pivot = new Vector2(0.5f, 1f);
            nameTf.anchoredPosition = new Vector2(0f, -4f);
            nameTf.sizeDelta = new Vector2(-6f, 44f);
            nameTf.SetAsLastSibling();

            var nameText = nameTf.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = displayName;
                nameText.fontSize = 26;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = new Color(0.1f, 0.12f, 0.16f, 1f);
                nameText.raycastTarget = false;
                nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameText.verticalOverflow = VerticalWrapMode.Overflow;
                nameText.enabled = true;
                nameText.gameObject.SetActive(true);
                UiFont.Apply(nameText, bold: true);
                gauge.BindNameLabel(nameText);
            }

            var valueTf = gauge.transform.Find("Value") as RectTransform;
            if (valueTf != null)
            {
                valueTf.anchorMin = new Vector2(0f, 0f);
                valueTf.anchorMax = new Vector2(1f, 0f);
                valueTf.pivot = new Vector2(0.5f, 0f);
                valueTf.anchoredPosition = new Vector2(0f, 4f);
                valueTf.sizeDelta = new Vector2(-6f, 32f);
                valueTf.SetAsLastSibling();
                var valueText = valueTf.GetComponent<Text>();
                if (valueText != null)
                {
                    valueText.fontSize = 24;
                    valueText.color = new Color(0.1f, 0.12f, 0.16f, 1f);
                    valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    valueText.verticalOverflow = VerticalWrapMode.Overflow;
                    UiFont.Apply(valueText);
                }
            }

            // Name이 Value보다 위(더 앞)에 오도록
            nameTf.SetAsLastSibling();
            gauge.SetName(displayName);
        }

        private static void ApplyEventPanel(EventPanelView eventPanel)
        {
            if (eventPanel == null)
            {
                return;
            }

            var root = eventPanel.transform as RectTransform;
            root.anchorMin = new Vector2(0f, 0.26f);
            root.anchorMax = new Vector2(1f, 0.78f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.offsetMin = new Vector2(12f, 4f);
            root.offsetMax = new Vector2(-12f, -4f);

            var rootImage = eventPanel.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.93f, 0.94f, 0.96f, 0.15f);
                rootImage.raycastTarget = false;
            }

            var expression = root.Find("Expression");
            if (expression != null)
            {
                expression.gameObject.SetActive(false);
            }

            var background = root.Find("Background") as RectTransform
                             ?? root.Find("Illustration") as RectTransform;
            if (background != null)
            {
                if (background.name == "Illustration")
                {
                    background.name = "Background";
                }

                background.anchorMin = new Vector2(0.03f, 0.30f);
                background.anchorMax = new Vector2(0.97f, 0.98f);
                background.offsetMin = Vector2.zero;
                background.offsetMax = Vector2.zero;
                background.SetAsFirstSibling();
                var bgImage = background.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.preserveAspect = true;
                    bgImage.raycastTarget = false;
                    bgImage.color = Color.white;
                }
            }

            var title = root.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                title.gameObject.SetActive(false);
            }

            EnsureDescriptionCard(root);

            var desc = root.Find("DescriptionCard/Description")?.GetComponent<Text>()
                       ?? root.Find("Description")?.GetComponent<Text>();
            var bgImg = background != null ? background.GetComponent<Image>() : null;
            if (desc != null && bgImg != null)
            {
                eventPanel.Bind(null, desc, bgImg, null, null, null);
            }
        }

        private static void EnsureDescriptionCard(RectTransform eventRoot)
        {
            var desc = eventRoot.Find("Description")?.GetComponent<Text>()
                       ?? eventRoot.Find("DescriptionCard/Description")?.GetComponent<Text>();
            if (desc == null)
            {
                var go = new GameObject("Description", typeof(RectTransform));
                go.transform.SetParent(eventRoot, false);
                desc = go.AddComponent<Text>();
            }

            var card = eventRoot.Find("DescriptionCard") as RectTransform;
            if (card == null)
            {
                var cardGo = new GameObject("DescriptionCard", typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(eventRoot, false);
                card = cardGo.GetComponent<RectTransform>();
                var img = cardGo.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.96f);
                img.raycastTarget = false;
            }

            card.anchorMin = new Vector2(0.06f, 0.02f);
            card.anchorMax = new Vector2(0.94f, 0.26f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            card.SetAsLastSibling();

            var descRect = desc.rectTransform;
            descRect.SetParent(card, false);
            descRect.anchorMin = Vector2.zero;
            descRect.anchorMax = Vector2.one;
            descRect.offsetMin = new Vector2(20f, 12f);
            descRect.offsetMax = new Vector2(-20f, -12f);
            desc.fontSize = 34;
            desc.alignment = TextAnchor.MiddleCenter;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            desc.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            desc.raycastTarget = false;
            UiFont.Apply(desc);
            desc.gameObject.SetActive(true);
        }

        private static void ApplyChoicePanel(ChoicePanelView choicePanel)
        {
            if (choicePanel == null)
            {
                return;
            }

            var root = choicePanel.transform as RectTransform;
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 20f);
            root.sizeDelta = new Vector2(-40f, 460f);

            var offsets = new[] { 268f, 164f, 60f };
            for (var i = 0; i < 3; i++)
            {
                var button = root.Find($"Choice_{i}") as RectTransform;
                if (button == null)
                {
                    continue;
                }

                button.anchorMin = new Vector2(0f, 0f);
                button.anchorMax = new Vector2(1f, 0f);
                button.pivot = new Vector2(0.5f, 0f);
                button.anchoredPosition = new Vector2(0f, offsets[i]);
                button.sizeDelta = new Vector2(-36f, 90f);
            }

            choicePanel.EnsureRerollButton();
        }
    }
}
