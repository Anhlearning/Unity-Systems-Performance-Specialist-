// The Unity port casts exactly what the web simulator casts. Expected values
// were printed by the web build's own compileSpell, spawnLayout and Performance.
// If the web build's rules change on purpose, regenerate these numbers.

using MagicCircleSim.Cast;
using MagicCircleSim.Circle;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;
using NUnit.Framework;

namespace MagicCircleSim.Tests
{
    public class WebParityTests
    {
        static readonly MagicCircle LoiTran = new MagicCircle(ElementId.Hoa, NatureId.DacBiet, PatternId.Cau,
            new[] { "form-disk", "radius-6", "count-7", "dist-random", "path-orbit", "speed-10", "lifetime-3" });

        [Test]
        public void SeedsMatchTheWebBuild()
        {
            Assert.AreEqual(3402558243u, SpellCompiler.Compile(LoiTran).Seed);
            Assert.AreEqual(1478955262u, SpellCompiler.Compile(MagicCircle.Empty).Seed);
        }

        [Test]
        public void RandomLayoutMatchesTheWebBuild()
        {
            var spell = SpellCompiler.Compile(LoiTran);
            var points = SpawnLayout.Compute(spell.Shape, spell.Motion.Distribution, spell.Seed);
            Assert.AreEqual(2.6057082094073825, points[0].X, 1e-12);
            Assert.AreEqual(0.1187294751972225, points[0].Z, 1e-12);
            Assert.AreEqual(1.9235420598512636, points[6].X, 1e-12);
            Assert.AreEqual(-5.414829797946485, points[6].Z, 1e-12);
        }

        [Test]
        public void SymmetricAndEvenLayoutsMatchTheWebBuild()
        {
            var symmetric = SpawnLayout.Compute(new SpellShape(FormValue.Square, 6, 1, 3), DistributionValue.Symmetric, 11);
            Assert.AreEqual(0.06952229188755155, symmetric[0].Z, 1e-12);
            Assert.AreEqual(1.8243556923698634, symmetric[1].X, 1e-12);
            Assert.AreEqual(0.5409458158537745, symmetric[2].Z, 1e-12);

            var disk = SpawnLayout.Compute(new SpellShape(FormValue.Disk, 4, 0.5, 3), DistributionValue.Even, 1);
            Assert.AreEqual(-1.632993161855452, disk[0].X, 1e-12);
            Assert.AreEqual(2.6924904515051487, disk[2].X, 1e-12);
        }

        [Test]
        public void APerformanceMatchesTheWebBuildStepForStep()
        {
            var perf = new SpellPerformance(SpellCompiler.Compile(LoiTran));
            perf.AdvanceTo(1.5);
            Assert.AreEqual(166, perf.Pool.Alive);
            Assert.AreEqual(-1.2327218307844998, perf.Instances[0].X, 1e-9);
            Assert.AreEqual(-2.2987407093365153, perf.Instances[0].Z, 1e-9);
            Assert.AreEqual(0.8113174106925726, perf.Instances[0].Alpha, 1e-12);
            Assert.AreEqual(1.382791519165039f, perf.Pool.Positions[0], 1e-5f);
        }
    }
}
