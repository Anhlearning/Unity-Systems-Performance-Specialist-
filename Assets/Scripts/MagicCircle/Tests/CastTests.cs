using System;
using System.Linq;
using MagicCircleSim.Cast;
using MagicCircleSim.Circle;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;
using NUnit.Framework;

namespace MagicCircleSim.Tests
{
    public class CastTests
    {
        static readonly MagicCircle OrbitingFire = new MagicCircle(ElementId.Hoa, NatureId.Tron, PatternId.Cau,
            new[] { "form-ring", "radius-4", "count-3", "path-orbit", "speed-10", "lifetime-3" });

        [Test]
        public void StraightFlightMovesForwardAtTheRuneSpeed_A03_A08()
        {
            var at = Trajectory.At(new SpawnPoint(1, 2), PathValue.Straight, 10, 1.5);
            Assert.AreEqual(1, at.X);
            Assert.AreEqual(17, at.Z);
        }

        [Test]
        public void OrbitingKeepsItsDistanceFromTheCaster_V4_MOVE_01()
        {
            foreach (var t in new[] { 0, 0.4, 2.5 })
            {
                var at = Trajectory.At(new SpawnPoint(0, 4), PathValue.Orbit, 10, t);
                Assert.Less(Math.Abs(Math.Sqrt(at.X * at.X + at.Z * at.Z) - 4), 1e-9);
            }
        }

        [Test]
        public void AnInstanceAtTheCenterStillOrbitsAtTheMinimumRadius_A13()
        {
            var at = Trajectory.At(new SpawnPoint(0, 0), PathValue.Orbit, 10, 1);
            Assert.Less(Math.Abs(Math.Sqrt(at.X * at.X + at.Z * at.Z) - 1), 1e-9);
        }

        [Test]
        public void NothingManifestsWhileTheCircleCharges()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(OrbitingFire));
            perf.AdvanceTo(CastTimeline.ChargeS - SpellPerformance.DT);
            Assert.AreEqual(0, perf.Pool.Alive);
            Assert.IsTrue(perf.Instances.All(i => i.Alpha == 0));
            perf.AdvanceTo(CastTimeline.ChargeS + CastTimeline.ManifestS + 0.5);
            Assert.Greater(perf.Pool.Alive, 0);
        }

        [Test]
        public void LifetimeStretchesOnlyTheFlightBeat_A02()
        {
            const double flightStart = CastTimeline.ChargeS + CastTimeline.ManifestS;
            Assert.AreEqual(Beat.Flight, CastTimeline.At(flightStart + 2.9, 3).Beat);
            Assert.AreEqual(Beat.Fade, CastTimeline.At(flightStart + 3.1, 3).Beat);
            Assert.AreEqual(Beat.Flight, CastTimeline.At(flightStart + 4.9, 5).Beat);
        }

        [Test]
        public void SteppingFreshToATimeEqualsSteppingThereFrameByFrame()
        {
            var spell = SpellCompiler.Compile(OrbitingFire);
            var fresh = new SpellPerformance(spell);
            fresh.AdvanceTo(2);
            var incremental = new SpellPerformance(spell);
            for (var t = 0.0; t <= 2; t += 1.0 / 37) incremental.AdvanceTo(t);
            incremental.AdvanceTo(2);
            CollectionAssert.AreEqual(fresh.Pool.Positions, incremental.Pool.Positions);
            CollectionAssert.AreEqual(fresh.Instances, incremental.Instances);
        }

        [Test]
        public void ACastEnds()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(OrbitingFire));
            perf.AdvanceTo(60);
            Assert.IsTrue(perf.Done);
        }

        [Test]
        public void EveryMaterialHasALookRow()
        {
            foreach (MaterialId id in Enum.GetValues(typeof(MaterialId))) Assert.IsNotNull(Looks.Of(id), id.ToString());
        }
    }
}
