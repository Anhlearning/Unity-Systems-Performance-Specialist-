using Ignite.Ecs;
using Newtonsoft.Json.Linq;
using Scellecs.Morpeh;

namespace IgniteSample.Gameplay
{
    /// <summary>Tiến trình săn quái, lưu vào file save (một entity duy nhất trong World).</summary>
    public struct FightProgress : IComponent
    {
        public int Caught;
        public int Escaped;
        public int LinesLost;
    }

    /// <summary>Đánh dấu trận đã được tính vào FightProgress, để không đếm hai lần.</summary>
    public struct FightCounted : IComponent { }

    public static class FightProgressAccess
    {
        public static ref FightProgress GetOrCreate(World world)
        {
            var stash = world.GetStash<FightProgress>();
            var filter = world.Filter.With<FightProgress>().Build();
            foreach (var existing in filter)
                return ref stash.Get(existing);

            var entity = world.CreateEntity();
            stash.Set(entity, default);
            world.Commit();
            return ref stash.Get(entity);
        }
    }

    /// <summary>8. Trận kết thúc → cộng vào FightProgress đúng một lần.</summary>
    public sealed class FightProgressSystem : ISystem
    {
        Filter _finished;
        Stash<FightOver> _over;
        Stash<FightCounted> _counted;

        public World World { get; set; }

        public void OnAwake()
        {
            _finished = World.Filter.With<FightOver>().Without<FightCounted>().Build();
            _over = World.GetStash<FightOver>();
            _counted = World.GetStash<FightCounted>();
            FightProgressAccess.GetOrCreate(World);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var fight in _finished)
            {
                ref var progress = ref FightProgressAccess.GetOrCreate(World);
                switch (_over.Get(fight).Outcome)
                {
                    case FightOutcome.Caught: progress.Caught++; break;
                    case FightOutcome.Escaped: progress.Escaped++; break;
                    default: progress.LinesLost++; break;
                }
                _counted.Set(fight);
            }
        }

        public void Dispose() { }
    }

    /// <summary>Lưu/đọc FightProgress cho FightModule.</summary>
    static class FightSave
    {
        public static JToken Save(World world)
        {
            var progress = FightProgressAccess.GetOrCreate(world);
            return new JObject
            {
                ["caught"] = progress.Caught,
                ["escaped"] = progress.Escaped,
                ["linesLost"] = progress.LinesLost,
            };
        }

        public static void Load(World world, JToken data)
        {
            ref var progress = ref FightProgressAccess.GetOrCreate(world);
            progress.Caught = data.Value<int?>("caught") ?? 0;
            progress.Escaped = data.Value<int?>("escaped") ?? 0;
            progress.LinesLost = data.Value<int?>("linesLost") ?? 0;
        }
    }
}
