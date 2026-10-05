// The ring art for Vòng 1 to Vòng 4, and the radii every layer sits at, in a
// viewBox of -100..100 so the same drawing serves the editor and the ground
// texture under the cast.

using System;
using MagicCircleSim.Design;

namespace MagicCircleSim.Circle
{
    /// <summary>Radii, in viewBox units, from the center outward.</summary>
    public static class CircleRadii
    {
        public const double Center = 13;
        public const double V1 = 22;
        public const double V2 = 33;
        /// <summary>The band between Vòng 2 and Vòng 3 where shape runes sit (V3_R01).</summary>
        public const double V3Runes = 44;
        public const double V3 = 53;
        public const double V4Runes = 63;
        public const double V4 = 73;
        /// <summary>Not part of the MVP. Drawn faint so the circle's final size reads.</summary>
        public const double V5 = 86;
    }

    public static class Rings
    {
        static void PolarPath(ArtCanvas canvas, int samples, Func<double, double> radiusAt)
        {
            var points = new Vec2[samples];
            for (var i = 0; i < samples; i++)
            {
                var a = (double)i / samples * Math.PI * 2;
                var r = radiusAt(a);
                points[i] = new Vec2(r * Math.Cos(a), r * Math.Sin(a));
            }
            canvas.Polyline(points, true);
        }

        /// <summary>Vòng 1: round, square, or the special form drawn as a six-petal wave.</summary>
        public static void Nature(ArtCanvas canvas, NatureId nature)
        {
            const double r = CircleRadii.V1;
            switch (nature)
            {
                case NatureId.Tron:
                    canvas.Circle(0, 0, r);
                    break;
                case NatureId.Vuong:
                    const double h = r * 0.82;
                    canvas.Push(Affine2.Rotate(45));
                    canvas.Polyline(new[] { new Vec2(-h, -h), new Vec2(h, -h), new Vec2(h, h), new Vec2(-h, h) }, true);
                    canvas.Pop();
                    break;
                default:
                    PolarPath(canvas, 120, a => r + 2.6 * Math.Sin(6 * a));
                    break;
            }
        }

        /// <summary>Vòng 2: a magic pattern band, never a plain circle (V2_RULE_001).</summary>
        public static void Pattern(ArtCanvas canvas, PatternId pattern)
        {
            const double r = CircleRadii.V2;
            switch (pattern)
            {
                case PatternId.Nhon:
                    PolarPath(canvas, 48, a => Math.Round(a / (Math.PI * 2) * 48, MidpointRounding.AwayFromZero) % 2 == 0 ? r - 3 : r + 3);
                    break;
                case PatternId.Vuong:
                    const int teeth = 16;
                    PolarPath(canvas, teeth * 8, a => a / (Math.PI * 2) * teeth % 1 < 0.5 ? r + 2.5 : r - 2.5);
                    break;
                default:
                    canvas.Circle(0, 0, r - 3.5);
                    canvas.Circle(0, 0, r + 3.5);
                    for (var i = 0; i < 16; i++)
                    {
                        var a = i / 16.0 * Math.PI * 2;
                        canvas.Circle(r * Math.Cos(a), r * Math.Sin(a), 2);
                    }
                    break;
            }
        }

        /// <summary>An empty slot: a faint dashed circle so the player sees where the layer goes.</summary>
        public static void Placeholder(ArtCanvas canvas, double radius)
        {
            var style = canvas.Style;
            canvas.Style = new StrokeStyle { Width = 0.8, Opacity = 0.35, DashOn = 2, DashOff = 3 };
            canvas.Circle(0, 0, radius);
            canvas.Style = style;
        }
    }
}
