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
            new Assumption("A09", null, "Ô nào để trống thì phép không có thuộc tính đó, không gán giá trị mặc định. Ngoại lệ duy nhất là “phân bố đều”, mặc định có trong nguồn (V4_RULE_001)."),
            new Assumption("A10", null, "Mỗi loại rune chỉ đặt được một cái. Nguồn chỉ nói rõ điều này cho rune hình dạng (V3_R02)."),
            new Assumption("A11", null, "Góc chiếm diện tích chỉ áp dụng cho hình tròn và vòng tròn (“biến hình tròn thành hình quạt”)."),
            new Assumption("A12", null, "“Đối xứng”: các instance đối xứng gương qua trục phía trước, vị trí lấy ngẫu nhiên có seed."),
            new Assumption("A13", null, "Bay theo vòng tròn quanh vị trí cast với bán kính tối thiểu 1 m, để instance ở tâm vẫn quay."),
            new Assumption("A14", null, "Bắt buộc có Tâm và Vòng 1: vật chất là Vòng 1 × Tâm (sheet 01), thiếu một trong hai thì chưa biết phép hiện ra thứ gì."),
            new Assumption("A15", null, "Không có rune hình dạng thì vật chất hiện ngay trên vòng phép, một khối duy nhất ở tâm. Bán kính, góc và số lượng cần có hình dạng mới có tác dụng."),
            new Assumption("A16", null, "Có hình dạng nhưng không có rune bán kính thì vùng xuất hiện lấy đúng bán kính vòng phép dưới đất (2.5 m)."),
            new Assumption("A17", null, "Có hình dạng nhưng không có rune số lượng thì vật chất phủ kín cả vùng, không tách thành instance."),
            new Assumption("A18", null, "Không có Vòng 2 thì không có lõi rắn: chỉ có bản thân vật chất (lửa, nước, gió...)."),
            new Assumption("A19", null, "Phép chỉ di chuyển khi có cả hướng di chuyển và tốc độ. Thiếu một trong hai thì phép đứng yên."),
            new Assumption("A20", null, "Không có rune thời gian tồn tại thì phép duy trì đến khi người chơi dừng nó."),
        };

        public static string TextOf(string id)
        {
            foreach (var assumption in All)
                if (assumption.Id == id) return assumption.Text;
            return null;
        }
    }

    /// <summary>
    /// The only values the cast reads when a rune is absent. None of them gives the
    /// spell a property its circle does not have (A09): an empty slot stays empty.
    /// </summary>
    public static class Defaults
    {
        /// <summary>V4_RULE_001: the source's own default for laying out several instances.</summary>
        public const DistributionValue Distribution = DistributionValue.Even;

        /// <summary>A13.</summary>
        public const double MinOrbitRadiusM = 1;

        /// <summary>A16: the magic circle's own radius on the ground, meters.</summary>
        public const double CircleRadiusM = 2.5;
    }
}
