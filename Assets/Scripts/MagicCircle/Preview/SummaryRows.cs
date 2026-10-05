// The compiled spell as readable rows, layer by layer. Pure, so the summary
// panel only renders them. An empty slot reads as a dash: the spell does not
// have that property (A09). The note says why a value is what it is.

using System.Collections.Generic;
using System.Globalization;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;

namespace MagicCircleSim.Preview
{
    public enum NoteKind { None, Required, Ignored, Derived }

    public readonly struct SummaryRow
    {
        public readonly string Layer;
        public readonly string Label;
        public readonly string Value;
        /// <summary>A short tag after the value, or null.</summary>
        public readonly string Note;
        public readonly NoteKind NoteKind;
        /// <summary>Assumption ids that give this value its meaning.</summary>
        public readonly string[] Assumptions;

        public SummaryRow(string layer, string label, string value, string note, NoteKind noteKind, params string[] assumptions)
        {
            Layer = layer;
            Label = label;
            Value = value;
            Note = note;
            NoteKind = noteKind;
            Assumptions = System.Array.FindAll(assumptions, id => id != null);
        }
    }

    public static class SummaryRows
    {
        const string Empty = "—";

        static string N(double value) => value.ToString(CultureInfo.InvariantCulture);

        public static List<SummaryRow> Of(SpellIR spell)
        {
            var shape = spell.Shape;
            var motion = spell.Motion;

            SummaryRow Rune(string layer, RuneCategory category, string value, params string[] assumptions)
            {
                var rune = spell.RuneIn(category);
                var label = Runes.CategoryName(category);
                if (rune == null) return new SummaryRow(layer, label, Empty, null, NoteKind.None, assumptions);
                if (spell.IsIgnored(category)) return new SummaryRow(layer, label, rune.Label, "chưa có tác dụng", NoteKind.Ignored, assumptions);
                return new SummaryRow(layer, label, value ?? rune.Label, null, NoteKind.None, assumptions);
            }

            var rows = new List<SummaryRow>
            {
                spell.Element.HasValue
                    ? new SummaryRow("Tâm", "Nguyên tố", Elements.Get(spell.Element.Value).Name, null, NoteKind.None)
                    : new SummaryRow("Tâm", "Nguyên tố", Empty, "bắt buộc", NoteKind.Required, "A14"),
                spell.Nature.HasValue
                    ? new SummaryRow("Vòng 1", "Bản chất", Natures.Get(spell.Nature.Value).Name, null, NoteKind.None)
                    : new SummaryRow("Vòng 1", "Bản chất", Empty, "bắt buộc", NoteKind.Required, "A14"),
                new SummaryRow("Vòng 1", "→ Vật chất", spell.Material.HasValue ? Materials.Get(spell.Material.Value).Name : Empty, null, NoteKind.None),
                spell.Pattern.HasValue
                    ? new SummaryRow("Vòng 2", "Pattern", Patterns.Get(spell.Pattern.Value).Name,
                        shape.HasValue && shape.Value.Fills ? "chưa có tác dụng" : null, shape.HasValue && shape.Value.Fills ? NoteKind.Ignored : NoteKind.None, "A07")
                    : new SummaryRow("Vòng 2", "Pattern", Empty, "không có lõi", NoteKind.Derived, "A18"),
                shape.HasValue
                    ? Rune("Vòng 3", RuneCategory.Form, null, "A06")
                    : new SummaryRow("Vòng 3", Runes.CategoryName(RuneCategory.Form), Empty, "hiện ở tâm vòng", NoteKind.Derived, "A15"),
            };

            if (shape.HasValue && spell.RuneIn(RuneCategory.Radius) == null)
                rows.Add(new SummaryRow("Vòng 3", Runes.CategoryName(RuneCategory.Radius), $"{N(shape.Value.Radius)} m", "theo vòng phép", NoteKind.Derived, "A16"));
            else
                rows.Add(Rune("Vòng 3", RuneCategory.Radius, shape.HasValue ? $"{N(shape.Value.Radius)} m" : null, "A04"));

            rows.Add(Rune("Vòng 3", RuneCategory.Angle,
                shape.HasValue ? $"{N(shape.Value.AngleFraction)} ({N(shape.Value.AngleFraction * 360)}°)" : null, "A01", "A11"));

            if (shape.HasValue && shape.Value.Fills)
                rows.Add(new SummaryRow("Vòng 3", Runes.CategoryName(RuneCategory.Count), Empty, "phủ kín vùng", NoteKind.Derived, "A17"));
            else
                rows.Add(Rune("Vòng 3", RuneCategory.Count, null));

            rows.Add(Rune("Vòng 4", RuneCategory.Path, null, motion.Path == PathValue.Orbit ? "A13" : "A08", "A19"));

            if (motion.Sustained)
                rows.Add(new SummaryRow("Vòng 4", Runes.CategoryName(RuneCategory.Lifetime), Empty, "duy trì đến khi dừng", NoteKind.Derived, "A20"));
            else
                rows.Add(Rune("Vòng 4", RuneCategory.Lifetime, $"{N(motion.LifetimeS.Value)} s", "A02"));

            var laysOut = shape.HasValue && !shape.Value.Fills && shape.Value.Count > 1;
            if (laysOut && spell.RuneIn(RuneCategory.Distribution) == null)
                rows.Add(new SummaryRow("Vòng 4", Runes.CategoryName(RuneCategory.Distribution), "Đều", "theo nguồn", NoteKind.Derived, "A09"));
            else
                rows.Add(Rune("Vòng 4", RuneCategory.Distribution, null, motion.Distribution == DistributionValue.Symmetric ? "A12" : null));

            rows.Add(Rune("Vòng 4", RuneCategory.Speed, motion.Speed.HasValue ? $"{N(motion.Speed.Value)} m/s" : null, "A03"));
            return rows;
        }
    }
}
