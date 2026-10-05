using System.Linq;
using MagicCircleSim.Circle;
using MagicCircleSim.Design;
using NUnit.Framework;

namespace MagicCircleSim.Tests
{
    public class DrawCircleTests
    {
        [Test]
        public void EveryRuneInTheCatalogHasGlyphArt()
        {
            foreach (var rune in Runes.All) Assert.IsNotNull(Glyphs.Rune(rune.Id), rune.Id);
        }

        [Test]
        public void AnEmptyCircleDrawsPlaceholdersForEveryLayer()
        {
            var drawing = DrawCircle.Build(MagicCircle.Empty);
            Assert.AreEqual(3, drawing.Strokes.Count(s => s.Dashed && s.Opacity == 0.35));
        }

        [Test]
        public void EveryPlacedRuneGetsAHitSlotAtItsGlyph()
        {
            var circle = new MagicCircle(ElementId.Thuy, NatureId.Vuong, PatternId.Nhon, new[] { "form-ring", "count-7", "path-orbit" });
            var drawing = DrawCircle.Build(circle);
            Assert.AreEqual(3, drawing.RuneSlots.Count);
            // The first rune of a band sits at the top.
            Assert.AreEqual("form-ring", drawing.RuneAt(new Vec2(0, -CircleRadii.V3Runes)));
            Assert.AreEqual("path-orbit", drawing.RuneAt(new Vec2(0, -CircleRadii.V4Runes)));
            Assert.IsNull(drawing.RuneAt(new Vec2(0, 0)));
        }

        [Test]
        public void TheInkFollowsTheCenterElementUnlessOverridden()
        {
            var circle = MagicCircle.Empty.WithCenter(ElementId.Hoa);
            Assert.AreEqual("#ff8a4c", DrawCircle.Build(circle).Ink);
            Assert.AreEqual("#ffffff", DrawCircle.Build(circle, "#ffffff").Ink);
        }

        [Test]
        public void EveryGlyphFlattensAndRasterizesInk()
        {
            var all = MagicCircle.Empty.WithCenter(ElementId.Thuy).WithNature(NatureId.DacBiet).WithPattern(PatternId.Vuong)
                .WithRunes(Runes.All.Select(r => r.Id));
            const int size = 256;
            var coverage = new float[size * size];
            CircleRaster.Rasterize(DrawCircle.Build(all), size, coverage);
            Assert.Greater(coverage.Count(c => c > 0.5f), size * 4);
            // Corners sit outside Vòng 5 and stay empty.
            Assert.AreEqual(0, coverage[0]);
            Assert.AreEqual(0, coverage[size * size - 1]);
        }

        [Test]
        public void TheDrawingsTopLandsOnTheTexturesTopRow()
        {
            // A single dot near the drawing's top edge (y = -90, y grows downward).
            var canvas = new ArtCanvas(DrawCircle.Box, "#fff", StrokeStyle.Solid(1));
            canvas.Dot(0, -90, 4);
            const int size = 100;
            var coverage = new float[size * size];
            CircleRaster.Rasterize(canvas.Drawing, size, coverage);
            Assert.AreEqual(1f, coverage[95 * size + 50], 1e-6, "Texture rows go bottom-up, so the top is row 95.");
            Assert.AreEqual(0f, coverage[5 * size + 50]);
        }

        [Test]
        public void ArcsFollowTheSvgSweepFlag()
        {
            // angle-0.5: a half disk over the top from (-0.75, 0) to (0.75, 0), sweep 1.
            var arc = SvgPath.Flatten("M-0.75,0 A0.75,0.75 0 0 1 0.75,0")[0].Points;
            Assert.IsTrue(arc.All(p => p.Y <= 1e-9), "Sweep 1 runs clockwise on screen, through the top (negative y).");
            Assert.AreEqual(0.75, arc.Max(p => -p.Y), 1e-3);
        }
    }
}
