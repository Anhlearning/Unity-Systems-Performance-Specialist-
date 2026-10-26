using System;
using Ignite.Unity.Localization;
using Ignite.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IgniteSample
{
    /// <summary>Dựng uGUI bằng code để sample không cần prefab. Game thật dùng prefab.</summary>
    public static class SampleUi
    {
        static Font s_font;
        static Font DefaultFont => s_font != null ? s_font : s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static readonly DefaultControls.Resources s_resources = new DefaultControls.Resources();

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // 1280×800 là độ phân giải Steam Deck: chữ phải đọc được ở đây.
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Một UIScreen phủ cả màn hình, có nền màu.</summary>
        public static T CreateScreen<T>(Transform parent, string name, Color background) where T : UIScreen
        {
            var panel = CreatePanel(parent, background);
            panel.name = name;
            return panel.gameObject.AddComponent<T>();
        }

        public static RectTransform CreatePanel(Transform parent, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Text CreateLabel(Transform parent, string text, Vector2 anchor, int fontSize = 32)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = DefaultFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1000, 60);
            return label;
        }

        public static Button CreateButton(Transform parent, string text, Vector2 anchor, UnityAction onClick, float width = 300f)
        {
            var go = new GameObject($"Button {text}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.9f);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(width, 56);

            var label = CreateLabel(go.transform, text, new Vector2(0.5f, 0.5f), 26);
            ((RectTransform)label.transform).sizeDelta = rect.sizeDelta;

            var button = go.GetComponent<Button>();
            button.colors = HighlightColors(button.colors);
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Thanh trượt 0..1 kèm nhãn bên trái.</summary>
        public static Slider CreateSlider(Transform parent, string text, Vector2 anchor, float value, Action<float> onChange)
        {
            var row = new GameObject($"Row {text}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = anchor;
            rowRect.sizeDelta = new Vector2(700, 44);

            var label = CreateLabel(row.transform, text, new Vector2(0f, 0.5f), 24);
            label.alignment = TextAnchor.MiddleLeft;
            var labelRect = (RectTransform)label.transform;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(280, 44);

            var sliderGo = DefaultControls.CreateSlider(s_resources);
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(1f, 0.5f);
            sliderRect.sizeDelta = new Vector2(380, 24);

            var slider = sliderGo.GetComponent<Slider>();
            slider.SetValueWithoutNotify(value);
            slider.colors = HighlightColors(slider.colors);
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        /// <summary>Gắn key dịch lên Text (của label hoặc nút): chữ tự đổi khi đổi ngôn ngữ.</summary>
        public static LocalizedText Localize(Component textOrButton, LocalizationService localization, string key, params object[] arguments)
        {
            var text = textOrButton as Text ?? textOrButton.GetComponentInChildren<Text>();
            var localized = text.gameObject.AddComponent<LocalizedText>();
            localized.Bind(localization, key, arguments);
            return localized;
        }

        public static void EnsureEventSystem(Transform parent)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }

        /// <summary>Phần tử đang được chọn bằng gamepad phải nhìn thấy rõ.</summary>
        static ColorBlock HighlightColors(ColorBlock colors)
        {
            colors.selectedColor = new Color(1f, 0.85f, 0.3f);
            colors.highlightedColor = new Color(0.8f, 0.9f, 1f);
            return colors;
        }
    }
}
