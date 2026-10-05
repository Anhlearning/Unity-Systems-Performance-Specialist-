// The cast clock. Every cast runs four beats: the circle charges, the
// instances manifest, they fly for the spell's lifetime, then they fade. Only
// the flight beat stretches with Vòng 4's lifetime (A02).

using System;

namespace MagicCircleSim.Cast
{
    public enum Beat { Charge, Manifest, Flight, Fade, Done }

    public readonly struct Envelope
    {
        public readonly Beat Beat;
        /// <summary>Seconds since the instances started moving. Zero until the flight beat.</summary>
        public readonly double FlightS;
        public readonly double Scale;
        public readonly double Alpha;

        public Envelope(Beat beat, double flightS, double scale, double alpha)
        {
            Beat = beat;
            FlightS = flightS;
            Scale = scale;
            Alpha = alpha;
        }
    }

    public static class CastTimeline
    {
        public const double ChargeS = 0.6;
        public const double ManifestS = 0.35;
        public const double FadeS = 0.5;
        /// <summary>Particles already in the air get this long to die out after the last instance.</summary>
        public const double TailS = 1.2;

        static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);

        public static double Duration(double lifetimeS) => ChargeS + ManifestS + lifetimeS + FadeS + TailS;

        public static Envelope At(double tS, double lifetimeS)
        {
            if (tS < ChargeS) return new Envelope(Beat.Charge, 0, 0, 0);
            var m = tS - ChargeS;
            if (m < ManifestS)
            {
                var k = EaseOut(m / ManifestS);
                return new Envelope(Beat.Manifest, 0, k, k);
            }
            var flightS = m - ManifestS;
            if (flightS < lifetimeS) return new Envelope(Beat.Flight, flightS, 1, 1);
            var g = flightS - lifetimeS;
            if (g < FadeS) return new Envelope(Beat.Fade, flightS, 1 + 0.4 * (g / FadeS), 1 - g / FadeS);
            return new Envelope(Beat.Done, flightS, 0, 0);
        }

        /// <summary>How brightly the circle on the ground glows: it charges up, holds, then dims with the spell.</summary>
        public static double CircleGlowAt(double tS, double lifetimeS)
        {
            if (tS < ChargeS) return 0.25 + 0.75 * (tS / ChargeS);
            var after = tS - ChargeS;
            var holdUntil = ManifestS + lifetimeS;
            if (after < holdUntil) return 1 - 0.45 * Math.Min(after / 0.6, 1);
            return Math.Max(0.55 * (1 - (after - holdUntil) / FadeS), 0.25);
        }

        /// <summary>The Vòng 3 area outline shows while the circle charges and fades as the instances manifest.</summary>
        public static double OutlineOpacityAt(double tS)
        {
            var end = ChargeS + ManifestS;
            if (tS < ChargeS) return 0.8 * (tS / ChargeS);
            return Math.Max(0.8 * (1 - (tS - ChargeS) / (end - ChargeS + 0.6)), 0);
        }
    }
}
