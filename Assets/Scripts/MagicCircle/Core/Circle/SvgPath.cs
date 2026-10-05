// Flattens SVG path data into polylines, so the glyph art keeps its original
// markup. Supports the absolute commands the art uses: M L H V Q T C A Z, with
// implicit repeats.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace MagicCircleSim.Circle
{
    public sealed class SubPath
    {
        public readonly List<Vec2> Points = new List<Vec2>();
        public bool Closed;
    }

    public static class SvgPath
    {
        const int CurveSegments = 16;
        const double ArcStepRad = Math.PI / 24;

        public static List<SubPath> Flatten(string d)
        {
            var paths = new List<SubPath>();
            var reader = new Reader(d);
            SubPath current = null;
            var pen = new Vec2(0, 0);
            var start = pen;
            var lastControl = pen;
            var command = 'M';
            var lastCommand = ' ';

            while (reader.More())
            {
                if (reader.PeekCommand(out var next)) command = next;
                else if (command == 'M') command = 'L';
                else if (command == 'Z') throw new FormatException($"Number after Z in path: {d}");

                if (command == 'M')
                {
                    pen = start = reader.Point();
                    current = new SubPath();
                    current.Points.Add(pen);
                    paths.Add(current);
                    lastCommand = command;
                    continue;
                }
                if (current == null) throw new FormatException($"Path must start with M: {d}");

                switch (command)
                {
                    case 'L':
                        pen = reader.Point();
                        current.Points.Add(pen);
                        break;
                    case 'H':
                        pen = new Vec2(reader.Number(), pen.Y);
                        current.Points.Add(pen);
                        break;
                    case 'V':
                        pen = new Vec2(pen.X, reader.Number());
                        current.Points.Add(pen);
                        break;
                    case 'Q':
                    {
                        var control = reader.Point();
                        var end = reader.Point();
                        Quadratic(current.Points, pen, control, end);
                        lastControl = control;
                        pen = end;
                        break;
                    }
                    case 'T':
                    {
                        var control = lastCommand == 'Q' || lastCommand == 'T' ? pen + (pen - lastControl) : pen;
                        var end = reader.Point();
                        Quadratic(current.Points, pen, control, end);
                        lastControl = control;
                        pen = end;
                        break;
                    }
                    case 'C':
                    {
                        var c1 = reader.Point();
                        var c2 = reader.Point();
                        var end = reader.Point();
                        Cubic(current.Points, pen, c1, c2, end);
                        lastControl = c2;
                        pen = end;
                        break;
                    }
                    case 'A':
                    {
                        var rx = reader.Number();
                        var ry = reader.Number();
                        var rotation = reader.Number();
                        var largeArc = reader.Number() != 0;
                        var sweep = reader.Number() != 0;
                        var end = reader.Point();
                        Arc(current.Points, pen, rx, ry, rotation, largeArc, sweep, end);
                        pen = end;
                        break;
                    }
                    case 'Z':
                        current.Closed = true;
                        pen = start;
                        break;
                    default:
                        throw new FormatException($"Unsupported path command '{command}' in: {d}");
                }
                lastCommand = command;
            }
            return paths;
        }

        static void Quadratic(List<Vec2> points, Vec2 p0, Vec2 p1, Vec2 p2)
        {
            for (var i = 1; i <= CurveSegments; i++)
            {
                double t = (double)i / CurveSegments, u = 1 - t;
                points.Add(p0 * (u * u) + p1 * (2 * u * t) + p2 * (t * t));
            }
        }

        static void Cubic(List<Vec2> points, Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3)
        {
            for (var i = 1; i <= CurveSegments; i++)
            {
                double t = (double)i / CurveSegments, u = 1 - t;
                points.Add(p0 * (u * u * u) + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * (t * t * t));
            }
        }

        /// <summary>SVG endpoint arc to center parameterization (SVG 1.1, appendix F.6.5).</summary>
        static void Arc(List<Vec2> points, Vec2 p0, double rx, double ry, double rotationDeg, bool largeArc, bool sweep, Vec2 p1)
        {
            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            if (rx < 1e-12 || ry < 1e-12)
            {
                points.Add(p1);
                return;
            }
            var phi = rotationDeg * Math.PI / 180;
            double cos = Math.Cos(phi), sin = Math.Sin(phi);
            double dx = (p0.X - p1.X) / 2, dy = (p0.Y - p1.Y) / 2;
            double x1 = cos * dx + sin * dy, y1 = -sin * dx + cos * dy;

            var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
            if (lambda > 1)
            {
                rx *= Math.Sqrt(lambda);
                ry *= Math.Sqrt(lambda);
            }
            double rx2 = rx * rx, ry2 = ry * ry;
            var den = rx2 * y1 * y1 + ry2 * x1 * x1;
            var coef = den < 1e-24 ? 0 : Math.Sqrt(Math.Max(0, (rx2 * ry2 - den) / den));
            if (largeArc == sweep) coef = -coef;
            double cxp = coef * rx * y1 / ry, cyp = -coef * ry * x1 / rx;
            var cx = cos * cxp - sin * cyp + (p0.X + p1.X) / 2;
            var cy = sin * cxp + cos * cyp + (p0.Y + p1.Y) / 2;

            var theta = Angle(1, 0, (x1 - cxp) / rx, (y1 - cyp) / ry);
            var delta = Angle((x1 - cxp) / rx, (y1 - cyp) / ry, (-x1 - cxp) / rx, (-y1 - cyp) / ry);
            if (!sweep && delta > 0) delta -= 2 * Math.PI;
            else if (sweep && delta < 0) delta += 2 * Math.PI;

            var steps = Math.Max(4, (int)Math.Ceiling(Math.Abs(delta) / ArcStepRad));
            for (var i = 1; i <= steps; i++)
            {
                var t = theta + delta * i / steps;
                double ct = Math.Cos(t), st = Math.Sin(t);
                points.Add(new Vec2(cx + rx * ct * cos - ry * st * sin, cy + rx * ct * sin + ry * st * cos));
            }
        }

        static double Angle(double ux, double uy, double vx, double vy) => Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);

        sealed class Reader
        {
            readonly string text;
            int at;

            public Reader(string text) => this.text = text;

            void SkipSeparators()
            {
                while (at < text.Length && (char.IsWhiteSpace(text[at]) || text[at] == ',')) at++;
            }

            public bool More()
            {
                SkipSeparators();
                return at < text.Length;
            }

            public bool PeekCommand(out char command)
            {
                SkipSeparators();
                command = at < text.Length ? text[at] : ' ';
                if (!char.IsLetter(command) || command == 'e' || command == 'E') return false;
                at++;
                return true;
            }

            public double Number()
            {
                SkipSeparators();
                var begin = at;
                if (at < text.Length && (text[at] == '-' || text[at] == '+')) at++;
                while (at < text.Length && (char.IsDigit(text[at]) || text[at] == '.')) at++;
                if (at < text.Length && (text[at] == 'e' || text[at] == 'E'))
                {
                    at++;
                    if (at < text.Length && (text[at] == '-' || text[at] == '+')) at++;
                    while (at < text.Length && char.IsDigit(text[at])) at++;
                }
                if (begin == at) throw new FormatException($"Expected a number at {begin} in path: {text}");
                return double.Parse(text.Substring(begin, at - begin), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            public Vec2 Point() => new Vec2(Number(), Number());
        }
    }
}
