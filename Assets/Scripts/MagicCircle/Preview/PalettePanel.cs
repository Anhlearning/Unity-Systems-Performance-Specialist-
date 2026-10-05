// PalettePanel: one tab per circle layer, from the center outward. Every
// button toggles, so picking a selected option takes it off the circle again.
// Icons are rasterized once from the same glyph art the circle uses.

using System;
using System.Collections.Generic;
using MagicCircleSim.Design;
using MagicCircleSim.View;
using UnityEngine;

namespace MagicCircleSim.Preview
{
    public sealed class PalettePanel : IDisposable
    {
        const int IconPx = 96;
        const float IconSize = 44;
        const float Gap = 6;

        readonly List<PaletteOption> elements = PaletteOptions.Elements();
        readonly List<PaletteOption> natures = PaletteOptions.Natures();
        readonly List<PaletteOption> patterns = PaletteOptions.Patterns();
        readonly Dictionary<RuneCategory, List<PaletteOption>> runes = new Dictionary<RuneCategory, List<PaletteOption>>();
        readonly Dictionary<PaletteOption, CircleTexture> icons = new Dictionary<PaletteOption, CircleTexture>();
        PaletteTab tab = PaletteTab.Center;
        Vector2 scroll;

        public PalettePanel()
        {
            foreach (var category in Runes.Categories) runes[category] = PaletteOptions.RuneOptions(category);
            foreach (var list in new[] { elements, natures, patterns }) AddIcons(list);
            foreach (var list in runes.Values) AddIcons(list);
        }

        void AddIcons(List<PaletteOption> options)
        {
            foreach (var option in options)
            {
                var icon = new CircleTexture(IconPx, $"Icon {option.Id}");
                icon.Draw(option.Icon);
                icons[option] = icon;
            }
        }

        public void Draw(Rect rect, SpellSession session, PreviewSkin skin)
        {
            GUI.DrawTexture(rect, skin.PanelTexture);
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20));
            GUILayout.BeginHorizontal();
            foreach (var (id, label, _) in PaletteOptions.Tabs)
            {
                if (GUILayout.Button(label, id == tab ? skin.TabActive : skin.Tab, GUILayout.Height(28))) tab = id;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            GUILayout.Label(PaletteOptions.Tabs[(int)tab].hint, skin.MutedLabel);

            scroll = GUILayout.BeginScrollView(scroll);
            var circle = session.Circle;
            var width = rect.width - 40;
            switch (tab)
            {
                case PaletteTab.Center:
                    Grid(null, elements, o => circle.Center?.ToString() == o.Id, o => session.ToggleCenter(Parse<ElementId>(o.Id)), width, skin);
                    break;
                case PaletteTab.V1:
                    Grid(null, natures, o => circle.Nature?.ToString() == o.Id, o => session.ToggleNature(Parse<NatureId>(o.Id)), width, skin);
                    break;
                case PaletteTab.V2:
                    Grid(null, patterns, o => circle.Pattern?.ToString() == o.Id, o => session.TogglePattern(Parse<PatternId>(o.Id)), width, skin);
                    break;
                default:
                    var categories = tab == PaletteTab.V3 ? PaletteOptions.V3Categories : PaletteOptions.V4Categories;
                    foreach (var category in categories)
                    {
                        Grid(Runes.CategoryName(category).ToUpperInvariant(), runes[category], o => circle.Contains(o.Id), o => session.ToggleRune(o.Id), width, skin);
                    }
                    break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static T Parse<T>(string id) where T : struct => (T)Enum.Parse(typeof(T), id);

        void Grid(string title, List<PaletteOption> options, Func<PaletteOption, bool> isSelected, Action<PaletteOption> onPick, float width, PreviewSkin skin)
        {
            if (title != null)
            {
                GUILayout.Space(8);
                GUILayout.Label(title, skin.Heading);
            }
            var perRow = Mathf.Max(1, Mathf.FloorToInt((width + Gap) / (skin.Option.fixedWidth + Gap)));
            for (var start = 0; start < options.Count; start += perRow)
            {
                GUILayout.BeginHorizontal();
                for (var i = start; i < Mathf.Min(start + perRow, options.Count); i++)
                {
                    var option = options[i];
                    var selected = isSelected(option);
                    var label = option.Review != null ? $"{option.Label}\n{option.Review}" : option.Label;
                    var content = new GUIContent(label, $"Nguồn: {option.Source}");
                    var style = selected ? skin.OptionSelected : skin.Option;
                    var button = GUILayoutUtility.GetRect(content, style, GUILayout.Width(style.fixedWidth), GUILayout.Height(style.fixedHeight));
                    if (GUI.Button(button, content, style)) onPick(option);
                    var previous = GUI.color;
                    GUI.color = selected ? PreviewSkin.Accent : PreviewSkin.Text;
                    var iconRect = new Rect(button.center.x - IconSize / 2, button.y + 6, IconSize, IconSize);
                    GUI.DrawTexture(iconRect, icons[option].Texture, ScaleMode.ScaleToFit, true);
                    GUI.color = previous;
                    GUILayout.Space(Gap);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(Gap);
            }
        }

        public void Dispose()
        {
            foreach (var icon in icons.Values) icon.Dispose();
            icons.Clear();
        }
    }
}
