// The outline of Vòng 3's area on the ground, shown while the circle charges so
// the player sees where the spell will appear. Pure, caster frame.

using System;
using System.Collections.Generic;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;

namespace MagicCircleSim.Cast
{
    public static class RegionOutline
    {
        const int ArcSamples = 64;

        public static List<SpawnPoint> Of(SpellShape shape)
        {
            var r = shape.Radius;
            if (shape.Form == FormValue.Line) return new List<SpawnPoint> { new SpawnPoint(-r, 0), new SpawnPoint(r, 0) };
            if (shape.Form == FormValue.Square)
            {
                var h = r / 2;
                return new List<SpawnPoint>
                {
                    new SpawnPoint(-h, -h), new SpawnPoint(h, -h), new SpawnPoint(h, h), new SpawnPoint(-h, h), new SpawnPoint(-h, -h),
                };
            }
            var half = Math.PI * Math.Min(Math.Max(shape.AngleFraction, 0), 1);
            var outline = new List<SpawnPoint>(ArcSamples + 3);
            // A sector closes through the caster. A full circle is already closed.
            var sector = half < Math.PI;
            if (sector) outline.Add(new SpawnPoint(0, 0));
            for (var i = 0; i <= ArcSamples; i++)
            {
                var a = -half + (double)i / ArcSamples * 2 * half;
                outline.Add(new SpawnPoint(r * Math.Sin(a), r * Math.Cos(a)));
            }
            if (sector) outline.Add(new SpawnPoint(0, 0));
            return outline;
        }
    }
}
