// ParticlePool: a fixed ring buffer of particles stepped on the CPU. Plain
// float arrays and no UnityEngine, so a whole cast runs in an EditMode test and
// the stage uploads the arrays to the GPU as-is. Nothing allocates per step.
//
// Each particle remembers the source it was born from (its anchor). The look's
// pinch, swirl and tear are measured from that anchor, which is what turns a
// cloud of dots into a column of fire or a whirlwind standing on its source.

using System;
using MagicCircleSim.Compiler;

namespace MagicCircleSim.Cast
{
    public sealed class ParticlePool
    {
        const double GroundY = 0.02;
        const double Bounce = 0.3;
        /// <summary>How much faster matter ages above the look's tear height.</summary>
        const double TearRate = 3;

        public readonly int Capacity;
        /// <summary>x, y, z per particle, caster frame.</summary>
        public readonly float[] Positions;
        /// <summary>age / life, 0..1. One means dead, which the shader draws as nothing.</summary>
        public readonly float[] Ratios;
        public readonly float[] Sizes;
        readonly float[] velocities;
        /// <summary>x, y, z of the source each particle was born from.</summary>
        readonly float[] anchors;
        readonly float[] ages;
        readonly float[] lives;
        int next;

        public ParticlePool(int capacity)
        {
            Capacity = capacity;
            Positions = new float[capacity * 3];
            Ratios = Filled(capacity, 1);
            Sizes = new float[capacity];
            velocities = new float[capacity * 3];
            anchors = new float[capacity * 3];
            ages = Filled(capacity, 1);
            lives = Filled(capacity, 1);
        }

        static float[] Filled(int length, float value)
        {
            var array = new float[length];
            for (var i = 0; i < length; i++) array[i] = value;
            return array;
        }

        /// <summary>
        /// Births one particle from a source at (x, y, z), somewhere on its mouth disc,
        /// overwriting the oldest slot once the pool is full.
        /// </summary>
        public void Emit(double x, double y, double z, double mouth, Look look, Mulberry32 rng, double brightness = 1)
        {
            var i = next;
            next = (next + 1) % Capacity;
            var p = i * 3;
            anchors[p] = (float)x;
            anchors[p + 1] = (float)y;
            anchors[p + 2] = (float)z;
            var mouthAngle = rng.Next() * Math.PI * 2;
            var mouthR = mouth * Math.Sqrt(rng.Next());
            Positions[p] = (float)(x + mouthR * Math.Cos(mouthAngle));
            Positions[p + 1] = (float)y;
            Positions[p + 2] = (float)(z + mouthR * Math.Sin(mouthAngle));
            // A random direction in the unit ball, scaled by the look's spread, plus its rise.
            var theta = rng.Next() * Math.PI * 2;
            var u = rng.Next() * 2 - 1;
            var s = Math.Sqrt(1 - u * u) * look.Spread * Math.Cbrt(rng.Next());
            velocities[p] = (float)(s * Math.Cos(theta));
            velocities[p + 1] = (float)(u * look.Spread * 0.6 + look.Rise * (0.5 + rng.Next()));
            velocities[p + 2] = (float)(s * Math.Sin(theta));
            ages[i] = 0;
            lives[i] = (float)(look.Life * (0.6 + 0.8 * rng.Next()));
            Sizes[i] = (float)(look.Size * (0.6 + 0.8 * rng.Next()) * brightness);
            Ratios[i] = 0;
        }

        /// <param name="tS">Cast time, which drives the coherent turbulence field.</param>
        public void Step(double dt, double tS, Look look, Mulberry32 rng)
        {
            var keep = Math.Max(1 - look.Drag * dt, 0);
            var v = velocities;
            for (var i = 0; i < Capacity; i++)
            {
                if (Ratios[i] >= 1) continue;
                var p = i * 3;
                double px = Positions[p], py = Positions[p + 1], pz = Positions[p + 2];
                double vx = v[p], vy = v[p + 1], vz = v[p + 2];
                double dx = px - anchors[p], dz = pz - anchors[p + 2];
                var above = py - anchors[p + 1];
                var dist = Math.Sqrt(dx * dx + dz * dz);
                var up = look.Height > 0 ? Clamp01(above / look.Height) : 0.5;

                // Uncorrelated jitter, drawn for every look so all looks consume the generator alike.
                vx += (rng.Next() - 0.5) * 2 * look.Jitter * dt;
                vy += (rng.Next() - 0.5) * 2 * look.Jitter * dt + look.Lift * dt;
                vz += (rng.Next() - 0.5) * 2 * look.Jitter * dt;

                // The column: matter outside its wall is pulled back onto the source's axis.
                // The wall runs from the mouth to the look's flare at the crown, so a flame
                // tapers to its tip and a whirlwind opens into a funnel.
                if (look.Pinch > 0 && dist > 1e-6)
                {
                    var off = dist - look.Mouth * (1 + (look.Flare - 1) * up);
                    if (off > 0)
                    {
                        var pull = look.Pinch * off * dt / dist;
                        vx -= dx * pull;
                        vz -= dz * pull;
                    }
                }

                // Spin about the source's axis: the tangent (-dz, dx), scaled by the distance.
                if (look.Swirl != 0)
                {
                    vx -= dz * look.Swirl * dt;
                    vz += dx * look.Swirl * dt;
                }

                // A coherent flow field, a smooth function of place and time, so neighbours
                // bend together and the body licks instead of spraying. Stronger up high.
                if (look.Turbulence > 0)
                {
                    var gain = look.Turbulence * dt * (0.5 + up);
                    vx += gain * (Math.Sin(2.3 * py + 4.1 * tS + 1.3 * pz) + 0.5 * Math.Sin(3.4 * pz - 5.3 * tS + 1.7));
                    vz += gain * (Math.Sin(2.6 * py - 3.7 * tS + 1.1 * px + 2.0) + 0.5 * Math.Sin(3.1 * px + 4.7 * tS));
                    vy += gain * 0.4 * Math.Sin(1.9 * (px + pz) + 2.9 * tS);
                }

                vx *= keep;
                vy *= keep;
                vz *= keep;
                px += vx * dt;
                py += vy * dt;
                pz += vz * dt;
                if (py < GroundY)
                {
                    py = GroundY;
                    vy = -vy * Bounce;
                }
                Positions[p] = (float)px;
                Positions[p + 1] = (float)py;
                Positions[p + 2] = (float)pz;
                v[p] = (float)vx;
                v[p + 1] = (float)vy;
                v[p + 2] = (float)vz;

                // Above the tear line the crown melts as it rises instead of standing as a lid.
                var ageRate = look.Height > 0 && above > look.Height ? TearRate : 1;
                ages[i] = (float)(ages[i] + dt * ageRate);
                Ratios[i] = (float)Math.Min(ages[i] / (double)lives[i], 1);
            }
        }

        static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

        public int Alive
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Capacity; i++)
                    if (Ratios[i] < 1) count++;
                return count;
            }
        }

        /// <summary>The mean height of the living particles, meters. Zero when none live.</summary>
        public double MeanHeight
        {
            get
            {
                double sum = 0;
                var count = 0;
                for (var i = 0; i < Capacity; i++)
                {
                    if (Ratios[i] >= 1) continue;
                    sum += Positions[i * 3 + 1];
                    count++;
                }
                return count > 0 ? sum / count : 0;
            }
        }
    }
}
