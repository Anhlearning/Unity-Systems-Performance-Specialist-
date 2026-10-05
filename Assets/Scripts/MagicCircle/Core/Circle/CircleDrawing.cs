// CircleDrawing: circle art as plain vector data (strokes and filled dots),
// the Unity stand-in for the web build's SVG document. ArtCanvas builds one
// the way SVG groups do, with a transform stack and an inherited stroke style.
// CircleRaster turns it into pixels.

using System;
using System.Collections.Generic;

namespace MagicCircleSim.Circle
{
    /// <summary>A square viewBox in SVG user units.</summary>
    public readonly struct ViewBox
    {
        public readonly double MinX, MinY, Size;

        public ViewBox(double minX, double minY, double size)
        {
            MinX = minX;
            MinY = minY;
            Size = size;
        }

        /// <summary>A box centered on the origin, reaching out to <paramref name="half"/>.</summary>
        public static ViewBox Centered(double half) => new ViewBox(-half, -half, 2 * half);
    }

    public struct StrokeStyle
    {
        /// <summary>Stroke width in the current group's local units, like CSS stroke-width.</summary>
        public double Width;
        public double Opacity;
        /// <summary>Dash and gap lengths, local units. Zero dash means a solid line.</summary>
        public double DashOn, DashOff;

        public static StrokeStyle Solid(double width) => new StrokeStyle { Width = width, Opacity = 1 };
    }

    public sealed class Stroke
    {
        public readonly Vec2[] Points;
        public readonly bool Closed;
        /// <summary>Width and dashes in drawing units, after the group transforms.</summary>
        public readonly double Width, Opacity, DashOn, DashOff;

        public Stroke(Vec2[] points, bool closed, double width, double opacity, double dashOn, double dashOff)
        {
            Points = points;
            Closed = closed;
            Width = width;
            Opacity = opacity;
            DashOn = dashOn;
            DashOff = dashOff;
        }

        public bool Dashed => DashOn > 0;
    }

    public readonly struct Disk
    {
        public readonly Vec2 Center;
        public readonly double Radius, Opacity;

        public Disk(Vec2 center, double radius, double opacity)
        {
            Center = center;
            Radius = radius;
            Opacity = opacity;
        }
    }

    /// <summary>Where a placed rune sits, so the editor can hit-test it like the web build's data-rune-id hooks.</summary>
    public readonly struct RuneSlot
    {
        public readonly string RuneId;
        public readonly Vec2 Center;
        public readonly double HitRadius;

        public RuneSlot(string runeId, Vec2 center, double hitRadius)
        {
            RuneId = runeId;
            Center = center;
            HitRadius = hitRadius;
        }
    }

    public sealed class CircleDrawing
    {
        public readonly ViewBox Box;
        public readonly List<Stroke> Strokes = new List<Stroke>();
        public readonly List<Disk> Disks = new List<Disk>();
        public readonly List<RuneSlot> RuneSlots = new List<RuneSlot>();
        /// <summary>Hex color the whole drawing is inked in.</summary>
        public string Ink;

        public CircleDrawing(ViewBox box, string ink)
        {
            Box = box;
            Ink = ink;
        }

        /// <summary>The placed rune under a point in drawing units, or null.</summary>
        public string RuneAt(Vec2 point)
        {
            foreach (var slot in RuneSlots)
                if ((point - slot.Center).Length <= slot.HitRadius) return slot.RuneId;
            return null;
        }
    }

    public sealed class ArtCanvas
    {
        /// <summary>A circle's flattening resolution, in segments per viewBox width of circumference.</summary>
        const double CircleSegmentsPerBox = 400;

        public readonly CircleDrawing Drawing;
        public StrokeStyle Style;
        readonly Stack<(Affine2 transform, StrokeStyle style)> saved = new Stack<(Affine2, StrokeStyle)>();
        Affine2 transform = Affine2.Identity;

        public ArtCanvas(ViewBox box, string ink, StrokeStyle style)
        {
            Drawing = new CircleDrawing(box, ink);
            Style = style;
        }

        public Affine2 Transform => transform;

        /// <summary>Opens a group, like &lt;g transform="..."&gt;. Pair with <see cref="Pop"/>.</summary>
        public void Push(Affine2 groupTransform)
        {
            saved.Push((transform, Style));
            transform = transform * groupTransform;
        }

        public void Pop() => (transform, Style) = saved.Pop();

        public void Polyline(IReadOnlyList<Vec2> points, bool closed)
        {
            if (points.Count < 2) return;
            var world = new Vec2[points.Count];
            for (var i = 0; i < points.Count; i++) world[i] = transform.Apply(points[i]);
            var k = transform.LengthScale;
            Drawing.Strokes.Add(new Stroke(world, closed, Style.Width * k, Style.Opacity, Style.DashOn * k, Style.DashOff * k));
        }

        public void Path(string d)
        {
            foreach (var sub in SvgPath.Flatten(d)) Polyline(sub.Points, sub.Closed);
        }

        public void Circle(double cx, double cy, double r)
        {
            var worldR = r * transform.LengthScale;
            var segments = Math.Max(24, (int)Math.Ceiling(2 * Math.PI * worldR / Drawing.Box.Size * CircleSegmentsPerBox));
            var points = new Vec2[segments];
            for (var i = 0; i < segments; i++)
            {
                var a = i * 2 * Math.PI / segments;
                points[i] = new Vec2(cx + r * Math.Cos(a), cy + r * Math.Sin(a));
            }
            Polyline(points, true);
        }

        /// <summary>A filled dot, the art's <c>class="fill"</c>.</summary>
        public void Dot(double x, double y, double r)
        {
            Drawing.Disks.Add(new Disk(transform.Apply(new Vec2(x, y)), r * transform.LengthScale, Style.Opacity));
        }

        public void MarkRune(string runeId, double localHitRadius)
        {
            Drawing.RuneSlots.Add(new RuneSlot(runeId, transform.Apply(new Vec2(0, 0)), localHitRadius * transform.LengthScale));
        }
    }
}
