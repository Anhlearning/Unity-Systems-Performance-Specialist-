using System;
using Unity.Profiling;
using UnityEngine;

public struct Health { public int Value; }
public struct Dead { }   // tag component: không cần dữ liệu

public class FilterTest : MonoBehaviour
{
    public int entityCount = 10000;

    static readonly ProfilerMarker s_Filter = new ProfilerMarker("ECSMini.Filter");

    World _world;
    Filter _alive;          // tạo 1 lần trong Start, dùng lại mỗi frame
    Pool<Health> _health;
    int _expected, _visited;
    long _lastAlloc, _maxAlloc;
    int _frame;

    void Start()
    {
        _world = new World();
        _health = _world.GetPool<Health>();
        var dead = _world.GetPool<Dead>();

        for (int i = 0; i < entityCount; i++)
        {
            var e = _world.Create();
            bool hasHealth = i % 4 != 0;    // 1/4 entity không có Health
            bool isDead = i % 3 == 0;       // 1/3 entity có Dead
            if (hasHealth) _health.Add(e.Id, new Health { Value = 100 });
            if (isDead) dead.Add(e.Id);
            if (hasHealth && !isDead) _expected++;
        }

        _alive = _world.Filter().With<Health>().Without<Dead>();
    }

    void Update()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        using (s_Filter.Auto())
        {
            _visited = 0;
            foreach (var e in _alive)   // GetEnumerator() trả struct → 0 alloc
            {
                ref var h = ref _health.Get(e.Id);
                h.Value = Mathf.Max(0, h.Value - 1);
                _visited++;
            }
            //             var it = _alive.GetEnumerator();
            // while (it.Next())
            // {
            //     var e = it.Value;
            //     ...
            // }
            // it.Dispose();
        }
        _lastAlloc = GC.GetAllocatedBytesForCurrentThread() - before;
        if (++_frame > 10) _maxAlloc = Math.Max(_maxAlloc, _lastAlloc);   // bỏ qua warm-up
    }

    // OnGUI có cấp phát (tạo string) nhưng nằm NGOÀI vùng đo
    void OnGUI()
    {
        GUI.Label(new Rect(10, 70, 600, 60),
            $"Filter With<Health>.Without<Dead>: {_visited}/{_expected} {(_visited == _expected ? "OK" : "SAI")}\n" +
            $"Alloc frame này: {_lastAlloc} B | Max (sau warm-up): {_maxAlloc} B");
    }
}
