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

        /// <summary>Only the two required layers: fire burning on the circle.</summary>
        static readonly MagicCircle Flame = MagicCircle.Empty.WithCenter(ElementId.Hoa).WithNature(NatureId.Tron);

        const double Manifested = CastTimeline.ChargeS + CastTimeline.ManifestS;

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
        public void AFlameWithNothingElseRisesFromTheCircleAndKeepsBurning_A15_A18_A20()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(Flame));
            Assert.IsTrue(perf.Rooted);
            Assert.AreEqual(1, perf.Instances.Length);
            perf.AdvanceTo(Manifested + 1.5);
            Assert.AreEqual(0, perf.Instances[0].X);
            Assert.AreEqual(0, perf.Instances[0].Z);
            Assert.Greater(perf.Pool.Alive, 100);
            Assert.Greater(perf.Pool.MeanHeight, 0.5, "Fire is buoyant: the flame stands above its source.");
            perf.AdvanceTo(600);
            Assert.IsFalse(perf.Done, "No lifetime rune: the spell lasts until it is stopped.");
            Assert.AreEqual(Beat.Flight, perf.Beat);
        }

        [Test]
        public void StoppingASustainedSpellFadesItOut()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(Flame));
            perf.AdvanceTo(Manifested + 2);
            perf.Release();
            perf.AdvanceTo(Manifested + 2 + CastTimeline.FadeS / 2);
            Assert.AreEqual(Beat.Fade, perf.Beat);
            perf.AdvanceTo(Manifested + 2 + CastTimeline.FadeS + CastTimeline.TailS + 0.1);
            Assert.IsTrue(perf.Done);
        }

        [Test]
        public void APathWithoutASpeedStandsStill_A19()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(Flame.WithRunes(new[] { "path-straight" })));
            perf.AdvanceTo(Manifested + 2);
            Assert.AreEqual(0, perf.Instances[0].Z);
        }

        [Test]
        public void AFormWithoutACountFillsItsAreaWithMatter_A17()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(Flame.WithRunes(new[] { "form-disk", "radius-4" })));
            CollectionAssert.IsEmpty(perf.Instances);
            perf.AdvanceTo(Manifested + 0.2);
            Assert.Greater(perf.Pool.Alive, 300);
            var far = 0;
            for (var i = 0; i < perf.Pool.Capacity; i++)
            {
                if (perf.Pool.Ratios[i] >= 1) continue;
                var x = perf.Pool.Positions[i * 3];
                var z = perf.Pool.Positions[i * 3 + 2];
                if (x * x + z * z > 2 * 2) far++;
            }
            Assert.Greater(far, 100, "Matter rises across the whole disk, not just at its center.");
        }

        [Test]
        public void EveryMaterialCastsAloneWithoutBreaking()
        {
            foreach (ElementId element in Enum.GetValues(typeof(ElementId)))
            foreach (NatureId nature in Enum.GetValues(typeof(NatureId)))
            {
                var perf = new SpellPerformance(SpellCompiler.Compile(MagicCircle.Empty.WithCenter(element).WithNature(nature)));
                perf.AdvanceTo(Manifested + 1);
                Assert.Greater(perf.Pool.Alive, 0, $"{element}/{nature}");
                Assert.IsFalse(perf.Pool.Positions.Any(float.IsNaN), $"{element}/{nature}");
            }
        }

        static SpellPerformance Alone(ElementId element, NatureId nature, params string[] runes) =>
            new SpellPerformance(SpellCompiler.Compile(MagicCircle.Empty.WithCenter(element).WithNature(nature).WithRunes(runes)));

        /// <summary>Each stroke's mean height and mean distance from the axis through (x, z).</summary>
        static (double y, double r)[] StrokeCenters(StrokeBuffer strokes, double x = 0, double z = 0)
        {
            var centers = new (double, double)[strokes.StrokeCount];
            for (var s = 0; s < strokes.StrokeCount; s++)
            {
                double y = 0, r = 0;
                for (var i = 0; i < strokes.Counts[s]; i++)
                {
                    var p = (strokes.Starts[s] + i) * 3;
                    y += strokes.Points[p + 1];
                    r += Math.Sqrt(Math.Pow(strokes.Points[p] - x, 2) + Math.Pow(strokes.Points[p + 2] - z, 2));
                }
                centers[s] = (y / strokes.Counts[s], r / strokes.Counts[s]);
            }
            return centers;
        }

        [Test]
        public void AWhirlwindIsAFunnelThatFlaresWithHeight()
        {
            var perf = Alone(ElementId.Phong, NatureId.Vuong);
            perf.AdvanceTo(Manifested + 1);
            perf.BuildStrokes();
            var centers = StrokeCenters(perf.Strokes);
            Assert.Greater(centers.Length, 20);
            var low = centers.Where(c => c.y < 1.5).Average(c => c.r);
            var high = centers.Where(c => c.y > 3.5).Average(c => c.r);
            Assert.Greater(high, low * 2, "Narrow at the ground, wide at the top.");
        }

        [Test]
        public void SoundWavefrontsSpreadOutward()
        {
            var perf = Alone(ElementId.Phong, NatureId.DacBiet);
            perf.AdvanceTo(Manifested + 0.1);
            perf.BuildStrokes();
            var early = StrokeCenters(perf.Strokes).Max(c => c.r);
            perf.AdvanceTo(Manifested + 0.25);
            perf.BuildStrokes();
            var later = StrokeCenters(perf.Strokes).Max(c => c.r);
            Assert.Greater(later, early);
        }

        [Test]
        public void ARadiusRuneSetsHowFarASoundCarries_A17()
        {
            var perf = Alone(ElementId.Phong, NatureId.DacBiet, "form-disk", "radius-2");
            perf.AdvanceTo(Manifested + 3);
            perf.BuildStrokes();
            Assert.LessOrEqual(StrokeCenters(perf.Strokes).Max(c => c.r), 2 * 1.03);
        }

        [Test]
        public void EveryWhirlwindStandsOnItsOwnInstance()
        {
            var perf = Alone(ElementId.Phong, NatureId.Vuong, "form-ring", "radius-4", "count-3");
            perf.AdvanceTo(Manifested + 1);
            perf.BuildStrokes();
            foreach (var spawn in perf.Spawns)
            {
                var near = StrokeCenters(perf.Strokes, spawn.X, spawn.Z).Count(c => c.r < 3);
                Assert.Greater(near, 20, spawn.ToString());
            }
        }

        [Test]
        public void BodiesAreAbsentWhileChargingAndRepeatable()
        {
            var perf = Alone(ElementId.Phong, NatureId.Tron);
            perf.AdvanceTo(CastTimeline.ChargeS / 2);
            perf.BuildStrokes();
            Assert.AreEqual(0, perf.Strokes.StrokeCount);
            perf.AdvanceTo(Manifested + 1);
            perf.BuildStrokes();
            var first = perf.Strokes.Points.Take(perf.Strokes.PointCount * 3).ToArray();
            perf.BuildStrokes();
            CollectionAssert.AreEqual(first, perf.Strokes.Points.Take(perf.Strokes.PointCount * 3).ToArray());
            Assert.Greater(perf.Strokes.StrokeCount, 5);
        }

        [Test]
        public void EveryMaterialHasASpriteAtlas()
        {
            foreach (MaterialId id in Enum.GetValues(typeof(MaterialId)))
            {
                var sprite = Looks.Of(id).Sprite;
                Assert.IsTrue(sprite.Grid == 1 || sprite.Grid == 2 || sprite.Grid == 4, id.ToString());
                // Art~/generate_particles.py draws these; rerun it after adding a material.
                var path = $"Assets/Scripts/MagicCircle/View/Resources/MagicCircleParticles/{Materials.Get(id).Key}.png";
                Assert.IsTrue(System.IO.File.Exists(path), path);
            }
        }

        [Test]
        public void EveryMaterialHasALookRow()
        {
            foreach (MaterialId id in Enum.GetValues(typeof(MaterialId))) Assert.IsNotNull(Looks.Of(id), id.ToString());
        }
    }
}
