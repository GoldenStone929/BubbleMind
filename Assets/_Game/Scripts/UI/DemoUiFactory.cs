using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    /// <summary>
    /// Small runtime UI factory used by the self-contained demo scene.
    /// Keeping the UI in code makes scene generation repeatable while shared
    /// project assets provide the authored font and illustration layer.
    /// </summary>
    public static class DemoUiFactory
    {
        private static Font cachedFont;

        public static readonly Color Background = new Color(0.063f, 0.075f, 0.106f, 1f);
        public static readonly Color Surface = new Color(0.090f, 0.114f, 0.157f, 0.92f);
        public static readonly Color SurfaceLight = new Color(0.169f, 0.204f, 0.259f, 0.97f);
        public static readonly Color LineSubtle = new Color(0.867f, 0.910f, 0.957f, 0.16f);
        public static readonly Color LineStrong = new Color(0.867f, 0.910f, 0.957f, 0.36f);
        public static readonly Color Accent = new Color(0.38f, 0.84f, 0.76f, 1f);
        public static readonly Color Action = new Color(0.94f, 0.43f, 0.38f, 1f);
        public static readonly Color Positive = new Color(0.45f, 0.79f, 0.55f, 1f);
        public static readonly Color Warning = new Color(0.95f, 0.78f, 0.43f, 1f);
        public static readonly Color Danger = new Color(0.92f, 0.36f, 0.39f, 1f);
        public static readonly Color TextPrimary = new Color(0.96f, 0.97f, 0.98f, 1f);
        public static readonly Color TextSecondary = new Color(0.78f, 0.82f, 0.86f, 1f);
        public static readonly Color TextMuted = new Color(0.56f, 0.61f, 0.67f, 1f);

        public static Font Font
        {
            get
            {
                if (cachedFont != null)
                {
                    return cachedFont;
                }

                cachedFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
                if (cachedFont != null)
                {
                    return cachedFont;
                }

                try
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch (Exception)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return cachedFont;
            }
        }

        public static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "DemoCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem(parent);
            return canvas;
        }

        public static GameObject CreateScreenRoot(string name, Transform parent, float artworkAlpha)
        {
            Image background = CreatePanel(
                name,
                parent,
                Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Texture2D texture = Resources.Load<Texture2D>("AbyssalObservatory_Concept");
            if (texture != null && artworkAlpha > 0f)
            {
                RectTransform imageRect = CreateStretchRect("EnvironmentBackdrop", background.transform);
                RawImage image = imageRect.gameObject.AddComponent<RawImage>();
                image.texture = texture;
                image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(artworkAlpha));
                image.raycastTarget = false;
                AspectRatioFitter fitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);

                Image veil = CreatePanel(
                    "BackdropVeil",
                    background.transform,
                    new Color(0.018f, 0.028f, 0.035f, 0.42f),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                veil.raycastTarget = false;
            }

            return background.gameObject;
        }

        public static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        public static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return rect;
        }

        public static RectTransform CreateStretchRect(string name, Transform parent, float margin = 0f)
        {
            return CreateRect(
                name,
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(margin, margin),
                new Vector2(-margin, -margin));
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Image CreateFramedPanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color? lineColor = null,
            float lineWidth = 1f)
        {
            Image panel = CreatePanel(name, parent, color, anchorMin, anchorMax, offsetMin, offsetMax);
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = lineColor ?? LineSubtle;
            outline.effectDistance = new Vector2(Mathf.Max(1f, lineWidth), -Mathf.Max(1f, lineWidth));
            outline.useGraphicAlpha = true;
            return panel;
        }

        public static Image CreatePortrait(
            string name,
            Transform parent,
            Sprite portrait,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color? tint = null)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = portrait;
            image.preserveAspect = true;
            image.color = tint ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateBadge(
            string name,
            Transform parent,
            string value,
            Color background,
            Color foreground)
        {
            Image badge = CreateFramedPanel(
                name,
                parent,
                background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(foreground.r, foreground.g, foreground.b, 0.42f));
            Text label = CreateText("Label", badge.transform, value, 15, TextAnchor.MiddleCenter, foreground, FontStyle.Bold);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 16;
            label.rectTransform.offsetMin = new Vector2(7f, 3f);
            label.rectTransform.offsetMax = new Vector2(-7f, -3f);
            return label;
        }

        public static Image CreateDivider(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            Image line = CreatePanel(name, parent, LineSubtle, anchorMin, anchorMax, offsetMin, offsetMax);
            line.raycastTarget = false;
            return line;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateStretchRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateStretchRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.84f, 0.87f, 0.92f, 1f);
            colors.disabledColor = new Color(0.45f, 0.48f, 0.54f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = LineSubtle;
            outline.effectDistance = new Vector2(1f, -1f);
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            Text buttonText = CreateText(
                "Label",
                rect,
                label,
                24,
                TextAnchor.MiddleCenter,
                TextPrimary,
                FontStyle.Bold);
            buttonText.resizeTextForBestFit = true;
            buttonText.resizeTextMinSize = 14;
            buttonText.resizeTextMaxSize = 26;
            RectTransform labelRect = buttonText.rectTransform;
            labelRect.offsetMin = new Vector2(18f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            return button;
        }

        public static VerticalLayoutGroup AddVerticalLayout(
            GameObject gameObject,
            float spacing,
            RectOffset padding,
            TextAnchor alignment = TextAnchor.UpperCenter)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(
            GameObject gameObject,
            float spacing,
            RectOffset padding,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement SetLayout(
            GameObject gameObject,
            float preferredWidth = -1f,
            float preferredHeight = -1f,
            float flexibleWidth = -1f,
            float flexibleHeight = -1f)
        {
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<LayoutElement>();
            }

            if (preferredWidth >= 0f)
            {
                layout.preferredWidth = preferredWidth;
            }

            if (preferredHeight >= 0f)
            {
                layout.preferredHeight = preferredHeight;
            }

            if (flexibleWidth >= 0f)
            {
                layout.flexibleWidth = flexibleWidth;
            }

            if (flexibleHeight >= 0f)
            {
                layout.flexibleHeight = flexibleHeight;
            }

            return layout;
        }

        public static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
