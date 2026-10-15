using System;
using Unity.Profiling;
using UnityEngine;

public struct Position { public Vector3 Value; }
public struct Velocity { public Vector3 Value; }

// Cách A: struct implement IForEach
struct MoveFn : IForEach<Position, Velocity>
{
    public float Dt;
    public int Visited;   // state ghi ngược lại caller nhờ ref
    public void Execute(Entity e, ref Position p, ref Velocity v)
    {
        p.Value += v.Value * Dt;
        Visited++;
    }
}

public class ZeroAllocQueryTest : MonoBehaviour
{
    public enum Mode { StructFn, CachedDelegate, CapturingLambda_BAD }
    public Mode mode = Mode.StructFn;
    public int entityCount = 10000;

    static readonly ProfilerMarker s_Query = new ProfilerMarker("ECSMini.Query");

    World _world;
    RefAction<Position, Velocity> _cachedMove;   // Cách B: cache delegate vào field
    float _dt;
    long _lastAlloc, _maxAlloc;
    int _frame;
    Mode _prevMode;

    void Start()
    {
        _world = new World();
        var pos = _world.GetPool<Position>();
        var vel = _world.GetPool<Velocity>();
        for (int i = 0; i < entityCount; i++)
        {
            var e = _world.Create();
            pos.Add(e.Id);
            vel.Add(e.Id, new Velocity { Value = UnityEngine.Random.insideUnitSphere });
        }
        _cachedMove = MoveCached;   // tạo delegate đúng 1 lần
    }

    void MoveCached(Entity e, ref Position p, ref Velocity v) => p.Value += v.Value * _dt;

    void Update()
    {
        if (mode != _prevMode) { _prevMode = mode; _maxAlloc = 0; _frame = 0; }
        float dt = Time.deltaTime;

        long before = GC.GetAllocatedBytesForCurrentThread();
        using (s_Query.Auto())   // AutoScope là struct → không cấp phát
        {
            switch (mode)
            {
                case Mode.StructFn:
                    {
                        var fn = new MoveFn { Dt = dt };
                        _world.Query<MoveFn, Position, Velocity>(ref fn);
                        break;
                    }
                case Mode.CachedDelegate:
                    _dt = dt;
                    _world.Query(_cachedMove);
                    break;
                case Mode.CapturingLambda_BAD:
                    RunCapturingLambda(dt);   // tách hàm: closure chỉ được tạo khi chạy case này
                    break;
            }
        }
        _lastAlloc = GC.GetAllocatedBytesForCurrentThread() - before;

        if (++_frame > 10) _maxAlloc = Math.Max(_maxAlloc, _lastAlloc);   // bỏ qua warm-up
    }

    // Để so sánh: lambda dùng biến dt → compiler tạo closure (DisplayClass) + delegate mỗi lần gọi.
    // PHẢI nằm ở hàm riêng: nếu để trong Update, closure bị new ở ĐẦU Update cho mọi mode.
    void RunCapturingLambda(float dt)
    {
        _world.Query<Position, Velocity>(
            (Entity e, ref Position p, ref Velocity v) => p.Value += v.Value * dt);
    }

    // OnGUI có cấp phát (tạo string) nhưng nằm NGOÀI vùng đo nên không ảnh hưởng kết quả
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 600, 60),
            $"Mode: {mode}\nAlloc frame này: {_lastAlloc} B | Max (sau warm-up): {_maxAlloc} B");
    }
}
