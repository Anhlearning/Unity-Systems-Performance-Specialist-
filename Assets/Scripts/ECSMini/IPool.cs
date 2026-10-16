using System;
using UnityEngine;

public interface IPool
{
    bool Has(int id);
    void RemoveIfHas(int id);
    int Count { get; }
    int EntityAt(int index);
    void ApplyDelayedAdd(int id, int slot);   // id < 0 = entity đã chết → bỏ qua, chỉ giải phóng slot
}

public sealed class Pool<T> : IPool where T : struct
{ //sealed = ko cho kế thừa
    readonly World _world;
    int[] _sparse = new int[64];     // entityId -> denseIndex + 1 (0 = không có) -- lưu index trong _densenTold +1 || Index trong _sparse = entityID -> giá trị -> vị trí trong denseTold và trong Data
    int[] _denseToId = new int[64];  // denseIndex -> entityId
    T[] _data = new T[64];             // denseIndex -> component (liền mạch)
    int _count;

    // Giá trị của các lệnh Add bị hoãn. DelayedOp chỉ lưu số slot → không cần generic, không boxing.
    T[] _staged = new T[8];
    int _stagedCount, _stagedUsed;

    internal Pool(World world) { _world = world; }

    public int Count => _count;
    public int EntityAt(int i) => _denseToId[i];
    public bool Has(int id) => id < _sparse.Length && _sparse[id] != 0;
    public ref T Add(int id, in T value = default)
    {
        if (_world.IsIterating) throw new InvalidOperationException("Đang duyệt: dùng world.Add<T>() để hoãn");
        if (Has(id)) throw new InvalidOperationException("đã có");
        if (id >= _sparse.Length) Array.Resize(ref _sparse, Math.Max(id + 1, _sparse.Length * 2));
        if (_count == _data.Length)
        {
            Array.Resize(ref _data, _count * 2);
            Array.Resize(ref _denseToId, _count * 2);
        }
        _data[_count] = value; // mảng data
        _denseToId[_count] = id; // lưu vị trí data thuộc về entityId nào
        _sparse[id] = ++_count;  // lưu vị trí data của entity ID
        return ref _data[_count - 1];
    }
    public ref T Get(int id) => ref _data[_sparse[id] - 1];
    public void Remove(int id)
    {             // swap-remove, O(1)
        if (_world.IsIterating) throw new InvalidOperationException("Đang duyệt: dùng world.Remove<T>() để hoãn");
        int idx = _sparse[id] - 1, last = _count - 1;
        _data[idx] = _data[last];
        _denseToId[idx] = _denseToId[last];
        _sparse[_denseToId[idx]] = idx + 1;
        _data[last] = default;
        _sparse[id] = 0;
        _count--;
    }
    public void RemoveIfHas(int id) { if (Has(id)) Remove(id); }

    // ===== Hoãn Add: cất giá trị vào _staged, trả về số slot =====
    internal int Stage(in T value)
    {
        if (_stagedCount == _staged.Length) Array.Resize(ref _staged, _stagedCount * 2);
        _staged[_stagedCount] = value;
        return _stagedCount++;
    }

    public void ApplyDelayedAdd(int id, int slot)
    {
        if (id >= 0)
        {
            if (Has(id)) Get(id) = _staged[slot];   // 2 lệnh Add cùng entity trong 1 lượt duyệt → lệnh sau ghi đè
            else Add(id, _staged[slot]);
        }
        _staged[slot] = default;
        if (++_stagedUsed == _stagedCount) _stagedCount = _stagedUsed = 0;   // dùng hết → tái sử dụng mảng từ đầu
    }
}
