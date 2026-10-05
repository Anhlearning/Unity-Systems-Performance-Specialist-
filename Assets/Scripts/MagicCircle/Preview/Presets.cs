// Sample circles for the preset bar, one per idea worth showing off. They are
// plain MagicCircle values, the same thing the palette builds.

using System.Collections.Generic;
using MagicCircleSim.Circle;
using MagicCircleSim.Design;

namespace MagicCircleSim.Preview
{
    public static class Presets
    {
        public static readonly IReadOnlyList<(string name, MagicCircle circle)> All = new[]
        {
            ("Hoả cầu", new MagicCircle(ElementId.Hoa, NatureId.Tron, PatternId.Cau,
                new[] { "form-disk", "count-1", "path-straight", "speed-10", "lifetime-3" })),
            ("Quạt laser", new MagicCircle(ElementId.Hoa, NatureId.Vuong, PatternId.Nhon,
                new[] { "form-ring", "radius-2", "angle-0.5", "count-7", "dist-even", "path-straight", "speed-20", "lifetime-3" })),
            ("Vòng băng xoay", new MagicCircle(ElementId.Thuy, NatureId.Vuong, PatternId.Nhon,
                new[] { "form-ring", "radius-4", "angle-1", "count-7", "dist-even", "path-orbit", "speed-10", "lifetime-5" })),
            ("Lôi trận", new MagicCircle(ElementId.Hoa, NatureId.DacBiet, PatternId.Cau,
                new[] { "form-disk", "radius-6", "count-7", "dist-random", "path-orbit", "speed-10", "lifetime-3" })),
            ("Bức tường đá", new MagicCircle(ElementId.Tho, NatureId.Vuong, PatternId.Vuong,
                new[] { "form-line", "radius-4", "count-7", "dist-even", "path-straight", "speed-10", "lifetime-5" })),
            ("Hoa vũ", new MagicCircle(ElementId.Moc, NatureId.DacBiet, PatternId.Cau,
                new[] { "form-square", "radius-6", "count-7", "dist-symmetric", "path-orbit", "speed-10", "lifetime-5" })),
        };
    }
}
