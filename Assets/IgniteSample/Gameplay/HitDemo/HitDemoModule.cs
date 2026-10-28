using Ignite.Ecs;
using Scellecs.Morpeh;
using VContainer;

namespace IgniteSample.Gameplay
{
    // Ví dụ nhỏ nhất của pattern Command → System → Event: cube đổi màu khi bị đánh.

    public struct Health : IComponent
    {
        public float Current;
        public float Max;
    }

    /// <summary>Mono → ECS: "đánh entity này". Không ghi thẳng vào Health.</summary>
    public readonly struct HitCommand
    {
        public readonly Entity Target;
        public readonly float Damage;

        public HitCommand(Entity target, float damage)
        {
            Target = target;
            Damage = damage;
        }
    }

    /// <summary>ECS → Mono: tự đủ data để view vẽ, không cần đọc lại component.</summary>
    public readonly struct DamagedEvent
    {
        public readonly Entity Target;
        public readonly float Remaining;
        public readonly float Max;

        public DamagedEvent(Entity target, float remaining, float max)
        {
            Target = target;
            Remaining = remaining;
            Max = max;
        }
    }

    /// <summary>System duy nhất được trừ Health (nguyên tắc "mỗi dữ liệu có một chủ").</summary>
    public sealed class HitSystem : ISystem
    {
        readonly CommandQueue<HitCommand> _commands;
        readonly EventQueue<DamagedEvent> _events;
        BridgeQueue<HitCommand>.Reader _reader;
        Stash<Health> _health;

        public HitSystem(CommandQueue<HitCommand> commands, EventQueue<DamagedEvent> events)
        {
            _commands = commands;
            _events = events;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _health = World.GetStash<Health>();
            _reader = _commands.Claim(nameof(HitSystem));
        }

        public void OnUpdate(float deltaTime)
        {
            while (_reader.TryRead(out var hit))
            {
                if (World.IsDisposed(hit.Target) || !_health.Has(hit.Target))
                    continue;

                ref var health = ref _health.Get(hit.Target);
                health.Current = health.Current - hit.Damage < 0f ? 0f : health.Current - hit.Damage;
                _events.Publish(new DamagedEvent(hit.Target, health.Current, health.Max));
            }
        }

        public void Dispose() { }
    }

    public sealed class HitDemoModule : IGameModule
    {
        public string Name => "HitDemo";

        public void Install(IContainerBuilder builder)
        {
            builder.Register<CommandQueue<HitCommand>>(Lifetime.Singleton);
            builder.Register<EventQueue<DamagedEvent>>(Lifetime.Singleton);
        }

        public void AddSystems(SystemsBuilder systems)
        {
            systems.Add(new HitSystem(systems.Resolve<CommandQueue<HitCommand>>(), systems.Resolve<EventQueue<DamagedEvent>>()));
        }
    }
}
