using Scellecs.Morpeh;

namespace IgniteSample.Gameplay
{
    // Module Giằng co, port từ prototype "Săn Quái Trên Cát" (Project B).
    // Chỉ phần quyết định thắng thua của trận giằng co; di chuyển, dây, va đá do engine (Mono) lo và gửi vào dưới dạng command.

    public enum HookSpot
    {
        Body,
        Head,
    }

    public enum FightOutcome
    {
        Caught,
        Escaped,
        LineSnapped,
        LineCut,
    }

    public enum FightEventKind
    {
        Started,
        RockHit,
        Exhausted,
        Recovering,
        Enraged,
        Caught,
        Escaped,
        LineSnapped,
        LineCut,
    }

    /// <summary>Hằng số cân bằng, lấy nguyên từ CONFIG.fight / CONFIG.hook / CONFIG.cut của prototype.</summary>
    public sealed class FightConfig
    {
        public float ExhaustWindow = 7f;
        public float HaulTime = 1.2f;
        public float HaulDecay = 0.8f;
        public float RecoverTime = 2.2f;
        public float RockStaminaPct = 0.08f;
        public float SnagLimit = 2f;
        public float SnagDecay = 0.7f;
        public float PullDrain = 1.5f;
        public float HeadDrain = 1.5f;
        public float CutHold = 0.5f;
    }

    /// <summary>Thông số loài quái lúc bị câu (ví dụ Torpedo: thể lực 120, drain 0.5, nặng 1.8).</summary>
    public struct BeastStats : IComponent
    {
        public float MaxStamina;
        public float Drain;
        public float Mass;
        public int Phase; // 1 = hiệp thường, 2 = cuồng nộ
    }

    public struct BeastStamina : IComponent
    {
        public float Current;
    }

    public struct FightLine : IComponent
    {
        public HookSpot Spot;
        public float Snag;
        public float CutHold;
    }

    /// <summary>Input và cảm biến mới nhất của frame, do FightCommandSystem ghi. System khác chỉ đọc.</summary>
    public struct FightControl : IComponent
    {
        public float Steer;
        public bool Pull;
        public bool Haul;
        public bool Cut;
        public int RocksCrossed;
        public float SpeedRatio;
        public bool RockHit;
    }

    // Tag trạng thái: mỗi trận có đúng một trong bốn tag, nhìn thấy trực tiếp trong World Browser.
    public struct Hooked : IComponent { }

    public struct Exhausted : IComponent
    {
        public float Window;
        public float Haul;
    }

    public struct Recovering : IComponent
    {
        public float Remaining;
    }

    public struct FightOver : IComponent
    {
        public FightOutcome Outcome;
    }

    // ---------- Bridge ----------

    /// <summary>Mono → ECS: bắt đầu trận khi móc trúng quái.</summary>
    public readonly struct StartFightCommand
    {
        public readonly BeastStats Stats;
        public readonly HookSpot Spot;

        public StartFightCommand(BeastStats stats, HookSpot spot)
        {
            Stats = stats;
            Spot = spot;
        }
    }

    /// <summary>Mono → ECS: phím người chơi đang giữ (A/D, chuột trái, F, C).</summary>
    public readonly struct FightInputCommand
    {
        public readonly float Steer;
        public readonly bool Pull;
        public readonly bool Haul;
        public readonly bool Cut;

        public FightInputCommand(float steer, bool pull, bool haul, bool cut)
        {
            Steer = steer;
            Pull = pull;
            Haul = haul;
            Cut = cut;
        }
    }

    /// <summary>Engine → ECS: số tảng đá dây đang cắt qua, tốc độ tương đối của quái, và quái vừa tông vỡ đá.</summary>
    public readonly struct FightSenseCommand
    {
        public readonly int RocksCrossed;
        public readonly float SpeedRatio;
        public readonly bool RockHit;

        public FightSenseCommand(int rocksCrossed, float speedRatio, bool rockHit)
        {
            RocksCrossed = rocksCrossed;
            SpeedRatio = speedRatio;
            RockHit = rockHit;
        }
    }

    /// <summary>ECS → Mono. Tự đủ data: view không cần đọc lại component để hiện thông báo.</summary>
    public readonly struct FightEvent
    {
        public readonly Entity Fight;
        public readonly FightEventKind Kind;
        public readonly int Phase;

        public FightEvent(Entity fight, FightEventKind kind, int phase)
        {
            Fight = fight;
            Kind = kind;
            Phase = phase;
        }
    }
}
