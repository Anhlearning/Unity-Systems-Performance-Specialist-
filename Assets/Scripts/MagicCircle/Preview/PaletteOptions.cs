// The palette's tabs and buttons, one list per circle layer, each with the
// glyph art it shows as a drawing. Pure, so the palette panel only lays them
// out and rasterizes the icons once.

using System.Collections.Generic;
using MagicCircleSim.Circle;
using MagicCircleSim.Design;

namespace MagicCircleSim.Preview
{
    public enum PaletteTab { Center, V1, V2, V3, V4 }

    public sealed class PaletteOption
    {
        public readonly string Id;
        public readonly string Label;
        /// <summary>Icon art in its own viewBox.</summary>
        public readonly CircleDrawing Icon;
        /// <summary>Source row or assumption id.</summary>
        public readonly string Source;
        /// <summary>Sheet 08 id when the source still marks this as unconfirmed.</summary>
        public readonly string Review;

        public PaletteOption(string id, string label, CircleDrawing icon, string source, string review = null)
        {
            Id = id;
            Label = label;
            Icon = icon;
            Source = source;
            Review = review;
        }
    }

    public static class PaletteOptions
    {
        public static readonly (PaletteTab tab, string label, string hint)[] Tabs =
        {
            (PaletteTab.Center, "Tâm", "Ký hiệu nguyên tố, lõi của vòng phép."),
            (PaletteTab.V1, "Vòng 1", "Bản chất: cách nguyên tố biểu hiện ra môi trường."),
            (PaletteTab.V2, "Vòng 2", "Pattern: hình dạng nguyên tố thể hiện ra bên ngoài."),
            (PaletteTab.V3, "Vòng 3", "Hình dạng và vùng xuất hiện. Chỉ một rune hình dạng."),
            (PaletteTab.V4, "Vòng 4", "Chuyển động, thời gian tồn tại và cách phân bố."),
        };

        public static readonly RuneCategory[] V3Categories = { RuneCategory.Form, RuneCategory.Radius, RuneCategory.Angle, RuneCategory.Count };
        public static readonly RuneCategory[] V4Categories = { RuneCategory.Path, RuneCategory.Lifetime, RuneCategory.Distribution, RuneCategory.Speed };

        static readonly ViewBox UnitBox = ViewBox.Centered(1.3);
        const double GlyphStroke = 0.14;
        const double RingStroke = 2.4;
        const string IconInk = "#ffffff";

        static CircleDrawing Icon(ViewBox box, double stroke, System.Action<ArtCanvas> draw)
        {
            var canvas = new ArtCanvas(box, IconInk, StrokeStyle.Solid(stroke));
            draw(canvas);
            return canvas.Drawing;
        }

        public static List<PaletteOption> Elements()
        {
            var options = new List<PaletteOption>();
            foreach (var id in Design.Elements.All)
            {
                options.Add(new PaletteOption(id.ToString(), Design.Elements.Get(id).Name,
                    Icon(UnitBox, GlyphStroke, c => Glyphs.Element(id).DrawInto(c)), "CENTER_001"));
            }
            return options;
        }

        public static List<PaletteOption> Natures()
        {
            var options = new List<PaletteOption>();
            foreach (var id in Design.Natures.All)
            {
                var def = Design.Natures.Get(id);
                options.Add(new PaletteOption(id.ToString(), def.Name,
                    Icon(ViewBox.Centered(CircleRadii.V1 + 5), RingStroke, c => Rings.Nature(c, id)), "V1", def.Review));
            }
            return options;
        }

        public static List<PaletteOption> Patterns()
        {
            var options = new List<PaletteOption>();
            foreach (var id in Design.Patterns.All)
            {
                var def = Design.Patterns.Get(id);
                options.Add(new PaletteOption(id.ToString(), def.Name,
                    Icon(ViewBox.Centered(CircleRadii.V2 + 6), RingStroke, c => Rings.Pattern(c, id)), def.SourceId));
            }
            return options;
        }

        public static List<PaletteOption> RuneOptions(RuneCategory category)
        {
            var options = new List<PaletteOption>();
            foreach (var rune in Runes.InCategory(category))
            {
                var art = Glyphs.Rune(rune.Id);
                options.Add(new PaletteOption(rune.Id, rune.Label, Icon(UnitBox, GlyphStroke, c => art?.DrawInto(c)), rune.SourceId));
            }
            return options;
        }
    }
}
