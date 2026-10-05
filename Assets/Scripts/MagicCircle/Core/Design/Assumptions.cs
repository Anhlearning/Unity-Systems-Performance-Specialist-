// Every decision the design workbook leaves open, in one place. Sheet 08 lists
// what the source does not settle. The simulator still has to run, so each gap
// gets a named assumption here, tied to its REV id, and nothing else in the
// code invents a rule. Change a value here once the design confirms it.

using System.Collections.Generic;

namespace MagicCircleSim.Design
{
    public sealed class Assumption
    {
        public readonly string Id;
        /// <summary>Sheet 08 row this answers, when there is one.</summary>
        public readonly string Rev;
        public readonly string Text;

        public Assumption(string id, string rev, string text)
        {
            Id = id;
            Rev = rev;
            Text = text;
        }
    }

    public static class Assumptions
    {
        public static readonly IReadOnlyList<Assumption> All = new[]
        {
            new Assumption("A01", "REV05", "Góc chiếm diện tích là tỉ lệ của vòng tròn: 1 = 360°, 0.5 = 180°. Hình quạt hướng về phía trước người cast."),
            new Assumption("A02", "REV06", "Thời gian tồn tại tính bằng giây, đếm từ lúc phép hiện hình xong."),
            new Assumption("A03", "REV06", "Tốc độ tính bằng m/s (1 đơn vị thế giới = 1 m). Khi bay vòng tròn, đây là tốc độ dài."),
            new Assumption("A04", "REV04", "Nguồn chưa có giá trị bán kính/cạnh, tạm dùng rune 2 / 4 / 6 m. Với hình vuông, giá trị là độ dài cạnh."),
            new Assumption("A05", null, "Đường thẳng nằm ngang qua vị trí cast, dài gấp đôi bán kính."),
            new Assumption("A06", "REV03", "“Phân bố hình tròn” rải bên trong hình tròn, “Phân bố vòng tròn” rải trên viền, theo dòng nguồn 4."),
            new Assumption("A07", null, "Mỗi instance mang hình dạng của Vòng 2 (Nhọn = mũi nhọn, Vuông = khối hộp, Cầu = khối cầu)."),
            new Assumption("A08", null, "“Bay đường thẳng”: mọi instance bay song song về phía trước người cast."),
            new Assumption("A09", null, "Thiếu rune thì dùng mặc định (xem DEFAULTS). Chỉ “phân bố đều” là mặc định có trong nguồn (V4_RULE_001)."),
            new Assumption("A10", null, "Mỗi loại rune chỉ đặt được một cái. Nguồn chỉ nói rõ điều này cho rune hình dạng (V3_R02)."),
            new Assumption("A11", null, "Góc chiếm diện tích chỉ áp dụng cho hình tròn và vòng tròn (“biến hình tròn thành hình quạt”)."),
            new Assumption("A12", null, "“Đối xứng”: các instance đối xứng gương qua trục phía trước, vị trí lấy ngẫu nhiên có seed."),
            new Assumption("A13", null, "Bay theo vòng tròn quanh vị trí cast với bán kính tối thiểu 1 m, để instance ở tâm vẫn quay."),
        };

        public static string TextOf(string id)
        {
            foreach (var assumption in All)
                if (assumption.Id == id) return assumption.Text;
            return null;
        }
    }

    /// <summary>What a spell uses when its circle leaves a slot empty (A09).</summary>
    public static class Defaults
    {
        public const NatureId Nature = NatureId.Tron;
        public const PatternId Pattern = PatternId.Cau;
        public const FormValue Form = FormValue.Disk;
        public const double Radius = 2;
        public const double Angle = 1;
        public const int Count = 1;
        public const PathValue Path = PathValue.Straight;
        public const double Lifetime = 3;
        public const DistributionValue Distribution = DistributionValue.Even;
        public const double Speed = 10;

        /// <summary>A13.</summary>
        public const double MinOrbitRadiusM = 1;
    }
}
