// SpawnLayout: where each instance appears, from Vòng 3's shape and Vòng 4's
// distribution. Pure and seeded.
//
// Caster frame: origin at the caster, x to the caster's right, z forward, on
// the ground plane. Azimuth is measured from forward toward the right.
//
//   SpawnLayout.Compute(new SpellShape(FormValue.Ring, 4, 0.5, 7), DistributionValue.Even, seed);

using System;
using System.Collections.Generic;
using MagicCircleSim.Design;

namespace MagicCircleSim.Compiler
{
    public readonly struct SpawnPoint
    {
        public readonly double X;
        public readonly double Z;

        public SpawnPoint(double x, double z)
        {
            X = x;
            Z = z;
        }

        public override string ToString() => $"({X}, {Z})";
    }

    public static class SpawnLayout
    {
        static readonly double GoldenAngle = Math.PI * (3 - Math.Sqrt(5));

        static SpawnPoint Polar(double r, double azimuth) => new SpawnPoint(r * Math.Sin(azimuth), r * Math.Cos(azimuth));

        public static SpawnPoint[] Compute(SpellShape shape, DistributionValue distribution, uint seed)
        {
            var rng = new Mulberry32(seed);
            switch (distribution)
            {
                case DistributionValue.Even:
                    return EvenPoints(shape);
                case DistributionValue.Symmetric:
                    return SymmetricPoints(shape, rng);
                default:
                    var points = new SpawnPoint[shape.Count];
                    for (var i = 0; i < points.Length; i++) points[i] = RandomPoint(shape, rng, false);
                    return points;
            }
        }

        /// <summary>Half the sector's opening, radians. A11: only round forms open a sector.</summary>
        static double HalfAngle(SpellShape shape)
        {
            var round = shape.Form == FormValue.Disk || shape.Form == FormValue.Ring;
            return Math.PI * (round ? Math.Min(Math.Max(shape.AngleFraction, 0), 1) : 1);
        }

        static SpawnPoint[] EvenPoints(SpellShape shape)
        {
            int n = shape.Count;
            var r = shape.Radius;
            var half = HalfAngle(shape);
            var full = half >= Math.PI;
            var points = new SpawnPoint[n];
            for (var i = 0; i < n; i++)
            {
                switch (shape.Form)
                {
                    case FormValue.Ring:
                        // A full ring starts on the forward axis. An arc centers each point in its slice.
                        points[i] = Polar(r, full ? (double)i / n * 2 * Math.PI : -half + (i + 0.5) / n * 2 * half);
                        break;
                    case FormValue.Disk:
                    {
                        if (n == 1)
                        {
                            points[i] = new SpawnPoint(0, 0);
                            break;
                        }
                        // Vogel's sunflower: even density inside the disk, folded into the sector.
                        var az = full ? i * GoldenAngle : -half + i * GoldenAngle / (Math.PI * 2) % 1 * 2 * half;
                        points[i] = Polar(r * Math.Sqrt((i + 0.5) / n), az);
                        break;
                    }
                    case FormValue.Square:
                    {
                        var side = (int)Math.Ceiling(Math.Sqrt(n));
                        var row = i / side;
                        var inRow = Math.Min(side, n - row * side);
                        var cell = r / side;
                        var x = (i % side - (inRow - 1) / 2.0) * cell;
                        var z = ((side - 1) / 2.0 - row) * cell;
                        points[i] = new SpawnPoint(x, z);
                        break;
                    }
                    default:
                        points[i] = new SpawnPoint(n == 1 ? 0 : -r + (i + 0.5) / n * 2 * r, 0);
                        break;
                }
            }
            return points;
        }

        /// <summary>One point anywhere in the shape. <paramref name="rightHalfOnly"/> serves the symmetric layout.</summary>
        static SpawnPoint RandomPoint(SpellShape shape, Mulberry32 rng, bool rightHalfOnly)
        {
            var half = HalfAngle(shape);
            // Drawn for every form, so each form consumes the generator the same way.
            var azimuth = rightHalfOnly ? rng.Next() * half : -half + rng.Next() * 2 * half;
            var r = shape.Radius;
            switch (shape.Form)
            {
                case FormValue.Ring:
                    return Polar(r, azimuth);
                case FormValue.Disk:
                    return Polar(r * Math.Sqrt(rng.Next()), azimuth);
                case FormValue.Square:
                {
                    var x = rightHalfOnly ? rng.Next() * (r / 2) : (rng.Next() - 0.5) * r;
                    var z = (rng.Next() - 0.5) * r;
                    return new SpawnPoint(x, z);
                }
                default:
                    return new SpawnPoint(rightHalfOnly ? rng.Next() * r : (rng.Next() - 0.5) * 2 * r, 0);
            }
        }

        /// <summary>A12: mirrored pairs across the forward axis, plus one on the axis when odd.</summary>
        static SpawnPoint[] SymmetricPoints(SpellShape shape, Mulberry32 rng)
        {
            var points = new List<SpawnPoint>(shape.Count);
            if (shape.Count % 2 == 1)
            {
                var onAxis = shape.Form == FormValue.Ring ? Polar(shape.Radius, 0) : new SpawnPoint(0, 0);
                if (shape.Form == FormValue.Disk || shape.Form == FormValue.Square)
                {
                    onAxis = new SpawnPoint(onAxis.X, (rng.Next() - 0.5) * shape.Radius);
                }
                points.Add(onAxis);
            }
            for (var i = 0; i < shape.Count / 2; i++)
            {
                var p = RandomPoint(shape, rng, true);
                points.Add(p);
                points.Add(new SpawnPoint(-p.X, p.Z));
            }
            return points.ToArray();
        }
    }
}
