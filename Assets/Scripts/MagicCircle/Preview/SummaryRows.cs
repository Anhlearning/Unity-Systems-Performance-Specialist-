// The compiled spell as readable rows, layer by layer. Pure, so the summary
// panel only renders them.

using System.Collections.Generic;
using System.Globalization;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;

namespace MagicCircleSim.Preview
{
    public readonly struct SummaryRow
    {
        public readonly string Layer;
        public readonly string Label;
        public readonly string Value;
        public readonly bool Defaulted;
        /// <summary>Assumption ids that give this value its meaning.</summary>
        public readonly string[] Assumptions;

        public SummaryRow(string layer, string label, string value, bool defaulted, params string[] assumptions)
        {
            Layer = layer;
            Label = label;
            Value = value;
            Defaulted = defaulted;
            Assumptions = assumptions;
        }
    }

    public static class SummaryRows
    {
        static string N(double value) => value.ToString(CultureInfo.InvariantCulture);

        static string LabelOf(RuneCategory category, System.Func<RuneDef, bool> matches)
        {
            foreach (var rune in Runes.InCategory(category))
                if (matches(rune)) return rune.Label;
            return "?";
        }

        public static List<SummaryRow> Of(SpellIR spell)
        {
            bool D(DefaultedSlot slot) => spell.WasDefaulted(slot);
            var shape = spell.Shape;
            var motion = spell.Motion;
            return new List<SummaryRow>
            {
                new SummaryRow("Tâm", "Nguyên tố", spell.Element.HasValue ? Elements.Get(spell.Element.Value).Name : "—", false),
                new SummaryRow("Vòng 1", "Bản chất", Natures.Get(spell.Nature).Name, D(DefaultedSlot.Nature)),
                new SummaryRow("Vòng 1", "→ Vật chất", spell.Material.HasValue ? Materials.Get(spell.Material.Value).Name : "—", D(DefaultedSlot.Nature)),
                new SummaryRow("Vòng 2", "Pattern", Patterns.Get(spell.Pattern).Name, D(DefaultedSlot.Pattern), "A07"),
                new SummaryRow("Vòng 3", "Hình dạng", LabelOf(RuneCategory.Form, r => r.Form == shape.Form), D(DefaultedSlot.Form), "A06"),
                new SummaryRow("Vòng 3", "Bán kính/cạnh", $"{N(shape.Radius)} m", D(DefaultedSlot.Radius), "A04"),
                new SummaryRow("Vòng 3", "Góc chiếm diện tích", $"{N(shape.AngleFraction)} ({N(shape.AngleFraction * 360)}°)", D(DefaultedSlot.Angle), "A01", "A11"),
                new SummaryRow("Vòng 3", "Số lượng", shape.Count.ToString(CultureInfo.InvariantCulture), D(DefaultedSlot.Count)),
                new SummaryRow("Vòng 4", "Hướng di chuyển", LabelOf(RuneCategory.Path, r => r.Path == motion.Path), D(DefaultedSlot.Path),
                    motion.Path == PathValue.Orbit ? "A13" : "A08"),
                new SummaryRow("Vòng 4", "Thời gian tồn tại", $"{N(motion.LifetimeS)} s", D(DefaultedSlot.Lifetime), "A02"),
                new SummaryRow("Vòng 4", "Cách phân bố", LabelOf(RuneCategory.Distribution, r => r.Distribution == motion.Distribution), D(DefaultedSlot.Distribution),
                    motion.Distribution == DistributionValue.Symmetric ? new[] { "A12" } : new string[0]),
                new SummaryRow("Vòng 4", "Tốc độ", $"{N(motion.Speed)} m/s", D(DefaultedSlot.Speed), "A03"),
            };
        }
    }
}
