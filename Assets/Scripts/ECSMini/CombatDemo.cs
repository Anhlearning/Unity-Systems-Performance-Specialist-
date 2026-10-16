using System;
using Unity.Profiling;
using UnityEngine;

public class CombatDemo : MonoBehaviour
{
    public int stressCount = 10000;

    static readonly ProfilerMarker s_Systems = new ProfilerMarker("ECSMini.Systems");

    SystemsGroup _stress;
    long _lastAlloc, _maxAlloc;
    int _frame;

    void Start()
    {
        RunScenario();
        BuildStress();
    }

    // ===== Kịch bản giống demo: a, c trúng độc; b không =====
    void RunScenario()
    {
        var w = new World();
        var hp = w.GetPool<Health>(); var po = w.GetPool<Poison>();
        var a = w.Create(); hp.Add(a.Id, new Health { Value = 12 }); po.Add(a.Id, new Poison { Dmg = 5, Ticks = 3 });
        var b = w.Create(); hp.Add(b.Id, new Health { Value = 100 });
        var c = w.Create(); hp.Add(c.Id, new Health { Value = 8 });  po.Add(c.Id, new Poison { Dmg = 5, Ticks = 3 });

        var group = new SystemsGroup(w)
            .Add(new PoisonTickSystem { Log = true })
            .Add(new CombatResolveSystem { Log = true });
        group.Awake();
        for (int tick = 1; tick <= 3; tick++) group.Update(1f);

        var d = w.Create();   // tái sử dụng id của a (a chết sau cùng → nằm đầu stack _free)
        Debug.Log($"a alive={w.IsAlive(a)}  c alive={w.IsAlive(c)}  b hp={hp.Get(b.Id).Value}  d={d}");

        bool ok = !w.IsAlive(a) && !w.IsAlive(c) && hp.Get(b.Id).Value == 100
               && d.Id == a.Id && d.Gen == a.Gen + 1;
        Debug.Log(ok ? "CombatDemo: PASS" : "CombatDemo: FAIL");
    }

    // ===== Stress: chứng minh SystemsGroup + Commit không cấp phát mỗi frame =====
    // Poison hết lượt → bị gỡ → ReapplyPoison gắn lại → có Add/Remove thật mỗi vài frame.
    void BuildStress()
    {
        var w = new World();
        var hp = w.GetPool<Health>(); var po = w.GetPool<Poison>();
        for (int i = 0; i < stressCount; i++)
        {
            var e = w.Create();
            hp.Add(e.Id, new Health { Value = int.MaxValue });
            po.Add(e.Id, new Poison { Dmg = 1, Ticks = 1 + i % 3 });
        }
        _stress = new SystemsGroup(w)
            .Add(new PoisonTickSystem())
            .Add(new CombatResolveSystem())
            .Add(new ReapplyPoisonSystem());
        _stress.Awake();
    }

    void Update()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        using (s_Systems.Auto())
            _stress.Update(Time.deltaTime);
        _lastAlloc = GC.GetAllocatedBytesForCurrentThread() - before;
        if (++_frame > 10) _maxAlloc = Math.Max(_maxAlloc, _lastAlloc);   // bỏ qua warm-up
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 210, 600, 40),
            $"SystemsGroup ({stressCount} entity): alloc frame này {_lastAlloc} B | Max (sau warm-up): {_maxAlloc} B");
    }

    sealed class ReapplyPoisonSystem : ISystem
    {
        World _world;
        Filter _clean;

        public void OnAwake(World w)
        {
            _world = w;
            _clean = w.Filter().With<Health>().Without<Poison>();
        }

        public void OnUpdate(float dt)
        {
            foreach (var e in _clean) _world.Add(e, new Poison { Dmg = 1, Ticks = 3 });
        }
    }
}
