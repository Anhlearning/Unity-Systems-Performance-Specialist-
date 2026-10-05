// The fifteen materials an element can manifest as. Vòng 1 picks the row, the
// center picks the column (sheet 01_V1_Nguyen_to). Names are verbatim from the
// source, including the ones still under review.

namespace MagicCircleSim.Design
{
    public enum MaterialId
    {
        Gio, DayLeo, Nuoc, Lua, CatDat,
        Loc, ThanCung, Bang, Laser, Da,
        SongAm, DangHoa, HoiNuoc, Loi, KimLoai,
    }

    public sealed class MaterialDef
    {
        public readonly string Key;
        public readonly string Name;
        /// <summary>Sheet 08 id when the source marks this name as unconfirmed.</summary>
        public readonly string Review;

        public MaterialDef(string key, string name, string review = null)
        {
            Key = key;
            Name = name;
            Review = review;
        }
    }

    public static class Materials
    {
        static readonly MaterialDef[] Defs =
        {
            new MaterialDef("gio", "Gió"),
            new MaterialDef("day-leo", "Dạng dây leo"),
            new MaterialDef("nuoc", "Nước"),
            new MaterialDef("lua", "Lửa"),
            new MaterialDef("cat-dat", "Cát, Đất"),
            new MaterialDef("loc", "Lốc?", "REV01"),
            new MaterialDef("than-cung", "Dạng thân cứng"),
            new MaterialDef("bang", "Băng"),
            new MaterialDef("laser", "Laser"),
            new MaterialDef("da", "Đá"),
            new MaterialDef("song-am", "Sóng âm"),
            new MaterialDef("dang-hoa", "Dạng hoa"),
            new MaterialDef("hoi-nuoc", "Hơi nước"),
            new MaterialDef("loi", "Lôi"),
            new MaterialDef("kim-loai", "Kim loại"),
        };

        public static int Count => Defs.Length;

        public static MaterialDef Get(MaterialId id) => Defs[(int)id];
    }
}
