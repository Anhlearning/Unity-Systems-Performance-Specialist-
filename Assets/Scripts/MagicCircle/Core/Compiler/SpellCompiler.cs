// SpellCompiler: a MagicCircle into a SpellIR. This is where the design's
// rules are enforced. It runs a fixed sequence: read the runes and check them,
// fill the empty slots from Defaults, then resolve the material.
//
//   var spell = SpellCompiler.Compile(circle);
//   if (spell.Valid) stage.Cast(spell);

using System.Collections.Generic;
using System.Text;
using MagicCircleSim.Circle;
using MagicCircleSim.Design;

namespace MagicCircleSim.Compiler
{
    public static class SpellCompiler
    {
        public static SpellIR Compile(MagicCircle circle)
        {
            var diagnostics = new List<Diagnostic>();
            var defaulted = new List<DefaultedSlot>();

            if (!circle.Center.HasValue)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.MissingCenter, Severity.Error, "Tâm vòng phép chưa có ký hiệu nguyên tố."));
            }

            var byCategory = GroupRunes(circle.Runes, diagnostics, out var categoryOrder);
            CheckOnePerCategory(byCategory, categoryOrder, diagnostics);

            RuneDef Pick(RuneCategory category)
            {
                if (byCategory.TryGetValue(category, out var runes)) return runes[0];
                defaulted.Add(SlotOf(category));
                return null;
            }

            var nature = circle.Nature ?? Defaults.Nature;
            if (!circle.Nature.HasValue) defaulted.Add(DefaultedSlot.Nature);
            var pattern = circle.Pattern ?? Defaults.Pattern;
            if (!circle.Pattern.HasValue) defaulted.Add(DefaultedSlot.Pattern);

            var form = Pick(RuneCategory.Form)?.Form ?? Defaults.Form;
            var angle = Pick(RuneCategory.Angle)?.Number ?? Defaults.Angle;
            if ((form == FormValue.Square || form == FormValue.Line) && byCategory.ContainsKey(RuneCategory.Angle) && angle < 1)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.AngleIgnored, Severity.Info, "Góc chiếm diện tích chỉ áp dụng cho hình tròn và vòng tròn (A11)."));
            }

            var radius = Pick(RuneCategory.Radius)?.Number ?? Defaults.Radius;
            var count = (int?)Pick(RuneCategory.Count)?.Number ?? Defaults.Count;
            var shape = new SpellShape(form, radius, angle, count);
            var motion = new SpellMotion(
                Pick(RuneCategory.Path)?.Path ?? Defaults.Path,
                Pick(RuneCategory.Lifetime)?.Number ?? Defaults.Lifetime,
                Pick(RuneCategory.Distribution)?.Distribution ?? Defaults.Distribution,
                Pick(RuneCategory.Speed)?.Number ?? Defaults.Speed);

            defaulted.Sort();
            if (defaulted.Count > 0)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.Defaulted, Severity.Info, "Một số ô còn trống nên dùng giá trị mặc định (A09)."));
            }

            var valid = true;
            foreach (var d in diagnostics)
                if (d.Severity == Severity.Error) valid = false;

            return new SpellIR
            {
                Valid = valid,
                Diagnostics = diagnostics,
                Element = circle.Center,
                Nature = nature,
                Material = circle.Center.HasValue ? Natures.MaterialOf(nature, circle.Center.Value) : (MaterialId?)null,
                Pattern = pattern,
                Shape = shape,
                Motion = motion,
                Defaulted = defaulted,
                Seed = Fnv1a.Hash(SeedKey(circle.Center, nature, pattern, circle.Runes)),
            };
        }

        static Dictionary<RuneCategory, List<RuneDef>> GroupRunes(IReadOnlyList<string> ids, List<Diagnostic> diagnostics, out List<RuneCategory> order)
        {
            var byCategory = new Dictionary<RuneCategory, List<RuneDef>>();
            order = new List<RuneCategory>();
            foreach (var id in ids)
            {
                var rune = Runes.Find(id);
                if (rune == null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticKey.UnknownRune, Severity.Error, $"Rune không có trong catalog: {id}"));
                    continue;
                }
                if (!byCategory.TryGetValue(rune.Category, out var runes))
                {
                    byCategory[rune.Category] = runes = new List<RuneDef>();
                    order.Add(rune.Category);
                }
                runes.Add(rune);
            }
            return byCategory;
        }

        static void CheckOnePerCategory(Dictionary<RuneCategory, List<RuneDef>> byCategory, List<RuneCategory> order, List<Diagnostic> diagnostics)
        {
            foreach (var category in order)
            {
                if (byCategory[category].Count < 2) continue;
                diagnostics.Add(category == RuneCategory.Form
                    ? new Diagnostic(DiagnosticKey.MultipleShapeRunes, Severity.Error, "Vòng 3 chỉ đặt được một rune hình dạng (V3_R02).")
                    : new Diagnostic(DiagnosticKey.DuplicateRune, Severity.Error, $"Có nhiều hơn một rune “{Runes.CategoryName(category)}” (A10)."));
            }
        }

        static DefaultedSlot SlotOf(RuneCategory category)
        {
            switch (category)
            {
                case RuneCategory.Form: return DefaultedSlot.Form;
                case RuneCategory.Radius: return DefaultedSlot.Radius;
                case RuneCategory.Angle: return DefaultedSlot.Angle;
                case RuneCategory.Count: return DefaultedSlot.Count;
                case RuneCategory.Path: return DefaultedSlot.Path;
                case RuneCategory.Lifetime: return DefaultedSlot.Lifetime;
                case RuneCategory.Distribution: return DefaultedSlot.Distribution;
                default: return DefaultedSlot.Speed;
            }
        }

        /// <summary>
        /// The web build's <c>JSON.stringify([center, nature, pattern, sortedRunes])</c>, so a
        /// circle seeds the same cast in both. Sorted by code unit, as JS sorts strings.
        /// </summary>
        public static string SeedKey(ElementId? center, NatureId nature, PatternId pattern, IReadOnlyList<string> runes)
        {
            var sorted = new List<string>(runes);
            sorted.Sort(string.CompareOrdinal);
            var key = new StringBuilder("[");
            if (center.HasValue) JsonString(key, Elements.Get(center.Value).Key);
            else key.Append("null");
            key.Append(',');
            JsonString(key, Natures.Get(nature).Key);
            key.Append(',');
            JsonString(key, Patterns.Get(pattern).Key);
            key.Append(",[");
            for (var i = 0; i < sorted.Count; i++)
            {
                if (i > 0) key.Append(',');
                JsonString(key, sorted[i]);
            }
            return key.Append("]]").ToString();
        }

        static void JsonString(StringBuilder into, string value)
        {
            into.Append('"');
            foreach (var c in value)
            {
                if (c == '"' || c == '\\') into.Append('\\').Append(c);
                else if (c < 0x20) into.Append("\\u").Append(((int)c).ToString("x4"));
                else into.Append(c);
            }
            into.Append('"');
        }
    }
}
