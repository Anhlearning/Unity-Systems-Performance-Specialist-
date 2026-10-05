// SpellCompiler: a MagicCircle into a SpellIR. This is where the design's
// rules are enforced. It runs a fixed sequence: check the required layers, read
// the runes and check them, then resolve each layer from what is actually on
// the circle. Nothing is filled in (A09): a rune that needs another rune to mean
// something is reported, not given a partner.
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
            var ignored = new List<RuneCategory>();

            if (!circle.Center.HasValue)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.MissingCenter, Severity.Error,
                    "Chưa chọn Tâm: hãy đặt một ký hiệu nguyên tố vào tâm vòng phép."));
            }
            if (!circle.Nature.HasValue)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.MissingNature, Severity.Error,
                    "Chưa chọn Vòng 1: hãy chọn bản chất (Tròn, Vuông, Đặc biệt) để biết nguyên tố hiện ra thành vật chất gì (A14)."));
            }

            var byCategory = GroupRunes(circle.Runes, diagnostics, out var categoryOrder);
            CheckOnePerCategory(byCategory, categoryOrder, diagnostics);
            var placed = new Dictionary<RuneCategory, RuneDef>();
            foreach (var category in categoryOrder) placed[category] = byCategory[category][0];

            RuneDef Rune(RuneCategory category) => placed.TryGetValue(category, out var rune) ? rune : null;

            void Ignore(RuneCategory category, DiagnosticKey key, Severity severity, string why)
            {
                if (Rune(category) == null) return;
                ignored.Add(category);
                diagnostics.Add(new Diagnostic(key, severity, $"Rune “{Runes.CategoryName(category)}” chưa có tác dụng: {why}"));
            }

            SpellShape? shape = null;
            var form = Rune(RuneCategory.Form);
            if (form == null)
            {
                foreach (var category in new[] { RuneCategory.Radius, RuneCategory.Angle, RuneCategory.Count })
                {
                    Ignore(category, DiagnosticKey.NeedsForm, Severity.Info, "cần có rune Hình dạng ở Vòng 3 (A15).");
                }
            }
            else
            {
                var round = form.Form == FormValue.Disk || form.Form == FormValue.Ring;
                var angle = Rune(RuneCategory.Angle);
                if (angle != null && !round)
                {
                    ignored.Add(RuneCategory.Angle);
                    diagnostics.Add(new Diagnostic(DiagnosticKey.AngleIgnored, Severity.Info, "Góc chiếm diện tích chỉ áp dụng cho hình tròn và vòng tròn (A11)."));
                }
                shape = new SpellShape(
                    form.Form,
                    Rune(RuneCategory.Radius)?.Number ?? Defaults.CircleRadiusM,
                    round && angle != null ? angle.Number : 1,
                    (int?)Rune(RuneCategory.Count)?.Number);
            }

            if (shape == null || shape.Value.Fills)
            {
                Ignore(RuneCategory.Distribution, DiagnosticKey.NeedsCount, Severity.Info, "cần có Hình dạng và Số lượng để có thứ mà phân bố.");
            }
            if (circle.Pattern.HasValue && shape.HasValue && shape.Value.Fills)
            {
                diagnostics.Add(new Diagnostic(DiagnosticKey.PatternNeedsCount, Severity.Info,
                    "Vòng 2 tạo hình cho từng instance, nhưng vật chất đang phủ kín cả vùng. Thêm rune Số lượng để thấy pattern (A07, A17)."));
            }

            var path = Rune(RuneCategory.Path);
            var speed = Rune(RuneCategory.Speed);
            var moves = path != null && speed != null;
            if (path != null && speed == null)
            {
                Ignore(RuneCategory.Path, DiagnosticKey.MotionIncomplete, Severity.Warning, "cần thêm rune Tốc độ ở Vòng 4, hiện phép đứng yên (A19).");
            }
            if (speed != null && path == null)
            {
                Ignore(RuneCategory.Speed, DiagnosticKey.MotionIncomplete, Severity.Warning, "cần thêm rune Hướng di chuyển ở Vòng 4, hiện phép đứng yên (A19).");
            }
            var motion = new SpellMotion(
                moves ? path.Path : (PathValue?)null,
                Rune(RuneCategory.Lifetime)?.Number,
                Rune(RuneCategory.Distribution)?.Distribution ?? Defaults.Distribution,
                moves ? speed.Number : (double?)null);

            var valid = true;
            foreach (var d in diagnostics)
                if (d.Severity == Severity.Error) valid = false;

            return new SpellIR
            {
                Valid = valid,
                Diagnostics = diagnostics,
                Element = circle.Center,
                Nature = circle.Nature,
                Material = circle.Center.HasValue && circle.Nature.HasValue ? Natures.MaterialOf(circle.Nature.Value, circle.Center.Value) : (MaterialId?)null,
                Pattern = circle.Pattern,
                Shape = shape,
                Motion = motion,
                Placed = placed,
                Ignored = ignored,
                Seed = Fnv1a.Hash(SeedKey(circle.Center, circle.Nature, circle.Pattern, circle.Runes)),
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

        /// <summary>
        /// The web build's <c>JSON.stringify([center, nature, pattern, sortedRunes])</c>, so a
        /// circle seeds the same cast in both. Sorted by code unit, as JS sorts strings.
        /// </summary>
        public static string SeedKey(ElementId? center, NatureId? nature, PatternId? pattern, IReadOnlyList<string> runes)
        {
            var sorted = new List<string>(runes);
            sorted.Sort(string.CompareOrdinal);
            var key = new StringBuilder("[");
            if (center.HasValue) JsonString(key, Elements.Get(center.Value).Key);
            else key.Append("null");
            key.Append(',');
            if (nature.HasValue) JsonString(key, Natures.Get(nature.Value).Key);
            else key.Append("null");
            key.Append(',');
            if (pattern.HasValue) JsonString(key, Patterns.Get(pattern.Value).Key);
            else key.Append("null");
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
