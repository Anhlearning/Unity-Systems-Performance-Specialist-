// Vòng 1, the element's nature: how it shows itself in the world
// (V1_RULE_001). The ring's own form (round, square, special) crossed with the
// center element names one material.

namespace MagicCircleSim.Design
{
    public enum NatureId { Tron, Vuong, DacBiet }

    public sealed class NatureDef
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string Review;

        public NatureDef(string key, string name, string review = null)
        {
            Key = key;
            Name = name;
            Review = review;
        }
    }

    public static class Natures
    {
        public static readonly NatureId[] All = { NatureId.Tron, NatureId.Vuong, NatureId.DacBiet };

        static readonly NatureDef[] Defs =
        {
            new NatureDef("tron", "Tròn"),
            new NatureDef("vuong", "Vuông"),
            new NatureDef("dac-biet", "Hình đặc biệt?", "REV02"),
        };

        /// <summary>Sheet 01 verbatim: row is the ring form, column the element (Phong, Mộc, Thủy, Hỏa, Thổ).</summary>
        static readonly MaterialId[,] MaterialTable =
        {
            { MaterialId.Gio, MaterialId.DayLeo, MaterialId.Nuoc, MaterialId.Lua, MaterialId.CatDat },
            { MaterialId.Loc, MaterialId.ThanCung, MaterialId.Bang, MaterialId.Laser, MaterialId.Da },
            { MaterialId.SongAm, MaterialId.DangHoa, MaterialId.HoiNuoc, MaterialId.Loi, MaterialId.KimLoai },
        };

        public static NatureDef Get(NatureId id) => Defs[(int)id];

        public static MaterialId MaterialOf(NatureId nature, ElementId element) => MaterialTable[(int)nature, (int)element];
    }
}
