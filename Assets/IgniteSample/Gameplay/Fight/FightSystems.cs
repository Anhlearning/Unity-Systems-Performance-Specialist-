using Ignite.Ecs;
using Scellecs.Morpeh;

namespace IgniteSample.Gameplay
{
    /// <summary>Kết thúc trận: gỡ mọi tag trạng thái, gắn FightOver, phát event. Dùng chung cho các system.</summary>
    sealed class FightEnder
    {
        readonly Stash<Hooked> _hooked;
        readonly Stash<Exhausted> _exhausted;
        readonly Stash<Recovering> _recovering;
        readonly Stash<FightOver> _over;
        readonly Stash<BeastStats> _stats;
        readonly EventQueue<FightEvent> _events;

        public FightEnder(World world, EventQueue<FightEvent> events)
        {
            _hooked = world.GetStash<Hooked>();
            _exhausted = world.GetStash<Exhausted>();
            _recovering = world.GetStash<Recovering>();
            _over = world.GetStash<FightOver>();
            _stats = world.GetStash<BeastStats>();
            _events = events;
        }

        public bool IsOver(Entity fight) => _over.Has(fight);

        public void End(Entity fight, FightOutcome outcome)
        {
            if (_over.Has(fight))
                return;

            _hooked.Remove(fight);
            _exhausted.Remove(fight);
            _recovering.Remove(fight);
            _over.Set(fight, new FightOver { Outcome = outcome });
            _events.Publish(new FightEvent(fight, ToKind(outcome), _stats.Get(fight).Phase));
        }

        static FightEventKind ToKind(FightOutcome outcome)
        {
            switch (outcome)
            {
                case FightOutcome.Caught: return FightEventKind.Caught;
                case FightOutcome.Escaped: return FightEventKind.Escaped;
                case FightOutcome.LineSnapped: return FightEventKind.LineSnapped;
                default: return FightEventKind.LineCut;
            }
        }
    }

    /// <summary>1. Móc trúng quái → tạo entity trận đấu.</summary>
    public sealed class StartFightSystem : ISystem
    {
        readonly CommandQueue<StartFightCommand> _commands;
        readonly EventQueue<FightEvent> _events;
        BridgeQueue<StartFightCommand>.Reader _reader;

        public StartFightSystem(CommandQueue<StartFightCommand> commands, EventQueue<FightEvent> events)
        {
            _commands = commands;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake() => _reader = _commands.Claim(nameof(StartFightSystem));

        public void OnUpdate(float deltaTime)
        {
            while (_reader.TryRead(out var start))
            {
                var fight = World.CreateEntity();
                var stats = start.Stats;
                if (stats.Phase == 0)
                    stats.Phase = 1;

                World.GetStash<BeastStats>().Set(fight, stats);
                World.GetStash<BeastStamina>().Set(fight, new BeastStamina { Current = stats.MaxStamina });
                World.GetStash<FightLine>().Set(fight, new FightLine { Spot = start.Spot });
                World.GetStash<FightControl>().Set(fight, new FightControl { SpeedRatio = 1f });
                World.GetStash<Hooked>().Set(fight);
                _events.Publish(new FightEvent(fight, FightEventKind.Started, stats.Phase));
            }
        }

        public void Dispose() { }
    }

    /// <summary>2. Gom command input + cảm biến của frame vào FightControl. System duy nhất ghi FightControl.</summary>
    public sealed class FightCommandSystem : ISystem
    {
        readonly CommandQueue<FightInputCommand> _input;
        readonly CommandQueue<FightSenseCommand> _sense;
        BridgeQueue<FightInputCommand>.Reader _inputReader;
        BridgeQueue<FightSenseCommand>.Reader _senseReader;
        Filter _fights;
        Stash<FightControl> _control;

        public FightCommandSystem(CommandQueue<FightInputCommand> input, CommandQueue<FightSenseCommand> sense)
        {
            _input = input;
            _sense = sense;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _inputReader = _input.Claim(nameof(FightCommandSystem));
            _senseReader = _sense.Claim(nameof(FightCommandSystem));
            _fights = World.Filter.With<FightControl>().Without<FightOver>().Build();
            _control = World.GetStash<FightControl>();
        }

        public void OnUpdate(float deltaTime)
        {
            bool hasInput = false, hasSense = false, rockHit = false;
            FightInputCommand input = default;
            FightSenseCommand sense = default;

            while (_inputReader.TryRead(out var i)) { input = i; hasInput = true; }
            while (_senseReader.TryRead(out var s)) { sense = s; hasSense = true; rockHit |= s.RockHit; }

            foreach (var fight in _fights)
            {
                ref var control = ref _control.Get(fight);
                if (hasInput)
                {
                    control.Steer = input.Steer;
                    control.Pull = input.Pull;
                    control.Haul = input.Haul;
                    control.Cut = input.Cut;
                }
                if (hasSense)
                {
                    control.RocksCrossed = sense.RocksCrossed;
                    control.SpeedRatio = sense.SpeedRatio;
                }
                control.RockHit = rockHit;
            }
        }

        public void Dispose() { }
    }

    /// <summary>3. Quái bị câu mất sức theo tốc độ, móc đầu, ghì cần; tông đá mất 8%. Hết sức → Exhausted.</summary>
    public sealed class HookedStaminaSystem : ISystem
    {
        readonly FightConfig _config;
        readonly EventQueue<FightEvent> _events;
        Filter _hooked;
        Stash<BeastStats> _stats;
        Stash<BeastStamina> _stamina;
        Stash<FightLine> _line;
        Stash<FightControl> _control;
        Stash<Hooked> _hookedTag;
        Stash<Exhausted> _exhausted;

        public HookedStaminaSystem(FightConfig config, EventQueue<FightEvent> events)
        {
            _config = config;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _hooked = World.Filter.With<Hooked>().With<BeastStats>().With<BeastStamina>().With<FightControl>().Build();
            _stats = World.GetStash<BeastStats>();
            _stamina = World.GetStash<BeastStamina>();
            _line = World.GetStash<FightLine>();
            _control = World.GetStash<FightControl>();
            _hookedTag = World.GetStash<Hooked>();
            _exhausted = World.GetStash<Exhausted>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _hooked)
            {
                ref var stats = ref _stats.Get(fight);
                ref var stamina = ref _stamina.Get(fight);
                ref var control = ref _control.Get(fight);

                float speed = control.SpeedRatio;
                float drain = stats.Drain * (0.5f + speed * speed)
                    * (_line.Get(fight).Spot == HookSpot.Head ? _config.HeadDrain : 1f)
                    * (control.Pull ? _config.PullDrain : 1f);
                stamina.Current -= drain * deltaTime;

                if (control.RockHit)
                {
                    float afterRock = stamina.Current - stats.MaxStamina * _config.RockStaminaPct;
                    stamina.Current = afterRock < 0.01f ? 0.01f : afterRock; // tông đá không bao giờ tự làm kiệt sức
                    _events.Publish(new FightEvent(fight, FightEventKind.RockHit, stats.Phase));
                }

                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    _hookedTag.Remove(fight);
                    _exhausted.Set(fight, new Exhausted { Window = _config.ExhaustWindow, Haul = 0f });
                    _events.Publish(new FightEvent(fight, FightEventKind.Exhausted, stats.Phase));
                }
            }
        }

        public void Dispose() { }
    }

    /// <summary>4. Dây cắt qua đá khi quái còn khoẻ thì tích luỹ; đủ ngưỡng là đứt. Không cắt qua đá thì nhả dần.</summary>
    public sealed class LineSnagSystem : ISystem
    {
        readonly FightConfig _config;
        readonly EventQueue<FightEvent> _events;
        Filter _fights;
        Stash<FightLine> _line;
        Stash<FightControl> _control;
        Stash<Hooked> _hooked;
        FightEnder _ender;

        public LineSnagSystem(FightConfig config, EventQueue<FightEvent> events)
        {
            _config = config;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _fights = World.Filter.With<FightLine>().With<FightControl>().Without<FightOver>().Build();
            _line = World.GetStash<FightLine>();
            _control = World.GetStash<FightControl>();
            _hooked = World.GetStash<Hooked>();
            _ender = new FightEnder(World, _events);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _fights)
            {
                ref var line = ref _line.Get(fight);
                ref var control = ref _control.Get(fight);

                if (control.RocksCrossed > 0 && _hooked.Has(fight))
                {
                    float speed = control.SpeedRatio < 0f ? 0f : (control.SpeedRatio > 1.2f ? 1.2f : control.SpeedRatio);
                    int rocks = control.RocksCrossed > 2 ? 2 : control.RocksCrossed;
                    line.Snag += deltaTime * rocks * (0.5f + speed);
                }
                else
                {
                    line.Snag -= deltaTime * _config.SnagDecay;
                    if (line.Snag < 0f)
                        line.Snag = 0f;
                }

                if (line.Snag >= _config.SnagLimit)
                    _ender.End(fight, FightOutcome.LineSnapped);
            }
        }

        public void Dispose() { }
    }

    /// <summary>5. Quái kiệt sức: giữ F đủ HaulTime là bắt được. Hết cửa sổ: hiệp 1 → hồi sức, hiệp 2 → quái trốn.</summary>
    public sealed class ExhaustedSystem : ISystem
    {
        readonly FightConfig _config;
        readonly EventQueue<FightEvent> _events;
        Filter _exhaustedFights;
        Stash<Exhausted> _exhausted;
        Stash<Recovering> _recovering;
        Stash<FightControl> _control;
        Stash<BeastStats> _stats;
        FightEnder _ender;

        public ExhaustedSystem(FightConfig config, EventQueue<FightEvent> events)
        {
            _config = config;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _exhaustedFights = World.Filter.With<Exhausted>().With<FightControl>().With<BeastStats>().Without<FightOver>().Build();
            _exhausted = World.GetStash<Exhausted>();
            _recovering = World.GetStash<Recovering>();
            _control = World.GetStash<FightControl>();
            _stats = World.GetStash<BeastStats>();
            _ender = new FightEnder(World, _events);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _exhaustedFights)
            {
                if (_ender.IsOver(fight))
                    continue;

                ref var exhausted = ref _exhausted.Get(fight);
                exhausted.Window -= deltaTime;

                if (_control.Get(fight).Haul)
                    exhausted.Haul += deltaTime / _config.HaulTime;
                else
                    exhausted.Haul = exhausted.Haul - deltaTime * _config.HaulDecay < 0f ? 0f : exhausted.Haul - deltaTime * _config.HaulDecay;

                if (exhausted.Haul >= 1f)
                {
                    _ender.End(fight, FightOutcome.Caught);
                    continue;
                }

                if (exhausted.Window > 0f)
                    continue;

                int phase = _stats.Get(fight).Phase;
                if (phase == 1)
                {
                    _exhausted.Remove(fight);
                    _recovering.Set(fight, new Recovering { Remaining = _config.RecoverTime });
                    _events.Publish(new FightEvent(fight, FightEventKind.Recovering, phase));
                }
                else
                {
                    _ender.End(fight, FightOutcome.Escaped);
                }
            }
        }

        public void Dispose() { }
    }

    /// <summary>6. Hồi sức: thể lực đầy dần, xong thì sang hiệp 2 (cuồng nộ) và lại bị câu.</summary>
    public sealed class RecoverSystem : ISystem
    {
        readonly FightConfig _config;
        readonly EventQueue<FightEvent> _events;
        Filter _recoveringFights;
        Stash<Recovering> _recovering;
        Stash<BeastStats> _stats;
        Stash<BeastStamina> _stamina;
        Stash<Hooked> _hooked;

        public RecoverSystem(FightConfig config, EventQueue<FightEvent> events)
        {
            _config = config;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _recoveringFights = World.Filter.With<Recovering>().With<BeastStats>().With<BeastStamina>().Without<FightOver>().Build();
            _recovering = World.GetStash<Recovering>();
            _stats = World.GetStash<BeastStats>();
            _stamina = World.GetStash<BeastStamina>();
            _hooked = World.GetStash<Hooked>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _recoveringFights)
            {
                ref var recovering = ref _recovering.Get(fight);
                ref var stats = ref _stats.Get(fight);
                ref var stamina = ref _stamina.Get(fight);

                recovering.Remaining -= deltaTime;
                float left = recovering.Remaining > 0f ? recovering.Remaining : 0f;
                stamina.Current = stats.MaxStamina * (1f - left / _config.RecoverTime);

                if (recovering.Remaining > 0f)
                    continue;

                stamina.Current = stats.MaxStamina;
                stats.Phase = 2;
                _recovering.Remove(fight);
                _hooked.Set(fight);
                _events.Publish(new FightEvent(fight, FightEventKind.Enraged, stats.Phase));
            }
        }

        public void Dispose() { }
    }

    /// <summary>7. Giữ C đủ CutHold giây là cắt dây thả quái.</summary>
    public sealed class CutLineSystem : ISystem
    {
        readonly FightConfig _config;
        readonly EventQueue<FightEvent> _events;
        Filter _fights;
        Stash<FightLine> _line;
        Stash<FightControl> _control;
        FightEnder _ender;

        public CutLineSystem(FightConfig config, EventQueue<FightEvent> events)
        {
            _config = config;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _fights = World.Filter.With<FightLine>().With<FightControl>().Without<FightOver>().Build();
            _line = World.GetStash<FightLine>();
            _control = World.GetStash<FightControl>();
            _ender = new FightEnder(World, _events);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _fights)
            {
                if (_ender.IsOver(fight))
                    continue;

                ref var line = ref _line.Get(fight);
                if (_control.Get(fight).Cut)
                {
                    line.CutHold += deltaTime;
                    if (line.CutHold >= _config.CutHold)
                        _ender.End(fight, FightOutcome.LineCut);
                }
                else
                {
                    line.CutHold = 0f;
                }
            }
        }

        public void Dispose() { }
    }
}
