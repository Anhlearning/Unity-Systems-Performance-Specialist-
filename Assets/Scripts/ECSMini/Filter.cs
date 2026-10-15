using System;
using System.Collections.Generic;

// Bộ lọc entity: With<T>() = bắt buộc CÓ component, Without<T>() = bắt buộc KHÔNG có.
// Filter là class → tạo 1 lần (Start) rồi dùng lại; new mỗi frame sẽ alloc.
public sealed class Filter
{
    readonly World _world;
    readonly List<IPool> _include = new();
    readonly List<IPool> _exclude = new();

    internal Filter(World world) { _world = world; }

    public Filter With<T>() where T : struct { _include.Add(_world.GetPool<T>()); return this; }
    public Filter Without<T>() where T : struct { _exclude.Add(_world.GetPool<T>()); return this; }

    // foreach tìm GetEnumerator() theo pattern, không cần IEnumerable<T>.
    // Trả về struct → foreach gọi thẳng MoveNext/Current, không boxing.
    public Enumerator GetEnumerator()
    {
        // Driver = pool NHỎ NHẤT trong include; chọn lúc bắt đầu duyệt vì Count đổi theo frame
        IPool driver = null;
        for (int i = 0; i < _include.Count; i++)
            if (driver == null || _include[i].Count < driver.Count) driver = _include[i];
        _world.BeginIteration();
        return new Enumerator(this, driver);
    }

    bool Match(int id, IPool driver)
    {
        for (int i = 0; i < _include.Count; i++)
        {
            var p = _include[i];
            if (p != driver && !p.Has(id)) return false;
        }
        for (int i = 0; i < _exclude.Count; i++)
            if (_exclude[i].Has(id)) return false;
        return true;
    }

    // IDisposable: foreach tự gọi Dispose() khi kết thúc (kể cả break/exception),
    // với struct thì gọi trực tiếp (constrained call) → không boxing.
    public struct Enumerator : IDisposable
    {
        readonly Filter _filter;
        readonly IPool _driver;
        int _index;
        int _id;

        internal Enumerator(Filter filter, IPool driver)
        {
            _filter = filter;
            _driver = driver;
            _index = driver != null ? driver.Count : 0;   // không có With<> → duyệt rỗng
            _id = -1;
        }

        public Entity Current => new Entity(_id, _filter._world.GenOf(_id));

        public bool MoveNext()
        {
            while (--_index >= 0)
            {                                             // duyệt NGƯỢC: swap-remove không làm sót entity
                if (_index >= _driver.Count) continue;    // pool bị remove bớt trong lúc duyệt
                _id = _driver.EntityAt(_index);
                if (_filter.Match(_id, _driver)) return true;
            }
            return false;
        }

        public void Dispose() => _filter._world.EndIteration();
    }
}
