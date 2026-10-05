// The rune catalog for Vòng 3 (shape and area) and Vòng 4 (motion and
// distribution). Every rune names its source row, so the editor and the summary
// can show where a value came from. Values with no unit in the source are read
// through Assumptions.cs.

using System.Collections.Generic;

namespace MagicCircleSim.Design
{
    public enum FormValue { Disk, Square, Ring, Line }

    public enum PathValue { Orbit, Straight }

    public enum DistributionValue { Even, Symmetric, Random }

    /// <summary>Declared in DEFAULTS order, which is also the order the compiler lists defaulted slots in.</summary>
    public enum RuneCategory { Form, Radius, Angle, Count, Path, Lifetime, Distribution, Speed }

    /// <summary>Vòng 3 runes sit between Vòng 2 and Vòng 3 (V3_R01). Vòng 4 runes sit on Vòng 4.</summary>
    public enum RuneBand { V3, V4 }

    public sealed class RuneDef
    {
        public readonly string Id;
        public readonly RuneCategory Category;
        public readonly string Label;
        /// <summary>Row id in the design workbook, or an assumption id when the source has no value.</summary>
        public readonly string SourceId;
        /// <summary>The value of a radius, angle, count, lifetime or speed rune.</summary>
        public readonly double Number;
        /// <summary>The enum value of a form, path or distribution rune.</summary>
        readonly int choice;

        RuneDef(string id, RuneCategory category, string label, string sourceId, double number, int choice)
        {
            Id = id;
            Category = category;
            Label = label;
            SourceId = sourceId;
            Number = number;
            this.choice = choice;
        }

        public FormValue Form => (FormValue)choice;
        public PathValue Path => (PathValue)choice;
        public DistributionValue Distribution => (DistributionValue)choice;
        public RuneBand Band => Runes.BandOf(Category);

        public static RuneDef Of(string id, FormValue value, string label, string sourceId) =>
            new RuneDef(id, RuneCategory.Form, label, sourceId, 0, (int)value);

        public static RuneDef Of(string id, PathValue value, string label, string sourceId) =>
            new RuneDef(id, RuneCategory.Path, label, sourceId, 0, (int)value);

        public static RuneDef Of(string id, DistributionValue value, string label, string sourceId) =>
            new RuneDef(id, RuneCategory.Distribution, label, sourceId, 0, (int)value);

        public static RuneDef Of(string id, RuneCategory category, double value, string label, string sourceId) =>
            new RuneDef(id, category, label, sourceId, value, 0);
    }

    public static class Runes
    {
        public static readonly RuneCategory[] Categories =
        {
            RuneCategory.Form, RuneCategory.Radius, RuneCategory.Angle, RuneCategory.Count,
            RuneCategory.Path, RuneCategory.Lifetime, RuneCategory.Distribution, RuneCategory.Speed,
        };

        public static readonly IReadOnlyList<RuneDef> All = new[]
        {
            RuneDef.Of("form-disk", FormValue.Disk, "Phân bố hình tròn", "V3_FORM_01"),
            RuneDef.Of("form-square", FormValue.Square, "Phân bố hình vuông", "V3_FORM_02"),
            RuneDef.Of("form-ring", FormValue.Ring, "Phân bố vòng tròn", "V3_FORM_03"),
            RuneDef.Of("form-line", FormValue.Line, "Đường thẳng", "V3_FORM_04"),
            RuneDef.Of("radius-2", RuneCategory.Radius, 2, "2", "A04"),
            RuneDef.Of("radius-4", RuneCategory.Radius, 4, "4", "A04"),
            RuneDef.Of("radius-6", RuneCategory.Radius, 6, "6", "A04"),
            RuneDef.Of("angle-1", RuneCategory.Angle, 1, "1", "V3_ANGLE_01"),
            RuneDef.Of("angle-0.5", RuneCategory.Angle, 0.5, "0.5", "V3_ANGLE_02"),
            RuneDef.Of("count-1", RuneCategory.Count, 1, "1", "V3_COUNT_04"),
            RuneDef.Of("count-2", RuneCategory.Count, 2, "2", "V3_COUNT_01"),
            RuneDef.Of("count-3", RuneCategory.Count, 3, "3", "V3_COUNT_02"),
            RuneDef.Of("count-7", RuneCategory.Count, 7, "7", "V3_COUNT_03"),
            RuneDef.Of("path-orbit", PathValue.Orbit, "Bay theo vòng tròn", "V4_MOVE_01"),
            RuneDef.Of("path-straight", PathValue.Straight, "Bay đường thẳng", "V4_MOVE_02"),
            RuneDef.Of("lifetime-3", RuneCategory.Lifetime, 3, "3", "V4_LIFE_01"),
            RuneDef.Of("lifetime-5", RuneCategory.Lifetime, 5, "5", "V4_LIFE_02"),
            RuneDef.Of("dist-symmetric", DistributionValue.Symmetric, "Đối xứng", "V4_DIST_01"),
            RuneDef.Of("dist-even", DistributionValue.Even, "Đều", "V4_DIST_02"),
            RuneDef.Of("dist-random", DistributionValue.Random, "Ngẫu nhiên", "V4_DIST_03"),
            RuneDef.Of("speed-10", RuneCategory.Speed, 10, "10", "V4_SPEED_01"),
            RuneDef.Of("speed-20", RuneCategory.Speed, 20, "20", "V4_SPEED_02"),
        };

        static readonly Dictionary<string, RuneDef> ById = BuildIndex();

        static Dictionary<string, RuneDef> BuildIndex()
        {
            var index = new Dictionary<string, RuneDef>();
            foreach (var rune in All) index[rune.Id] = rune;
            return index;
        }

        public static RuneBand BandOf(RuneCategory category)
        {
            switch (category)
            {
                case RuneCategory.Form:
                case RuneCategory.Radius:
                case RuneCategory.Angle:
                case RuneCategory.Count:
                    return RuneBand.V3;
                default:
                    return RuneBand.V4;
            }
        }

        public static string CategoryName(RuneCategory category)
        {
            switch (category)
            {
                case RuneCategory.Form: return "Hình dạng";
                case RuneCategory.Radius: return "Bán kính/cạnh";
                case RuneCategory.Angle: return "Góc chiếm diện tích";
                case RuneCategory.Count: return "Số lượng";
                case RuneCategory.Path: return "Hướng di chuyển";
                case RuneCategory.Lifetime: return "Thời gian tồn tại";
                case RuneCategory.Distribution: return "Cách phân bố";
                default: return "Tốc độ";
            }
        }

        /// <summary>The rune with this id, or null when the catalog has none.</summary>
        public static RuneDef Find(string id) => id != null && ById.TryGetValue(id, out var rune) ? rune : null;

        public static List<RuneDef> InCategory(RuneCategory category)
        {
            var runes = new List<RuneDef>();
            foreach (var rune in All)
                if (rune.Category == category) runes.Add(rune);
            return runes;
        }
    }
}
