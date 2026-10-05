using System.Linq;
using MagicCircleSim.Circle;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;
using NUnit.Framework;

namespace MagicCircleSim.Tests
{
    public class CompileSpellTests
    {
        static MagicCircle Fire(params string[] runes) => MagicCircle.Empty.WithCenter(ElementId.Hoa).WithRunes(runes);

        [Test]
        public void ACircleWithoutACenterElementDoesNotCompile_CENTER_001()
        {
            var spell = SpellCompiler.Compile(MagicCircle.Empty);
            Assert.IsFalse(spell.Valid);
            Assert.IsTrue(spell.Has(DiagnosticKey.MissingCenter));
        }

        [Test]
        public void OnlyOneShapeRuneMaySitOnVong3_V3_R02()
        {
            var spell = SpellCompiler.Compile(Fire("form-disk", "form-ring"));
            Assert.IsFalse(spell.Valid);
            Assert.IsTrue(spell.Has(DiagnosticKey.MultipleShapeRunes));
        }

        [Test]
        public void TwoRunesOfAnotherCategoryAreRejectedToo_A10()
        {
            Assert.IsTrue(SpellCompiler.Compile(Fire("speed-10", "speed-20")).Has(DiagnosticKey.DuplicateRune));
        }

        [Test]
        public void DistributionDefaultsToEvenWhenNoRuneIsPlaced_V4_RULE_001()
        {
            var spell = SpellCompiler.Compile(Fire());
            Assert.IsTrue(spell.Valid);
            Assert.AreEqual(DistributionValue.Even, spell.Motion.Distribution);
            Assert.IsTrue(spell.WasDefaulted(DefaultedSlot.Distribution));
        }

        [Test]
        public void EmptySlotsFallBackToDefaultsAndSaySo_A09()
        {
            var spell = SpellCompiler.Compile(Fire());
            Assert.AreEqual(Defaults.Nature, spell.Nature);
            Assert.AreEqual(Defaults.Pattern, spell.Pattern);
            CollectionAssert.AreEqual(new[] { DefaultedSlot.Nature, DefaultedSlot.Pattern }, spell.Defaulted.Take(2));
            Assert.IsTrue(spell.Diagnostics.Any(d => d.Key == DiagnosticKey.Defaulted && d.Severity == Severity.Info));
        }

        [TestCase(NatureId.Tron, ElementId.Hoa, MaterialId.Lua)]
        [TestCase(NatureId.Vuong, ElementId.Hoa, MaterialId.Laser)]
        [TestCase(NatureId.DacBiet, ElementId.Hoa, MaterialId.Loi)]
        [TestCase(NatureId.Vuong, ElementId.Thuy, MaterialId.Bang)]
        [TestCase(NatureId.DacBiet, ElementId.Tho, MaterialId.KimLoai)]
        public void Vong1CrossedWithTheCenterNamesTheMaterial_Sheet01(NatureId nature, ElementId center, MaterialId material)
        {
            Assert.AreEqual(material, SpellCompiler.Compile(MagicCircle.Empty.WithCenter(center).WithNature(nature)).Material);
        }

        [Test]
        public void RunesResolveToTheirValuesWithUnits()
        {
            var spell = SpellCompiler.Compile(Fire("form-ring", "radius-4", "angle-0.5", "count-7", "path-orbit", "lifetime-5", "speed-20", "dist-random"));
            Assert.AreEqual(new SpellShape(FormValue.Ring, 4, 0.5, 7), spell.Shape);
            Assert.AreEqual(new SpellMotion(PathValue.Orbit, 5, DistributionValue.Random, 20), spell.Motion);
            CollectionAssert.AreEqual(new[] { DefaultedSlot.Nature, DefaultedSlot.Pattern }, spell.Defaulted);
        }

        [Test]
        public void AnAngleOnASquareIsNotedAsIgnored_A11()
        {
            var spell = SpellCompiler.Compile(Fire("form-square", "angle-0.5"));
            Assert.IsTrue(spell.Valid);
            Assert.IsTrue(spell.Has(DiagnosticKey.AngleIgnored));
        }

        [Test]
        public void TheSeedDependsOnWhatIsOnTheCircleNotTheOrderItWasPlacedIn()
        {
            var a = SpellCompiler.Compile(Fire("count-3", "form-ring"));
            var b = SpellCompiler.Compile(Fire("form-ring", "count-3"));
            var c = SpellCompiler.Compile(Fire("form-ring", "count-7"));
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreNotEqual(a.Seed, c.Seed);
        }

        [Test]
        public void PlacingARuneReplacesItsCategoryAndPlacingItAgainRemovesIt()
        {
            var circle = Fire().PlaceRune("speed-10").PlaceRune("speed-20");
            CollectionAssert.AreEqual(new[] { "speed-20" }, circle.Runes);
            CollectionAssert.IsEmpty(circle.PlaceRune("speed-20").Runes);
        }
    }
}
