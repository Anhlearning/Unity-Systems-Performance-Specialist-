// CircleRaster: a CircleDrawing as an anti-aliased coverage mask, one float per
// pixel, rows bottom-up the way a Texture2D stores them. The view uploads it as
// an alpha texture and tints it with the drawing's ink. Pure and allocation-free
// once the caller owns the buffer, so redrawing on every edit stays cheap.

using System;

namespace MagicCircleSim.Circle
{
    public static class CircleRaster
    {
        /// <summary>Clears <paramref name="coverage"/> (size × size) and draws into it.</summary>
        public static void Rasterize(CircleDrawing drawing, int size, float[] coverage)
        {
            if (coverage.Length < size * size) throw new ArgumentException("Coverage buffer is smaller than size × size.");
            Array.Clear(coverage, 0, size * size);
            var map = new PixelMap(drawing.Box, size);
            foreach (var stroke in drawing.Strokes) DrawStroke(stroke, map, size, coverage);
            foreach (var disk in drawing.Disks) DrawDisk(disk, map, size, coverage);
        }

        /// <summary>Drawing units to pixels, y flipped so the drawing's top lands on the texture's top row.</summary>
        readonly struct PixelMap
        {
            readonly ViewBox box;
            public readonly double PixelsPerUnit;

            public PixelMap(ViewBox box, int size)
            {
                this.box = box;
                PixelsPerUnit = size / box.Size;
            }

            public Vec2 ToPixel(Vec2 p) => new Vec2((p.X - box.MinX) * PixelsPerUnit, (box.MinY + box.Size - p.Y) * PixelsPerUnit);
        }

        static void DrawStroke(Stroke stroke, PixelMap map, int size, float[] coverage)
        {
            var halfWidth = stroke.Width * map.PixelsPerUnit / 2;
            var count = stroke.Closed ? stroke.Points.Length : stroke.Points.Length - 1;
            // Dash state runs across the whole stroke, like stroke-dasharray.
            var dashOn = stroke.DashOn * map.PixelsPerUnit;
            var period = dashOn + stroke.DashOff * map.PixelsPerUnit;
            var travelled = 0.0;
            for (var i = 0; i < count; i++)
            {
                var a = map.ToPixel(stroke.Points[i]);
                var b = map.ToPixel(stroke.Points[(i + 1) % stroke.Points.Length]);
                var length = (b - a).Length;
                if (!stroke.Dashed) Segment(a, b, halfWidth, stroke.Opacity, size, coverage);
                else DashedSegment(a, b, length, travelled, dashOn, period, halfWidth, stroke.Opacity, size, coverage);
                travelled += length;
            }
        }

        static void DashedSegment(Vec2 a, Vec2 b, double length, double startAt, double dashOn, double period,
            double halfWidth, double opacity, int size, float[] coverage)
        {
            if (length <= 0) return;
            var t = 0.0;
            while (t < length)
            {
                var phase = (startAt + t) % period;
                var run = phase < dashOn ? dashOn - phase : period - phase;
                var end = Math.Min(t + run, length);
                if (phase < dashOn)
                {
                    Segment(a + (b - a) * (t / length), a + (b - a) * (end / length), halfWidth, opacity, size, coverage);
                }
                t = end;
            }
        }

        /// <summary>A round-capped segment. Joins come out round because neighbours overlap.</summary>
        static void Segment(Vec2 a, Vec2 b, double halfWidth, double opacity, int size, float[] coverage)
        {
            var reach = halfWidth + 1;
            var x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.X, b.X) - reach));
            var x1 = Math.Min(size - 1, (int)Math.Ceiling(Math.Max(a.X, b.X) + reach));
            var y0 = Math.Max(0, (int)Math.Floor(Math.Min(a.Y, b.Y) - reach));
            var y1 = Math.Min(size - 1, (int)Math.Ceiling(Math.Max(a.Y, b.Y) + reach));
            double dx = b.X - a.X, dy = b.Y - a.Y;
            var lengthSq = dx * dx + dy * dy;
            // Hairlines thinner than a pixel fade instead of vanishing.
            var thin = Math.Min(1, 2 * halfWidth);

            for (var y = y0; y <= y1; y++)
            {
                var py = y + 0.5;
                for (var x = x0; x <= x1; x++)
                {
                    var px = x + 0.5;
                    var t = lengthSq > 0 ? ((px - a.X) * dx + (py - a.Y) * dy) / lengthSq : 0;
                    t = t < 0 ? 0 : t > 1 ? 1 : t;
                    double ex = px - (a.X + t * dx), ey = py - (a.Y + t * dy);
                    var distance = Math.Sqrt(ex * ex + ey * ey);
                    Blend(coverage, y * size + x, Coverage(halfWidth - distance) * thin * opacity);
                }
            }
        }

        static void DrawDisk(Disk disk, PixelMap map, int size, float[] coverage)
        {
            var c = map.ToPixel(disk.Center);
            var r = disk.Radius * map.PixelsPerUnit;
            var x0 = Math.Max(0, (int)Math.Floor(c.X - r - 1));
            var x1 = Math.Min(size - 1, (int)Math.Ceiling(c.X + r + 1));
            var y0 = Math.Max(0, (int)Math.Floor(c.Y - r - 1));
            var y1 = Math.Min(size - 1, (int)Math.Ceiling(c.Y + r + 1));
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    double ex = x + 0.5 - c.X, ey = y + 0.5 - c.Y;
                    Blend(coverage, y * size + x, Coverage(r - Math.Sqrt(ex * ex + ey * ey)) * disk.Opacity);
                }
            }
        }

        /// <summary>One pixel of anti-aliasing at the edge.</summary>
        static double Coverage(double inside) => inside + 0.5 <= 0 ? 0 : inside + 0.5 >= 1 ? 1 : inside + 0.5;

        /// <summary>Max, not "over", so overlapping segments of one stroke never darken their joints.</summary>
        static void Blend(float[] coverage, int index, double value)
        {
            if (value > coverage[index]) coverage[index] = (float)value;
        }
    }
}
