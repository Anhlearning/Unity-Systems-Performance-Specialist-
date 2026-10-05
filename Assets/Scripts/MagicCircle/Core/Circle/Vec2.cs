// 2D math for the circle art, in SVG user units: x right, y down.

using System;

namespace MagicCircleSim.Circle
{
    public readonly struct Vec2
    {
        public readonly double X;
        public readonly double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double k) => new Vec2(a.X * k, a.Y * k);

        public double Length => Math.Sqrt(X * X + Y * Y);

        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>An SVG transform matrix: x' = a·x + c·y + e, y' = b·x + d·y + f.</summary>
    public readonly struct Affine2
    {
        public readonly double A, B, C, D, E, F;

        public Affine2(double a, double b, double c, double d, double e, double f)
        {
            A = a; B = b; C = c; D = d; E = e; F = f;
        }

        public static readonly Affine2 Identity = new Affine2(1, 0, 0, 1, 0, 0);

        public static Affine2 Translate(double x, double y) => new Affine2(1, 0, 0, 1, x, y);

        public static Affine2 Scale(double s) => new Affine2(s, 0, 0, s, 0, 0);

        /// <summary>SVG's rotate(deg): clockwise on screen, since y points down.</summary>
        public static Affine2 Rotate(double degrees)
        {
            var r = degrees * Math.PI / 180;
            double cos = Math.Cos(r), sin = Math.Sin(r);
            return new Affine2(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>Composes like an SVG transform list: <c>p * q</c> applies q first.</summary>
        public static Affine2 operator *(Affine2 p, Affine2 q) => new Affine2(
            p.A * q.A + p.C * q.B,
            p.B * q.A + p.D * q.B,
            p.A * q.C + p.C * q.D,
            p.B * q.C + p.D * q.D,
            p.A * q.E + p.C * q.F + p.E,
            p.B * q.E + p.D * q.F + p.F);

        public Vec2 Apply(Vec2 p) => new Vec2(A * p.X + C * p.Y + E, B * p.X + D * p.Y + F);

        /// <summary>How much lengths grow under this transform. The art only uses uniform scales.</summary>
        public double LengthScale => Math.Sqrt(Math.Abs(A * D - B * C));
    }
}
