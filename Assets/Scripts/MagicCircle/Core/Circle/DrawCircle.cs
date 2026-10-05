// DrawCircle: a MagicCircle as one CircleDrawing. The editor shows it and the
// cast stage paints it on the ground, so both always agree.
//
//   var drawing = DrawCircle.Build(circle);

using System.Collections.Generic;
using MagicCircleSim.Design;

namespace MagicCircleSim.Circle
{
    public static class DrawCircle
    {
        public static readonly ViewBox Box = ViewBox.Centered(100);

        const double RuneScale = 5;
        /// <summary>The web build's invisible hit circle around each rune.</summary>
        const double RuneHitRadius = 1.25;
        const double InkWidth = 1.4;
        /// <summary>Stroke width inside scaled glyph groups, in glyph units.</summary>
        const double GlyphWidth = 0.25;
        const string NeutralInk = "#b9bfd3";

        public static string InkOf(MagicCircle circle) => circle.Center.HasValue ? Elements.Get(circle.Center.Value).Color : NeutralInk;

        /// <param name="ink">Overrides the element color, e.g. white for the ground texture.</param>
        public static CircleDrawing Build(MagicCircle circle, string ink = null)
        {
            var canvas = new ArtCanvas(Box, ink ?? InkOf(circle), StrokeStyle.Solid(InkWidth));

            if (circle.Center.HasValue)
            {
                Glyph(canvas, Affine2.Scale(CircleRadii.Center * 0.7), Glyphs.Element(circle.Center.Value));
            }
            else Rings.Placeholder(canvas, CircleRadii.Center * 0.7);

            if (circle.Nature.HasValue) Rings.Nature(canvas, circle.Nature.Value);
            else Rings.Placeholder(canvas, CircleRadii.V1);

            if (circle.Pattern.HasValue) Rings.Pattern(canvas, circle.Pattern.Value);
            else Rings.Placeholder(canvas, CircleRadii.V2);

            RuneBand(canvas, circle.RunesOnBand(Design.RuneBand.V3), CircleRadii.V3Runes);
            canvas.Circle(0, 0, CircleRadii.V3);
            RuneBand(canvas, circle.RunesOnBand(Design.RuneBand.V4), CircleRadii.V4Runes);
            canvas.Circle(0, 0, CircleRadii.V4);
            canvas.Circle(0, 0, CircleRadii.V4 + 2.5);

            canvas.Style = new StrokeStyle { Width = InkWidth, Opacity = 0.25, DashOn = 1, DashOff = 4 };
            canvas.Circle(0, 0, CircleRadii.V5);
            return canvas.Drawing;
        }

        /// <summary>Runes spread evenly around their band, the first at the top.</summary>
        static void RuneBand(ArtCanvas canvas, List<RuneDef> runes, double radius)
        {
            for (var i = 0; i < runes.Count; i++)
            {
                var degrees = (double)i / runes.Count * 360;
                // Rotate first so the glyph's top faces outward wherever it sits.
                var place = Affine2.Rotate(degrees) * Affine2.Translate(0, -radius) * Affine2.Scale(RuneScale);
                canvas.Push(place);
                canvas.MarkRune(runes[i].Id, RuneHitRadius);
                canvas.Pop();
                var art = Glyphs.Rune(runes[i].Id);
                if (art != null) Glyph(canvas, place, art);
            }
        }

        static void Glyph(ArtCanvas canvas, Affine2 place, GlyphArt art)
        {
            canvas.Push(place);
            canvas.Style.Width = GlyphWidth;
            art.DrawInto(canvas);
            canvas.Pop();
        }
    }
}
