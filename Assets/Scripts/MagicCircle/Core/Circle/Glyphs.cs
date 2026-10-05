// Glyph art for the center symbols and the runes, in a unit box (-1..1, y
// down, -y is "outward" once placed on a ring). The path data is the web
// build's markup verbatim. These are placeholder designs for the simulator, not
// canon art. Swapping one means editing its entry and nothing else.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MagicCircleSim.Design;

namespace MagicCircleSim.Circle
{
    /// <summary>A glyph as a list of shapes. Combine glyph parts with <c>+</c>.</summary>
    public sealed class GlyphArt
    {
        enum Kind { Path, Circle, Dot }

        readonly struct Shape
        {
            public readonly Kind Kind;
            public readonly string D;
            public readonly double X, Y, R;

            public Shape(Kind kind, string d, double x, double y, double r)
            {
                Kind = kind;
                D = d;
                X = x;
                Y = y;
                R = r;
            }
        }

        readonly Shape[] shapes;

        GlyphArt(Shape[] shapes) => this.shapes = shapes;

        public static GlyphArt Path(string d) => new GlyphArt(new[] { new Shape(Kind.Path, d, 0, 0, 0) });

        public static GlyphArt Circle(double r) => new GlyphArt(new[] { new Shape(Kind.Circle, null, 0, 0, r) });

        public static GlyphArt Dot(double x, double y, double r = 0.13) => new GlyphArt(new[] { new Shape(Kind.Dot, null, x, y, r) });

        public static GlyphArt operator +(GlyphArt a, GlyphArt b)
        {
            var joined = new Shape[a.shapes.Length + b.shapes.Length];
            a.shapes.CopyTo(joined, 0);
            b.shapes.CopyTo(joined, a.shapes.Length);
            return new GlyphArt(joined);
        }

        public static GlyphArt Join(IEnumerable<GlyphArt> parts)
        {
            var all = new List<Shape>();
            foreach (var part in parts) all.AddRange(part.shapes);
            return new GlyphArt(all.ToArray());
        }

        public void DrawInto(ArtCanvas canvas)
        {
            foreach (var shape in shapes)
            {
                switch (shape.Kind)
                {
                    case Kind.Path: canvas.Path(shape.D); break;
                    case Kind.Circle: canvas.Circle(shape.X, shape.Y, shape.R); break;
                    case Kind.Dot: canvas.Dot(shape.X, shape.Y, shape.R); break;
                }
            }
        }
    }

    public static class Glyphs
    {
        static GlyphArt P(string d) => GlyphArt.Path(d);
        static GlyphArt C(double r) => GlyphArt.Circle(r);
        static GlyphArt Dot(double x, double y, double r = 0.13) => GlyphArt.Dot(x, y, r);

        static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Points evenly around a circle, starting at the top.</summary>
        static (double x, double y)[] Around(int count, double radius)
        {
            var points = new (double, double)[count];
            for (var i = 0; i < count; i++)
            {
                var a = (double)i / count * Math.PI * 2 - Math.PI / 2;
                points[i] = (radius * Math.Cos(a), radius * Math.Sin(a));
            }
            return points;
        }

        static GlyphArt DotsAround(int count, double radius, double dotR)
        {
            var dots = new List<GlyphArt>();
            foreach (var (x, y) in Around(count, radius)) dots.Add(Dot(x, y, dotR));
            return GlyphArt.Join(dots);
        }

        static readonly GlyphArt[] ElementArt =
        {
            P("M-0.8,-0.35 Q0,-0.95 0.8,-0.35 M-0.8,0.1 Q0,-0.5 0.8,0.1 M-0.5,0.55 Q0,0.15 0.5,0.55"),
            P("M0,0.9 L0,-0.85 M0,0.15 L-0.6,-0.35 M0,-0.25 L0.6,-0.7 M0,0.5 L0.55,0.05"),
            P("M0,-0.9 C0.55,-0.2 0.75,0.2 0.6,0.5 A0.62,0.62 0 0 1 -0.6,0.5 C-0.75,0.2 -0.55,-0.2 0,-0.9 Z")
                + P("M-0.35,0.35 Q-0.17,0.2 0,0.35 T0.35,0.35"),
            P("M0,-0.9 L0.8,0.7 L-0.8,0.7 Z M0,-0.15 L0.38,0.55 L-0.38,0.55 Z"),
            P("M-0.7,-0.7 H0.7 V0.7 H-0.7 Z M-0.7,0 H0.7 M0,-0.7 V0.7"),
        };

        public static GlyphArt Element(ElementId id) => ElementArt[(int)id];

        static GlyphArt CountGlyph(int n)
        {
            if (n == 1) return Dot(0, 0, 0.2);
            if (n == 2) return Dot(-0.4, 0, 0.18) + Dot(0.4, 0, 0.18);
            if (n == 3) return DotsAround(3, 0.5, 0.17);
            return Dot(0, 0, 0.14) + DotsAround(n - 1, 0.62, 0.14);
        }

        static string Ticks(int n, double y)
        {
            var step = 1.2 / Math.Max(n - 1, 1);
            var d = new StringBuilder();
            for (var i = 0; i < n; i++)
            {
                var x = n == 1 ? 0 : -0.6 + i * step;
                if (i > 0) d.Append(' ');
                d.Append('M').Append(N(x)).Append(',').Append(N(y - 0.12)).Append(" V").Append(N(y + 0.12));
            }
            return d.ToString();
        }

        static GlyphArt Arrow(int ticks) => P($"M0,0.8 V-0.8 M-0.3,-0.5 L0,-0.8 L0.3,-0.5 {Ticks(ticks, 0.3)}");

        static GlyphArt Hourglass(int ticks) => P($"M-0.5,-0.8 H0.5 L-0.5,0.4 H0.5 Z {Ticks(ticks, 0.75)}");

        /// <summary>Keyed by rune id. Every rune in the catalog needs a row (GlyphTests).</summary>
        static readonly Dictionary<string, GlyphArt> RuneArt = new Dictionary<string, GlyphArt>
        {
            ["form-disk"] = C(0.8) + Dot(0, 0) + Dot(-0.35, 0.2) + Dot(0.35, 0.2) + Dot(0, -0.38),
            ["form-square"] = P("M-0.75,-0.75 H0.75 V0.75 H-0.75 Z") + Dot(-0.3, -0.3) + Dot(0.3, -0.3) + Dot(-0.3, 0.3) + Dot(0.3, 0.3),
            ["form-ring"] = C(0.65) + DotsAround(6, 0.65, 0.15),
            ["form-line"] = P("M-0.9,0 H0.9") + Dot(-0.55, 0) + Dot(0, 0) + Dot(0.55, 0),
            ["radius-2"] = Arrow(1),
            ["radius-4"] = Arrow(2),
            ["radius-6"] = Arrow(3),
            ["angle-1"] = C(0.75) + P("M0,0 V-0.75") + Dot(0, 0),
            ["angle-0.5"] = P("M-0.75,0 A0.75,0.75 0 0 1 0.75,0 Z") + Dot(0, 0),
            ["count-1"] = CountGlyph(1),
            ["count-2"] = CountGlyph(2),
            ["count-3"] = CountGlyph(3),
            ["count-7"] = CountGlyph(7),
            ["path-orbit"] = P("M0.55,-0.55 A0.75,0.75 0 1 0 0.75,0 M0.2,-0.6 L0.55,-0.55 L0.55,-0.9"),
            ["path-straight"] = P("M0,0.85 V-0.85 M-0.4,-0.45 L0,-0.85 L0.4,-0.45"),
            ["lifetime-3"] = Hourglass(3),
            ["lifetime-5"] = Hourglass(5),
            ["dist-symmetric"] = P("M0,-0.9 V-0.6 M0,-0.4 V-0.1 M0,0.1 V0.4 M0,0.6 V0.9")
                + Dot(-0.5, -0.3) + Dot(0.5, -0.3) + Dot(-0.4, 0.45) + Dot(0.4, 0.45),
            ["dist-even"] = P("M-0.6,-0.7 V0.7 M0,-0.7 V0.7 M0.6,-0.7 V0.7"),
            ["dist-random"] = Dot(-0.55, -0.5) + Dot(0.3, -0.65) + Dot(0.6, 0.1) + Dot(-0.15, 0.05) + Dot(-0.6, 0.55) + Dot(0.25, 0.6),
            ["speed-10"] = P("M-0.6,0.3 L0,-0.3 L0.6,0.3"),
            ["speed-20"] = P("M-0.6,0.05 L0,-0.55 L0.6,0.05 M-0.6,0.6 L0,0 L0.6,0.6"),
        };

        /// <summary>The rune's art, or null when it has none yet.</summary>
        public static GlyphArt Rune(string runeId) => runeId != null && RuneArt.TryGetValue(runeId, out var art) ? art : null;
    }
}
