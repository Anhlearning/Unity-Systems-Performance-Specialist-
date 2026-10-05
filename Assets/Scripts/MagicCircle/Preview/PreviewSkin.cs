// The preview's IMGUI look: the web build's dark palette as GUIStyles. Built
// lazily inside OnGUI, the only place GUI.skin may be read.

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MagicCircleSim.Preview
{
    public sealed class PreviewSkin : IDisposable
    {
        public static readonly Color Bg = Hex("#0b0d14");
        public static readonly Color Panel = Hex("#141824");
        public static readonly Color Panel2 = Hex("#1b2030");
        public static readonly Color Line = Hex("#2a3146");
        public static readonly Color Text = Hex("#e6e8ef");
        public static readonly Color Muted = Hex("#8b93a8");
        public static readonly Color Accent = Hex("#c8a6ff");
        public static readonly Color Error = Hex("#ff6b6b");
        public static readonly Color Info = Hex("#7cc4ff");
        public static readonly Color Assume = Hex("#ffcf6b");
        public static readonly Color Ok = Hex("#6be29b");

        public readonly Texture2D BgTexture;
        public readonly Texture2D PanelTexture;
        public readonly GUIStyle Title, Heading, Body, MutedLabel, Small, Button, Primary, Tab, TabActive, Option, OptionSelected, Tag;
        readonly List<Object> textures = new List<Object>();

        public PreviewSkin()
        {
            BgTexture = Solid(Bg);
            PanelTexture = Solid(Panel);
            var button = Solid(Panel2);
            var hover = Solid(Color.Lerp(Panel2, Accent, 0.12f));
            var selected = Solid(Color.Lerp(Panel2, Accent, 0.22f));

            Title = Label(18, Text, FontStyle.Bold);
            Heading = Label(12, Muted, FontStyle.Bold);
            Body = Label(13, Text);
            Body.wordWrap = true;
            MutedLabel = Label(12, Muted);
            MutedLabel.wordWrap = true;
            Small = Label(10, Muted);

            Button = Clickable(13, Text, button, hover, hover);
            Primary = Clickable(14, Hex("#1a1230"), Solid(Accent), Solid(Color.Lerp(Accent, Color.white, 0.2f)), Solid(Accent));
            Primary.fontStyle = FontStyle.Bold;
            Tab = Clickable(13, Muted, button, hover, hover);
            TabActive = Clickable(13, Accent, selected, selected, selected);

            Option = Clickable(11, Muted, button, hover, hover);
            // The icon is drawn over the top of the button, so the label sits at the bottom.
            Option.alignment = TextAnchor.LowerCenter;
            Option.fixedWidth = 84;
            Option.fixedHeight = 82;
            Option.wordWrap = true;
            Option.padding = new RectOffset(4, 4, 6, 4);
            OptionSelected = new GUIStyle(Option);
            SetStates(OptionSelected, Accent, selected, selected, selected);

            Tag = Label(10, Muted);
            Tag.normal.background = button;
            Tag.padding = new RectOffset(6, 6, 1, 1);
            Tag.margin = new RectOffset(4, 0, 3, 0);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        static GUIStyle Label(int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var label = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, richText = false };
            label.normal.textColor = color;
            return label;
        }

        static GUIStyle Clickable(int size, Color text, Texture2D normal, Texture2D hover, Texture2D active)
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = size, alignment = TextAnchor.MiddleCenter };
            style.border = new RectOffset(0, 0, 0, 0);
            SetStates(style, text, normal, hover, active);
            return style;
        }

        static void SetStates(GUIStyle style, Color text, Texture2D normal, Texture2D hover, Texture2D active)
        {
            foreach (var state in new[] { style.normal, style.focused, style.onNormal, style.onFocused })
            {
                state.background = normal;
                state.textColor = text;
            }
            foreach (var state in new[] { style.hover, style.onHover })
            {
                state.background = hover;
                state.textColor = text;
            }
            foreach (var state in new[] { style.active, style.onActive })
            {
                state.background = active;
                state.textColor = text;
            }
        }

        public void Dispose()
        {
            foreach (var texture in textures) Object.Destroy(texture);
            textures.Clear();
        }
    }
}
