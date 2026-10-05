// Bodies: the macroscopic form of a phenomenon, for the materials whose
// identity is a shape rather than a spray of matter. A whirlwind is one funnel
// of wind coiling around a bending axis, not puffs of air rising; a sound is
// wavefronts spreading from their source, not floating rings. Particles cannot
// draw that, so a body is drawn as strokes: 3D polylines with a width, an alpha
// and a tone, which the view turns into camera-facing ribbons.
//
// Pure and seeded. A body is a function of the cast clock, so a fresh replay
// and an incremental one draw the same strokes. It reads only its spec and its
// source, never a rune: where and how many sources there are, and how big, is
// already settled by SpellPerformance from Vòng 3 and Vòng 4.

using System;

namespace MagicCircleSim.Cast
{
    public enum BodyKind { None, Funnel, Gust, Waves }

    /// <summary>One row's body. Sizes are for one source at scale 1; a filled area scales them up.</summary>
    public sealed class BodySpec
    {
        public static readonly BodySpec None = new BodySpec(BodyKind.None, 0, 0, 0, 0, 0, 0, 0, false, null);

        public readonly BodyKind Kind;
        /// <summary>Meters. A funnel's height, a gust's band of heights.</summary>
        public readonly double Height;
        /// <summary>Meters. A funnel's radius at the ground, a gust's inner radius.</summary>
        public readonly double BaseRadius;
        /// <summary>Meters. A funnel's radius at the top, a gust's outer radius, a wave's reach.</summary>
        public readonly double TopRadius;
        /// <summary>A funnel's spin (rad/s), a gust's stream speed (m/s), a wavefront's speed (m/s).</summary>
        public readonly double Speed;
        /// <summary>A funnel's strokes, a gust's streaks, a wave's rings per pulse.</summary>
        public readonly int Strands;
        /// <summary>Seconds. A gust streak's cycle, the gap between sound pulses.</summary>
        public readonly double Period;
        /// <summary>Meters. A stroke at its widest.</summary>
        public readonly double Width;
        public readonly bool Additive;
        /// <summary>The stroke texture, by name: "brush" for painted wind, "line" for a clean front.</summary>
        public readonly string Texture;

        BodySpec(BodyKind kind, double height, double baseRadius, double topRadius, double speed, int strands, double period, double width,
            bool additive, string texture)
        {
            Kind = kind;
            Height = height;
            BaseRadius = baseRadius;
            TopRadius = topRadius;
            Speed = speed;
            Strands = strands;
            Period = period;
            Width = width;
            Additive = additive;
            Texture = texture;
        }

        /// <summary>A whirlwind: strokes coiling around an axis that narrows to the ground and sways as it spins.</summary>
        public static BodySpec Funnel(double height, double baseRadius, double topRadius, double spin, int strands, double width) =>
            new BodySpec(BodyKind.Funnel, height, baseRadius, topRadius, spin, strands, 0, width, false, "brush");

        /// <summary>A wind: streaks streaming around their source between two radii, some curling at the head.</summary>
        public static BodySpec Gust(double height, double innerRadius, double outerRadius, double speed, int streaks, double cycleS, double width) =>
            new BodySpec(BodyKind.Gust, height, innerRadius, outerRadius, speed, streaks, cycleS, width, false, "brush");

        /// <summary>A sound: wavefronts leaving the source every period and fading as they reach their range.</summary>
        public static BodySpec Waves(double reach, double speed, double periodS, int rings, double width) =>
            new BodySpec(BodyKind.Waves, 0, 0, reach, speed, rings, periodS, width, true, "line");

        /// <summary>How many stroke points one source can need, so the buffer is sized once.</summary>
        public int PointsPerSource
        {
            get
            {
                switch (Kind)
                {
                    case BodyKind.Funnel: return (Strands + Strands / 4 + 1) * BodyStrokes.FunnelPoints;
                    case BodyKind.Gust: return Strands * BodyStrokes.GustPoints;
                    case BodyKind.Waves:
                        var pulses = (int)Math.Ceiling(TopRadius / Speed / Period) + 1;
                        return pulses * Strands * (BodyStrokes.RingPoints + 1);
                    default: return 0;
                }
            }
        }
    }

    /// <summary>Where one body stands this instant, and how present it is.</summary>
    public readonly struct BodySource
    {
        public readonly double X, Y, Z;
        /// <summary>Multiplies every size in the spec.</summary>
        public readonly double Size;
        public readonly double Alpha;
        /// <summary>Seeds this source's strokes, so seven whirlwinds never coil alike.</summary>
        public readonly int Index;
        /// <summary>The source floats (a Vòng 2 core holds it up) instead of standing on the ground.</summary>
        public readonly bool Aloft;

        public BodySource(double x, double y, double z, double size, double alpha, int index, bool aloft)
        {
            X = x;
            Y = y;
            Z = z;
            Size = size;
            Alpha = alpha;
            Index = index;
            Aloft = aloft;
        }
    }

    /// <summary>
    /// Polylines with a width and an alpha per point and a tone per stroke, in
    /// fixed arrays sized once, so building a frame's strokes allocates nothing.
    /// </summary>
    public sealed class StrokeBuffer
    {
        public readonly int Capacity;
        /// <summary>x, y, z per point, caster frame.</summary>
        public readonly float[] Points;
        public readonly float[] Widths;
        public readonly float[] Alphas;
        /// <summary>Per stroke: its first point, its point count and its gray tone, 0..1.</summary>
        public readonly int[] Starts, Counts;
        public readonly float[] Tones;
        public int StrokeCount { get; private set; }
        public int PointCount { get; private set; }
        int open = -1;

        public StrokeBuffer(int pointCapacity)
        {
            Capacity = Math.Max(pointCapacity, 2);
            Points = new float[Capacity * 3];
            Widths = new float[Capacity];
            Alphas = new float[Capacity];
            Starts = new int[Capacity / 2];
            Counts = new int[Capacity / 2];
            Tones = new float[Capacity / 2];
        }

        public void Clear()
        {
            StrokeCount = 0;
            PointCount = 0;
            open = -1;
        }

        public void Begin(double tone)
        {
            if (StrokeCount >= Starts.Length) return;
            open = StrokeCount;
            Starts[open] = PointCount;
            Counts[open] = 0;
            Tones[open] = (float)tone;
        }

        public void Add(double x, double y, double z, double width, double alpha)
        {
            if (open < 0 || PointCount >= Capacity) return;
            var p = PointCount * 3;
            Points[p] = (float)x;
            Points[p + 1] = (float)y;
            Points[p + 2] = (float)z;
            Widths[PointCount] = (float)width;
            Alphas[PointCount] = (float)alpha;
            PointCount++;
            Counts[open]++;
        }

        /// <summary>Closes the stroke. One with fewer than two points is dropped.</summary>
        public void End()
        {
            if (open < 0) return;
            if (Counts[open] >= 2) StrokeCount++;
            else PointCount = Starts[open];
            open = -1;
        }
    }

    public static class BodyStrokes
    {
        public const int FunnelPoints = 40;
        public const int GustPoints = 36;
        public const int RingPoints = 128;
        /// <summary>Waves leave the ground this high, so the ground ring is not buried in the circle.</summary>
        const double WaveFloorY = 0.04;

        /// <summary>A stable 0..1 value for (seed, source, stroke, salt), so strokes keep their character.</summary>
        static double Hash(uint seed, int source, int stroke, int salt)
        {
            unchecked
            {
                var h = seed ^ (uint)(source * 0x27d4eb2d) ^ (uint)(stroke * 0x165667b1) ^ (uint)(salt * 0x9e3779b9);
                h ^= h >> 15;
                h *= 0x2c1b3c6d;
                h ^= h >> 12;
                h *= 0x297a2d39;
                h ^= h >> 15;
                return h / 4294967296.0;
            }
        }

        public static void Build(BodySpec spec, in BodySource source, double tS, double lifetimeS, uint seed, StrokeBuffer into)
        {
            if (source.Alpha <= 0 && spec.Kind != BodyKind.Waves) return;
            switch (spec.Kind)
            {
                case BodyKind.Funnel:
                    Funnel(spec, source, tS, seed, into);
                    break;
                case BodyKind.Gust:
                    Gust(spec, source, tS, seed, into);
                    break;
                case BodyKind.Waves:
                    Waves(spec, source, tS, lifetimeS, into);
                    break;
            }
        }

        /// <summary>
        /// The whirlwind: stacked partial loops whose radius flares with height, each a
        /// brush stroke turning about an axis that bends like a rope. The foot spins
        /// fastest; a few loose outer sweeps wrap the body.
        /// </summary>
        static void Funnel(BodySpec spec, in BodySource s, double t, uint seed, StrokeBuffer into)
        {
            var size = s.Size;
            var height = spec.Height * size;
            var r0 = spec.BaseRadius * size;
            var r1 = spec.TopRadius * size;
            var sway = 0.18 * height;
            var outer = spec.Strands / 4;
            var index = s.Index;
            for (var k = 0; k < spec.Strands + outer; k++)
            {
                var sweep = k >= spec.Strands;
                double H(int salt) => Hash(seed, index, k, salt);
                // Height fraction. More strokes crowd the wide top, as a funnel's mass does.
                var f = sweep ? 0.3 + 0.6 * H(1) : Math.Pow((k + H(1)) / spec.Strands, 0.8);
                var y0 = height * f;
                var radius = (r0 + (r1 - r0) * Math.Pow(f, 1.9)) * (sweep ? 1.3 + 0.4 * H(2) : 0.85 + 0.3 * H(2));
                var spin = spec.Speed * (1.5 - 0.8 * f) * (sweep ? 0.7 : 1);
                var a0 = H(3) * Math.PI * 2 + spin * t;
                var span = (sweep ? 1.1 : 1.25) * Math.PI + 0.75 * Math.PI * H(4) + 0.2 * Math.PI * Math.Sin(t * 1.3 + k);
                var rise = (sweep ? -0.25 : 0.06 + 0.1 * H(5)) * height * (0.5 + 0.5 * f);
                var width = spec.Width * size * (0.3 + 0.9 * f) * (sweep ? 0.4 : 0.75 + 0.5 * H(7));
                var alpha = s.Alpha * (0.5 + 0.5 * H(8)) * (sweep ? 0.7 : 1);
                into.Begin(0.4 + 0.6 * H(6));
                for (var i = 0; i < FunnelPoints; i++)
                {
                    var u = (double)i / (FunnelPoints - 1);
                    var angle = a0 + span * u;
                    var y = Math.Max(0.02, y0 + rise * (u - 0.5));
                    var up = Math.Min(y / Math.Max(height, 1e-6), 1);
                    // The axis bends more the higher it goes, and the bend travels.
                    var cx = s.X + sway * Math.Pow(up, 1.5) * Math.Sin(1.1 * t + up * 2.6 + s.Index);
                    var cz = s.Z + sway * Math.Pow(up, 1.5) * Math.Cos(0.8 * t + up * 2.1 + s.Index);
                    var r = radius * (1 + 0.07 * Math.Sin(angle * 2 + k));
                    var taper = Math.Sin(Math.PI * u);
                    into.Add(cx + r * Math.Sin(angle), y, cz + r * Math.Cos(angle), width * Math.Pow(taper, 0.7), alpha * Math.Sqrt(taper));
                }
                into.End();
            }
        }

        /// <summary>
        /// The wind: streaks streaming around the source at many radii and heights, each
        /// living one cycle of its own, rippling as it goes; some curl over at the head.
        /// </summary>
        static void Gust(BodySpec spec, in BodySource s, double t, uint seed, StrokeBuffer into)
        {
            var size = s.Size;
            var floor = s.Aloft ? s.Y - spec.Height * size / 2 : 0;
            var index = s.Index;
            for (var k = 0; k < spec.Strands; k++)
            {
                var period = spec.Period * (0.7 + 0.6 * Hash(seed, s.Index, k, 1));
                var clock = t / period + Hash(seed, s.Index, k, 2);
                var cycle = (int)Math.Floor(clock);
                var u = clock - cycle;
                double H(int salt) => Hash(seed, index, k * 131 + cycle, salt);
                var radius = (spec.BaseRadius + (spec.TopRadius - spec.BaseRadius) * H(3)) * size;
                var y0 = floor + (0.15 + spec.Height * H(4)) * size;
                var sense = H(5) < 0.85 ? 1 : -1;
                var length = (2 + 2.5 * H(6)) * size / radius;              // radians of arc the streak covers
                var head = H(7) * Math.PI * 2 + sense * spec.Speed * u * period / radius;
                var curl = H(8) < 0.25;
                var life = Math.Sin(Math.PI * u);
                var width = spec.Width * size * (0.6 + 0.6 * H(9));
                into.Begin(0.75 + 0.25 * H(10));
                for (var i = 0; i < GustPoints; i++)
                {
                    var v = (double)i / (GustPoints - 1);                  // 0 at the tail, 1 at the head
                    var angle = head - sense * length * (1 - v);
                    var r = radius;
                    var y = y0 + 0.16 * size * Math.Sin(angle * 3 + t * 2.2 + k);
                    if (curl && v > 0.82)
                    {
                        // The head rolls over into a small loop, the way a gust line curls.
                        var q = (v - 0.82) / 0.18;
                        var loop = 0.2 * size * (1 - 0.4 * q);
                        r -= loop * (1 - Math.Cos(q * Math.PI * 2));
                        y += loop * Math.Sin(q * Math.PI * 2);
                    }
                    var taper = Math.Pow(v, 0.7) * Math.Pow(1 - v * 0.85, 0.5);
                    into.Add(s.X + r * Math.Sin(angle), Math.Max(0.03, y), s.Z + r * Math.Cos(angle), width * taper, s.Alpha * life * taper);
                }
                into.End();
            }
        }

        /// <summary>
        /// The sound: a pulse every period, its wavefront a shell expanding at the spec's
        /// speed. A shell is drawn as rings at a few latitudes: a dome over the ground for a
        /// source on it, a sphere for one aloft. Each front keeps the strength it left with
        /// and thins as it nears its reach.
        /// </summary>
        static void Waves(BodySpec spec, in BodySource s, double t, double lifetimeS, StrokeBuffer into)
        {
            var reach = spec.TopRadius * s.Size;
            var newest = (int)Math.Floor((t - CastTimeline.ChargeS) / spec.Period);
            var oldest = Math.Max(0, (int)Math.Floor((t - CastTimeline.ChargeS - reach / spec.Speed) / spec.Period));
            var centerY = s.Aloft ? s.Y : WaveFloorY;
            for (var j = oldest; j <= newest; j++)
            {
                var born = CastTimeline.ChargeS + j * spec.Period;
                var radius = spec.Speed * (t - born);
                if (radius < 0.05 || radius > reach) continue;
                var strength = CastTimeline.At(born, lifetimeS).Alpha;
                if (strength <= 0) continue;
                var fall = Math.Pow(1 - radius / reach, 1.3);
                for (var ring = 0; ring < spec.Strands; ring++)
                {
                    // Latitudes: 0, then up the dome; aloft, spread above and below the equator.
                    var lat = s.Aloft
                        ? (ring - (spec.Strands - 1) / 2.0) * 0.55
                        : ring * 0.5;
                    var rr = radius * Math.Cos(lat);
                    var y = centerY + radius * Math.Sin(lat);
                    if (y < WaveFloorY - 1e-6) continue;
                    var alpha = strength * fall * (lat == 0 ? 1 : 0.5);
                    var width = spec.Width * (1 + 0.12 * radius);
                    into.Begin(1);
                    for (var i = 0; i <= RingPoints; i++)
                    {
                        var a = (double)i / RingPoints * Math.PI * 2;
                        var wobble = 1 + 0.006 * Math.Sin(5 * a + 6 * t + j);
                        into.Add(s.X + rr * wobble * Math.Sin(a), y, s.Z + rr * wobble * Math.Cos(a), width, alpha);
                    }
                    into.End();
                }
            }
        }
    }
}
