using System;
using Unity.Profiling;
using UnityEngine;

public struct Marked { public int Frame; }

// Mỗi frame: duyệt entity CHƯA có Marked → Add<Marked>; rồi duyệt entity CÓ Marked → Remove<Marked>.
// Lệnh Add/Remove trong lúc duyệt bị hoãn tới khi foreach kết thúc → vòng lặp không bị xáo trộn.
public class DelayedOpsTest : MonoBehaviour
{
    public int entityCount = 10000;

    static readonly ProfilerMarker s_Ops = new ProfilerMarker("ECSMini.DelayedOps");

    World _world;
    Filter _unmarked, _marked;
    Pool<Marked> _markedPool;
    int _added, _removed, _countDuringLoop, _countAfterAdd, _countAfterRemove;
    long _lastAlloc, _maxAlloc;
    int _frame;

    void Start()
    {
        _world = new World();
        var health = _world.GetPool<Health>();
        _markedPool = _world.GetPool<Marked>();
        for (int i = 0; i < entityCount; i++)
            health.Add(_world.Create().Id, new Health { Value = 100 });

        _unmarked = _world.Filter().With<Health>().Without<Marked>();
        _marked = _world.Filter().With<Marked>();
    }

    void Update()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        using (s_Ops.Auto())
        {
            _added = _removed = 0;
            foreach (var e in _unmarked)
            {
                _world.Add(e, new Marked { Frame = _frame });   // hoãn
                _added++;
                _countDuringLoop = _markedPool.Count;   // vẫn là 0: chưa Add thật
            }
            _countAfterAdd = _markedPool.Count;

            foreach (var e in _marked)
            {
                _world.Remove<Marked>(e);   // hoãn
                _removed++;
            }
            _countAfterRemove = _markedPool.Count;
        }
        _lastAlloc = GC.GetAllocatedBytesForCurrentThread() - before;
        if (++_frame > 10) _maxAlloc = Math.Max(_maxAlloc, _lastAlloc);   // bỏ qua warm-up
    }

    void OnGUI()
    {
        bool ok = _added == entityCount && _countDuringLoop == 0 && _countAfterAdd == entityCount
               && _removed == entityCount && _countAfterRemove == 0;
        GUI.Label(new Rect(10, 130, 700, 80),
            $"DelayedOps: added {_added} (trong lúc duyệt Marked.Count = {_countDuringLoop}) → sau duyệt {_countAfterAdd} | removed {_removed} → Marked.Count {_countAfterRemove}  {(ok ? "OK" : "SAI")}\n" +
            $"Alloc frame này: {_lastAlloc} B | Max (sau warm-up): {_maxAlloc} B");
    }
}
