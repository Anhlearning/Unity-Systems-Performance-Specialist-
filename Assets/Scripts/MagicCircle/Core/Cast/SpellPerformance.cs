// SpellPerformance: one cast of a compiled spell, stepped on a fixed clock. It
// owns the instances and the particles and knows nothing about Unity. Stepping
// fresh to a time gives the same state as stepping there frame by frame,
// because the clock is steps × DT and the only randomness is seeded.
//
// The material is emitted from its sources, which follow from what the circle
// actually holds:
//   no form rune         one source at the circle's center (A15)
//   form and count       one source per instance, laid out by SpawnLayout
//   form, no count       the whole area emits (A17)
// A spell nothing moves and no Vòng 2 core holds up stands on the ground, so a
// fire circle raises a flame from the circle itself.
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
        const int ParticleCapacity = 8192;
        const uint ParticleSeedSalt = 0x9e3779b9;
        /// <summary>Share of the pool a cast keeps alive, so a large area never recycles living matter.</summary>
        const double PoolBudget = 0.8;
        /// <summary>Height a rooted source emits from, just above the ground circle.</summary>
        const double GroundSourceY = 0.05;

        public readonly SpellIR Spell;
        public readonly Look Look;
        /// <summary>Where each instance appears. Empty when the material fills the area instead (A17).</summary>
        public readonly SpawnPoint[] Spawns;
        public readonly InstanceState[] Instances;
        public readonly ParticlePool Pool = new ParticlePool(ParticleCapacity);
        /// <summary>This instant's body strokes, rebuilt by <see cref="BuildStrokes"/>. Empty for a material without a body.</summary>
        public readonly StrokeBuffer Strokes;
        /// <summary>The material stands on the ground: nothing moves it and no Vòng 2 core holds it up.</summary>
        public readonly bool Rooted;
        public Beat Beat { get; private set; } = Beat.Charge;

        readonly Mulberry32 rng;
        /// <summary>Fractional particles owed per instance, so low rates still emit on time.</summary>
        readonly double[] owed;
        /// <summary>Particles per second for each instance, or for the whole area when it fills.</summary>
        readonly double rate;
        double owedArea;
        /// <summary>Flight seconds at which the player stopped the spell, if they did.</summary>
        double? releasedAtS;
        int steps;

        public SpellPerformance(SpellIR spell)
        {
            if (!spell.Valid || !spell.Material.HasValue) throw new ArgumentException("SpellPerformance needs a valid spell", nameof(spell));
            Spell = spell;
            Look = Looks.Of(spell.Material.Value);
            var shape = spell.Shape;
            if (!shape.HasValue) Spawns = new[] { new SpawnPoint(0, 0) };
            else if (shape.Value.Fills) Spawns = Array.Empty<SpawnPoint>();
            else Spawns = SpawnLayout.Compute(shape.Value, spell.Motion.Distribution, spell.Seed);
            Rooted = !spell.Motion.Moves && !spell.Pattern.HasValue;

            Instances = new InstanceState[Spawns.Length];
            for (var i = 0; i < Instances.Length; i++) Instances[i].DirZ = 1;
            owed = new double[Spawns.Length];
            rng = new Mulberry32(spell.Seed ^ ParticleSeedSalt);

            var sources = shape.HasValue && shape.Value.Fills ? Patches(shape.Value, Look.Mouth) : Spawns.Length;
            var total = Math.Min(Look.Rate * sources, PoolBudget * ParticleCapacity / Look.Life);
            rate = shape.HasValue && shape.Value.Fills ? total : total / Spawns.Length;

            var bodySources = shape.HasValue && shape.Value.Fills ? 1 : Spawns.Length;
            Strokes = new StrokeBuffer(Look.Body.PointsPerSource * bodySources);
        }

        /// <summary>How many mouth-sized sources it takes to cover a filled area, at least one.</summary>
        static double Patches(SpellShape shape, double mouth)
        {
            var m = Math.Max(mouth, 0.3);
            var r = shape.Radius;
            switch (shape.Form)
            {
                case Design.FormValue.Disk: return Math.Max(1, shape.AngleFraction * r * r / (m * m));
                case Design.FormValue.Ring: return Math.Max(1, shape.AngleFraction * Math.PI * r / m);
                case Design.FormValue.Square: return Math.Max(1, r * r / (Math.PI * m * m));
                default: return Math.Max(1, r / m);
            }
        }

        /// <summary>How long the flight beat lasts: the rune's lifetime, cut short by a release, or forever (A20).</summary>
        public double LifetimeS => releasedAtS ?? Spell.Motion.LifetimeS ?? double.PositiveInfinity;

        /// <summary>Infinite while a sustained spell has not been released.</summary>
        public double DurationS => CastTimeline.Duration(LifetimeS);

        public double TimeS => steps * DT;

        public bool Done => TimeS >= DurationS;

        /// <summary>Ends the flight beat now, so the spell fades out. A sustained spell only ends this way.</summary>
        public void Release()
        {
            var flightS = CastTimeline.At(TimeS, LifetimeS).FlightS;
            if (flightS < LifetimeS) releasedAtS = flightS;
        }

        /// <summary>
        /// Rebuilds <see cref="Strokes"/> for the current time: one body per instance, or one
        /// over the whole area when the material fills it, sized to that area. A pure
        /// function of the clock, so it may be called any number of times per step.
        /// </summary>
        public void BuildStrokes()
        {
            Strokes.Clear();
            var body = Look.Body;
            if (body.Kind == BodyKind.None) return;
            var aloft = Spell.Pattern.HasValue;
            // A wavefront already sent keeps its range; every other body grows and swells with the envelope.
            var swells = body.Kind != BodyKind.Waves;
            var shape = Spell.Shape;
            if (shape.HasValue && shape.Value.Fills)
            {
                var envelope = CastTimeline.At(TimeS, LifetimeS);
                var at = Place(new SpawnPoint(0, 0), envelope.FlightS);
                var extent = shape.Value.Form == Design.FormValue.Square ? shape.Value.Radius / 2 : shape.Value.Radius;
                var size = extent / body.TopRadius * (swells ? envelope.Scale : 1);
                BodyStrokes.Build(body, new BodySource(at.X, at.Y, at.Z, size, envelope.Alpha, 0, aloft), TimeS, LifetimeS, Spell.Seed, Strokes);
                return;
            }
            for (var i = 0; i < Instances.Length; i++)
            {
                var state = Instances[i];
                var source = new BodySource(state.X, state.Y, state.Z, swells ? state.Scale : 1, state.Alpha, i, aloft);
                BodyStrokes.Build(body, source, TimeS, LifetimeS, Spell.Seed, Strokes);
            }
        }

        public void AdvanceTo(double tS)
        {
            var target = Math.Min(Math.Floor(tS / DT + 1e-9), Math.Ceiling(DurationS / DT));
            while (steps < target) Step();
        }

        TrajectoryPoint Place(SpawnPoint spawn, double flightS)
        {
            var motion = Spell.Motion;
            if (motion.Moves) return Trajectory.At(spawn, motion.Path.Value, motion.Speed.Value, flightS);
            return new TrajectoryPoint(spawn.X, Rooted ? GroundSourceY : Trajectory.HoverM, spawn.Z, 0, 1);
        }

        void Step()
        {
            steps++;
            var envelope = CastTimeline.At(TimeS, LifetimeS);
            Beat = envelope.Beat;
            for (var i = 0; i < Spawns.Length; i++)
            {
                var at = Place(Spawns[i], envelope.FlightS);
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
                owed[i] += rate * DT * envelope.Alpha;
                for (; owed[i] >= 1; owed[i]--) Pool.Emit(at.X, at.Y, at.Z, Look.Mouth, Look, rng, flicker);
            }
            if (Spell.Shape.HasValue && Spell.Shape.Value.Fills && envelope.Alpha > 0)
            {
                owedArea += rate * DT * envelope.Alpha;
                for (; owedArea >= 1; owedArea--)
                {
                    var at = Place(SpawnLayout.PointIn(Spell.Shape.Value, rng), envelope.FlightS);
                    Pool.Emit(at.X, at.Y, at.Z, 0, Look, rng, 1 - Look.Flicker * rng.Next());
                }
            }
            Pool.Step(DT, TimeS, Look, rng);
        }
    }
}
