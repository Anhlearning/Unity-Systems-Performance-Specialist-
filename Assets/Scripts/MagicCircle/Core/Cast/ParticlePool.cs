// ParticlePool: a fixed ring buffer of particles stepped on the CPU. Plain
// float arrays and no UnityEngine, so a whole cast runs in an EditMode test and
// the stage uploads the arrays to the GPU as-is. Nothing allocates per step.

using System;
using MagicCircleSim.Compiler;

namespace MagicCircleSim.Cast
{
    public sealed class ParticlePool
    {
        const double GroundY = 0.02;
        const double Bounce = 0.3;

        public readonly int Capacity;
        /// <summary>x, y, z per particle, caster frame.</summary>
        public readonly float[] Positions;
        /// <summary>age / life, 0..1. One means dead, which the shader draws as nothing.</summary>
        public readonly float[] Ratios;
        public readonly float[] Sizes;
        readonly float[] velocities;
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
            ages = Filled(capacity, 1);
            lives = Filled(capacity, 1);
        }

        static float[] Filled(int length, float value)
        {
            var array = new float[length];
            for (var i = 0; i < length; i++) array[i] = value;
            return array;
        }

        /// <summary>Births one particle, overwriting the oldest slot once the pool is full.</summary>
        public void Emit(double x, double y, double z, Look look, Mulberry32 rng, double brightness = 1)
        {
            var i = next;
            next = (next + 1) % Capacity;
            var p = i * 3;
            Positions[p] = (float)x;
            Positions[p + 1] = (float)y;
            Positions[p + 2] = (float)z;
            // A random direction in the unit ball, scaled by the look's spread.
            var theta = rng.Next() * Math.PI * 2;
            var u = rng.Next() * 2 - 1;
            var s = Math.Sqrt(1 - u * u) * look.Spread * Math.Cbrt(rng.Next());
            velocities[p] = (float)(s * Math.Cos(theta));
            velocities[p + 1] = (float)(u * look.Spread * 0.6);
            velocities[p + 2] = (float)(s * Math.Sin(theta));
            ages[i] = 0;
            lives[i] = (float)(look.Life * (0.6 + 0.8 * rng.Next()));
            Sizes[i] = (float)(look.Size * (0.6 + 0.8 * rng.Next()) * brightness);
            Ratios[i] = 0;
        }

        public void Step(double dt, Look look, Mulberry32 rng)
        {
            var keep = Math.Max(1 - look.Drag * dt, 0);
            var v = velocities;
            for (var i = 0; i < Capacity; i++)
            {
                if (Ratios[i] >= 1) continue;
                var p = i * 3;
                v[p] = (float)((v[p] + (rng.Next() - 0.5) * 2 * look.Jitter * dt) * keep);
                v[p + 1] = (float)((v[p + 1] + look.Lift * dt + (rng.Next() - 0.5) * 2 * look.Jitter * dt) * keep);
                v[p + 2] = (float)((v[p + 2] + (rng.Next() - 0.5) * 2 * look.Jitter * dt) * keep);
                Positions[p] = (float)(Positions[p] + v[p] * dt);
                Positions[p + 1] = (float)(Positions[p + 1] + v[p + 1] * dt);
                Positions[p + 2] = (float)(Positions[p + 2] + v[p + 2] * dt);
                if (Positions[p + 1] < GroundY)
                {
                    Positions[p + 1] = (float)GroundY;
                    v[p + 1] = (float)(-v[p + 1] * Bounce);
                }
                ages[i] = (float)(ages[i] + dt);
                Ratios[i] = (float)Math.Min(ages[i] / (double)lives[i], 1);
            }
        }

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
    }
}
