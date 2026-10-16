using UnityEngine;

public struct Poison { public int Dmg; public int Ticks; }

// Mỗi tick: trừ máu theo Poison, hết lượt thì gỡ Poison.
// Bản demo dùng lambda capture (w, po) → closure mỗi frame; ở đây dùng struct IForEach → 0 alloc.
public sealed class PoisonTickSystem : ISystem
{
    public bool Log;
    World _world;
    int _tick;

    struct Fn : IForEach<Health, Poison>
    {
        public World World;
        public bool Log;
        public int Tick;

        public void Execute(Entity e, ref Health h, ref Poison p)
        {
            h.Value -= p.Dmg;
            p.Ticks--;
            if (Log) Debug.Log($"tick{Tick} {e} hp={h.Value}");
            // Demo gọi po.Remove(e.Id): giờ world đang khoá nên pool.Remove sẽ ném lỗi → dùng world.Remove (hoãn)
            if (p.Ticks == 0) World.Remove<Poison>(e);
        }
    }

    public void OnAwake(World w) { _world = w; }

    public void OnUpdate(float dt)
    {
        _tick++;
        var fn = new Fn { World = _world, Log = Log, Tick = _tick };
        _world.Query<Fn, Health, Poison>(ref fn);
    }
}

// Chạy SAU PoisonTick: entity hết máu thì Destroy.
// Nhờ Commit giữa 2 system, ở đây máu đã trừ xong và Poison hết lượt đã được gỡ.
public sealed class CombatResolveSystem : ISystem
{
    public bool Log;
    public int Killed;
    World _world;
    Filter _withHealth;
    Pool<Health> _health;

    public void OnAwake(World w)
    {
        _world = w;
        _health = w.GetPool<Health>();
        _withHealth = w.Filter().With<Health>();   // tạo 1 lần
    }

    public void OnUpdate(float dt)
    {
        foreach (var e in _withHealth)
        {
            if (_health.Get(e.Id).Value > 0) continue;
            if (Log) Debug.Log($"→ {e} chết");
            _world.Destroy(e);   // hoãn tới Commit sau system này
            Killed++;
        }
    }
}
