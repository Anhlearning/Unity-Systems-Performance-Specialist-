// Tâm vòng phép: the five elements the center symbol can name (CENTER_001,
// sheet 00). The element is the spell's core.

namespace MagicCircleSim.Design
{
    public enum ElementId { Phong, Moc, Thuy, Hoa, Tho }

    public sealed class ElementDef
    {
        /// <summary>Id shared with the web simulator, so a circle seeds the same cast in both.</summary>
        public readonly string Key;
        public readonly string Name;
        /// <summary>Ink color the circle is drawn in once this element is at its center.</summary>
        public readonly string Color;

        public ElementDef(string key, string name, string color)
        {
            Key = key;
            Name = name;
            Color = color;
        }
    }

    public static class Elements
    {
        public static readonly ElementId[] All = { ElementId.Phong, ElementId.Moc, ElementId.Thuy, ElementId.Hoa, ElementId.Tho };

        static readonly ElementDef[] Defs =
        {
            new ElementDef("phong", "Phong", "#9be7c4"),
            new ElementDef("moc", "Mộc", "#8fd16f"),
            new ElementDef("thuy", "Thủy", "#6cb8ff"),
            new ElementDef("hoa", "Hỏa", "#ff8a4c"),
            new ElementDef("tho", "Thổ", "#d9ad66"),
        };

        public static ElementDef Get(ElementId id) => Defs[(int)id];
    }
}
