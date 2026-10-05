// SpellPerformance: one cast of a compiled spell, stepped on a fixed clock. It
// owns the instances and the particles and knows nothing about Unity. Stepping
// fresh to a time gives the same state as stepping there frame by frame,
// because the clock is steps × DT and the only randomness is seeded.
//
//   var perf = new SpellPerformance(spell);
//   perf.AdvanceTo(1.5);
//   var x = perf.Instances[0].X;

using System;
using MagicCircleSim.Compiler;

namespace MagicCircleSim.Cast
{
    public struct InstanceState
    {
        public double X, Y, Z;
        public double DirX, DirZ;
        public double Scale;
        public double Alpha;
    }

    public sealed class SpellPerformance
    {
        public const double DT = 1.0 / 60;
        const int ParticleCapacity = 6000;
        const uint ParticleSeedSalt = 0x9e3779b9;

        public readonly SpellIR Spell;
        public readonly Look Look;
        public readonly SpawnPoint[] Spawns;
        public readonly InstanceState[] Instances;
        public readonly ParticlePool Pool = new ParticlePool(ParticleCapacity);
        public readonly double DurationS;
        public Beat Beat { get; private set; } = Beat.Charge;

        readonly Mulberry32 rng;
        /// <summary>Fractional particles owed per instance, so low rates still emit on time.</summary>
        readonly double[] owed;
        int steps;

        public SpellPerformance(SpellIR spell)
        {
            if (!spell.Valid || !spell.Material.HasValue) throw new ArgumentException("SpellPerformance needs a valid spell", nameof(spell));
            Spell = spell;
            Look = Looks.Of(spell.Material.Value);
            Spawns = SpawnLayout.Compute(spell.Shape, spell.Motion.Distribution, spell.Seed);
            Instances = new InstanceState[Spawns.Length];
            for (var i = 0; i < Instances.Length; i++) Instances[i].DirZ = 1;
            owed = new double[Spawns.Length];
            rng = new Mulberry32(spell.Seed ^ ParticleSeedSalt);
            DurationS = CastTimeline.Duration(spell.Motion.LifetimeS);
        }

        public double TimeS => steps * DT;

        public bool Done => TimeS >= DurationS;

        public void AdvanceTo(double tS)
        {
            var target = Math.Min(Math.Floor(tS / DT + 1e-9), Math.Ceiling(DurationS / DT));
            while (steps < target) Step();
        }

        void Step()
        {
            steps++;
            var motion = Spell.Motion;
            var envelope = CastTimeline.At(TimeS, motion.LifetimeS);
            Beat = envelope.Beat;
            for (var i = 0; i < Spawns.Length; i++)
            {
                var at = Trajectory.At(Spawns[i], motion.Path, motion.Speed, envelope.FlightS);
                var flicker = 1 - Look.Flicker * rng.Next();
                ref var state = ref Instances[i];
                state.X = at.X;
                state.Y = at.Y;
                state.Z = at.Z;
                state.DirX = at.DirX;
                state.DirZ = at.DirZ;
                state.Scale = envelope.Scale;
                state.Alpha = envelope.Alpha * flicker;
                if (envelope.Alpha <= 0) continue;
                owed[i] += Look.Rate * DT * envelope.Alpha;
                for (; owed[i] >= 1; owed[i]--) Pool.Emit(at.X, at.Y, at.Z, Look, rng, flicker);
            }
            Pool.Step(DT, Look, rng);
        }
    }
}
