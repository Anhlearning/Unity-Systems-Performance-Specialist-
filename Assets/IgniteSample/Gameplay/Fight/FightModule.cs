using Ignite.Ecs;
using Newtonsoft.Json.Linq;
using Scellecs.Morpeh;
using VContainer;

namespace IgniteSample.Gameplay
{
    /// <summary>
    /// Module Giằng co. Thay cho FightBridge.Awake tự dựng World ở Project B: World giờ do EcsWorldHost tạo,
    /// module chỉ khai báo queue và thứ tự system.
    /// </summary>
    public sealed class FightModule : IGameModule, ISaveableModule
    {
        readonly FightConfig _config;

        public FightModule(FightConfig config = null)
        {
            _config = config ?? new FightConfig();
        }

        public string Name => "Fight";

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);
            builder.Register<CommandQueue<StartFightCommand>>(Lifetime.Singleton);
            builder.Register<CommandQueue<FightInputCommand>>(Lifetime.Singleton);
            builder.Register<CommandQueue<FightSenseCommand>>(Lifetime.Singleton);
            builder.Register<EventQueue<FightEvent>>(Lifetime.Singleton);
        }

        public void AddSystems(SystemsBuilder systems)
        {
            var events = systems.Resolve<EventQueue<FightEvent>>();

            // Thứ tự chạy, đọc từ trên xuống. Giống thứ tự trong Game.updateFight của prototype.
            systems
                .Add(new StartFightSystem(systems.Resolve<CommandQueue<StartFightCommand>>(), events))
                .Add(new FightCommandSystem(systems.Resolve<CommandQueue<FightInputCommand>>(), systems.Resolve<CommandQueue<FightSenseCommand>>()))
                .Add(new HookedStaminaSystem(_config, events))
                .Add(new LineSnagSystem(_config, events))
                .Add(new ExhaustedSystem(_config, events))
                .Add(new RecoverSystem(_config, events))
                .Add(new CutLineSystem(_config, events))
                .Add(new FightProgressSystem());
        }

        public string SaveKey => "fight";
        public JToken Save(World world) => FightSave.Save(world);
        public void Load(World world, JToken data) => FightSave.Load(world, data);
    }

    /// <summary>Thông số mẫu của các loài, lấy từ SPECIES trong prototype.</summary>
    public static class BeastPresets
    {
        public static BeastStats Torpedo => new BeastStats { MaxStamina = 120f, Drain = 0.5f, Mass = 1.8f, Phase = 1 };
        public static BeastStats Serpent => new BeastStats { MaxStamina = 90f, Drain = 0.5f, Mass = 0.5f, Phase = 1 };
        public static BeastStats Leviathan => new BeastStats { MaxStamina = 320f, Drain = 0.5f, Mass = 4.5f, Phase = 1 };
    }
}
