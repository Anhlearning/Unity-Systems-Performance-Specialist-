using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void RefAction<T1, T2>(Entity entity, ref T1 arg1, ref T2 arg2);
public interface IForEach<T1, T2> where T1 : struct where T2 : struct
{
    void Execute(Entity entity, ref T1 c1, ref T2 c2);
}

// Lệnh bị hoãn trong lúc duyệt. Là struct → List<DelayedOp> không sinh rác (khác List<Action>).
enum DelayedOpKind : byte { Add, Remove, Destroy }
struct DelayedOp
{
    public DelayedOpKind Kind;
    public Entity Entity;
    public IPool Pool;     // pool cần Add/Remove (null với Destroy)
    public int Slot;       // Add: vị trí giá trị trong Pool<T>._staged
}

public sealed class World
{
    int[] _gens = new int[64];                 // thế hệ hiện tại của từng id
    readonly Stack<int> _free = new();          // id chờ tái sử dụng (LIFO)
    int _nextId;
    readonly Dictionary<Type, IPool> _pools = new();
    int _iterating;                               // > 0 = đang trong query
    readonly List<DelayedOp> _delayedOps = new();
    internal bool IsIterating => _iterating > 0;
    public Entity Create()
    {
        int id = _free.Count > 0 ? _free.Pop() : _nextId++;
        if (id >= _gens.Length) Array.Resize(ref _gens, _gens.Length * 2);
        if (_gens[id] == 0) _gens[id] = 1;          // gen 0 = "chưa từng sống"
        return new Entity(id, _gens[id]);
    }
    public bool IsAlive(Entity e) => e.Id < _nextId && _gens[e.Id] == e.Gen;
    public void Destroy(Entity e)
    {
        if (!IsAlive(e)) return;
        if (_iterating > 0)
        {   // hoãn
            _delayedOps.Add(new DelayedOp { Kind = DelayedOpKind.Destroy, Entity = e });
            return;
        }
        foreach (var p in _pools.Values) p.RemoveIfHas(e.Id);
        _gens[e.Id]++;                              // mọi handle cũ lập tức "chết"
        _free.Push(e.Id);
    }
    public Pool<T> GetPool<T>() where T : struct
    {
        if (!_pools.TryGetValue(typeof(T), out var p)) _pools[typeof(T)] = p = new Pool<T>(this);
        return (Pool<T>)p;
    }

    // ===== 6) ADD/REMOVE an toàn: đang duyệt → hoãn tới khi duyệt xong =====
    public void Add<T>(Entity e, in T value = default) where T : struct
    {
        if (!IsAlive(e)) return;
        var pool = GetPool<T>();
        if (_iterating == 0) { pool.Add(e.Id, value); return; }
        if (pool.Has(e.Id)) throw new InvalidOperationException("đã có");   // báo lỗi ngay, không đợi lúc áp dụng
        _delayedOps.Add(new DelayedOp { Kind = DelayedOpKind.Add, Entity = e, Pool = pool, Slot = pool.Stage(value) });
    }

    public void Remove<T>(Entity e) where T : struct
    {
        if (!IsAlive(e)) return;
        var pool = GetPool<T>();
        if (_iterating == 0) { pool.RemoveIfHas(e.Id); return; }
        _delayedOps.Add(new DelayedOp { Kind = DelayedOpKind.Remove, Entity = e, Pool = pool });
    }
    // ===== 4) QUERY: duyệt pool NHO ̉ HƠN =====
    public void Query<TFn, T1, T2>(ref TFn fn)
    where TFn : struct, IForEach<T1, T2>
    where T1 : struct where T2 : struct
    {
        var p1 = GetPool<T1>(); var p2 = GetPool<T2>();
        bool p1Drives = p1.Count <= p2.Count;          // duyệt pool NHỎ HƠN
        int n = p1Drives ? p1.Count : p2.Count;
        _iterating++;
        for (int i = n - 1; i >= 0; i--)
        {                                               // duyệt NGƯỢC
            int id = p1Drives ? p1.EntityAt(i) : p2.EntityAt(i);
            if (p1Drives ? !p2.Has(id) : !p1.Has(id)) continue;
            fn.Execute(new Entity(id, _gens[id]), ref p1.Get(id), ref p2.Get(id));
        }
        EndIteration();
    }

    // Bản delegate cũ vẫn giữ, chỉ bọc lại để dùng chung một vòng lặp
    struct DelegateFn<T1, T2> : IForEach<T1, T2> where T1 : struct where T2 : struct
    {
        public RefAction<T1, T2> Action;
        public void Execute(Entity e, ref T1 a, ref T2 b) => Action(e, ref a, ref b);
    }

    public void Query<T1, T2>(RefAction<T1, T2> action) where T1 : struct where T2 : struct
    {
        var fn = new DelegateFn<T1, T2> { Action = action };
        Query<DelegateFn<T1, T2>, T1, T2>(ref fn);
    }

    // ===== 5) FILTER: With<>/Without<>, duyệt bằng foreach =====
    public Filter Filter() => new Filter(this);
    internal int GenOf(int id) => _gens[id];
    internal void BeginIteration() => _iterating++;

    internal void EndIteration()
    {
        if (--_iterating == 0) Commit();   // còn vòng duyệt / khoá bên ngoài → chưa áp dụng
    }

    // ===== 7) COMMIT (kiểu Morpeh): áp dụng mọi Add/Remove/Destroy đang hoãn =====
    // Tự chạy khi lớp khoá ngoài cùng kết thúc (foreach, Query, hoặc 1 system trong SystemsGroup).
    public void Commit()
    {
        if (_iterating > 0) throw new InvalidOperationException("Không Commit giữa lúc đang duyệt");

        // Áp dụng ĐÚNG THỨ TỰ đã gọi. _iterating == 0 nên mọi lệnh chạy thẳng, không hoãn tiếp.
        for (int i = 0; i < _delayedOps.Count; i++)
        {
            var op = _delayedOps[i];
            bool alive = IsAlive(op.Entity);   // entity có thể đã bị Destroy bởi lệnh trước đó
            switch (op.Kind)
            {
                case DelayedOpKind.Add: op.Pool.ApplyDelayedAdd(alive ? op.Entity.Id : -1, op.Slot); break;
                case DelayedOpKind.Remove: if (alive) op.Pool.RemoveIfHas(op.Entity.Id); break;
                case DelayedOpKind.Destroy: Destroy(op.Entity); break;
            }
        }
        _delayedOps.Clear();
    }
}