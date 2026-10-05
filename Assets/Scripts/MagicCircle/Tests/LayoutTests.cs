using System;
using System.Linq;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;
using NUnit.Framework;

namespace MagicCircleSim.Tests
{
    public class LayoutTests
    {
        static readonly FormValue[] Forms = { FormValue.Disk, FormValue.Square, FormValue.Ring, FormValue.Line };
        static readonly DistributionValue[] Distributions = { DistributionValue.Even, DistributionValue.Symmetric, DistributionValue.Random };
        const double Eps = 1e-9;

        static SpellShape Shape(FormValue form, int count, double angleFraction = 1, double radius = 4) => new SpellShape(form, radius, angleFraction, count);

        static double Hypot(SpawnPoint p) => Math.Sqrt(p.X * p.X + p.Z * p.Z);

        [Test]
        public void EveryFormAndDistributionSpawnsExactlyTheRuneCount()
        {
            foreach (var form in Forms)
            foreach (var distribution in Distributions)
            foreach (var count in new[] { 1, 2, 3, 7 })
                Assert.AreEqual(count, SpawnLayout.Compute(Shape(form, count), distribution, 42).Length, $"{form}/{distribution}/{count}");
        }

        [Test]
        public void EveryPointStaysInsideItsShape()
        {
            foreach (var distribution in Distributions)
            {
                foreach (var p in SpawnLayout.Compute(Shape(FormValue.Disk, 7), distribution, 7)) Assert.LessOrEqual(Hypot(p), 4 + Eps);
                foreach (var p in SpawnLayout.Compute(Shape(FormValue.Ring, 7), distribution, 7)) Assert.Less(Math.Abs(Hypot(p) - 4), 1e-6);
                foreach (var p in SpawnLayout.Compute(Shape(FormValue.Square, 7), distribution, 7))
                    Assert.IsTrue(Math.Abs(p.X) <= 2 + Eps && Math.Abs(p.Z) <= 2 + Eps);
                foreach (var p in SpawnLayout.Compute(Shape(FormValue.Line, 7), distribution, 7))
                    Assert.IsTrue(p.Z == 0 && Math.Abs(p.X) <= 4 + Eps);
            }
        }

        [Test]
        public void AHalfAngleTurnsARingIntoAForwardArc_A01()
        {
            foreach (var distribution in Distributions)
            foreach (var p in SpawnLayout.Compute(Shape(FormValue.Ring, 7, 0.5), distribution, 3))
                Assert.GreaterOrEqual(p.Z, -Eps, $"{distribution} z={p.Z}");
        }

        [Test]
        public void AnEvenRingSpacesItsPointsEquallyStartingForward()
        {
            var points = SpawnLayout.Compute(Shape(FormValue.Ring, 4), DistributionValue.Even, 1);
            Assert.Less(Math.Abs(points[0].X), Eps);
            Assert.Less(Math.Abs(points[0].Z - 4), Eps);
            var angles = points.Select(p => Math.Atan2(p.X, p.Z)).ToArray();
            for (var i = 1; i < 4; i++)
            {
                var gap = ((angles[i] - angles[i - 1]) % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
                Assert.Less(Math.Abs(gap - Math.PI / 2), 1e-9);
            }
        }

        [Test]
        public void ASymmetricLayoutMirrorsAcrossTheForwardAxis_A12()
        {
            foreach (var form in Forms)
            {
                var points = SpawnLayout.Compute(Shape(form, 7), DistributionValue.Symmetric, 11);
                foreach (var m in points)
                    Assert.IsTrue(points.Any(p => Math.Abs(p.X + m.X) < 1e-9 && Math.Abs(p.Z - m.Z) < 1e-9), form.ToString());
            }
        }

        [Test]
        public void TheSameSeedGivesTheSameLayoutANewSeedANewOne()
        {
            var a = SpawnLayout.Compute(Shape(FormValue.Disk, 7), DistributionValue.Random, 5);
            CollectionAssert.AreEqual(a, SpawnLayout.Compute(Shape(FormValue.Disk, 7), DistributionValue.Random, 5));
            CollectionAssert.AreNotEqual(a, SpawnLayout.Compute(Shape(FormValue.Disk, 7), DistributionValue.Random, 6));
        }
    }
}
