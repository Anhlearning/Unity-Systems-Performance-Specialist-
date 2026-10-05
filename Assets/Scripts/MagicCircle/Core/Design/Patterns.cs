// Vòng 2, the outward pattern: the shape the element takes outside the circle
// (V2_RULE_001). Each spawned instance wears this shape (assumption A07).

namespace MagicCircleSim.Design
{
    public enum PatternId { Nhon, Vuong, Cau }

    public sealed class PatternDef
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string SourceId;

        public PatternDef(string key, string name, string sourceId)
        {
            Key = key;
            Name = name;
            SourceId = sourceId;
        }
    }

    public static class Patterns
    {
        public static readonly PatternId[] All = { PatternId.Nhon, PatternId.Vuong, PatternId.Cau };

        static readonly PatternDef[] Defs =
        {
            new PatternDef("nhon", "Nhọn", "V2_SHAPE_01"),
            new PatternDef("vuong", "Vuông", "V2_SHAPE_02"),
            new PatternDef("cau", "Cầu", "V2_SHAPE_03"),
        };

        public static PatternDef Get(PatternId id) => Defs[(int)id];
    }
}
