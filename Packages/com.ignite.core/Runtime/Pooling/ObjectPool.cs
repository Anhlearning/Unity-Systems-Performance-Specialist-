using System;
using System.Collections.Generic;

namespace Ignite.Core.Pooling
{
    /// <summary>Object nhận callback khi được lấy ra hoặc trả về pool.</summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>Pool C# thuần. Sau khi Prewarm đủ, Get/Release không sinh rác.</summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Stack<T> _free;
        readonly Func<T> _create;
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;
        readonly Action<T> _onDestroy;
        readonly int _maxSize;

        public ObjectPool(
            Func<T> create,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int initialCapacity = 16,
            int maxSize = 1024)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            _free = new Stack<T>(initialCapacity);
        }

        /// <summary>Số object đã tạo và còn thuộc pool (đang dùng + đang rảnh).</summary>
        public int CountAll { get; private set; }
        public int CountInactive => _free.Count;
        public int CountActive => CountAll - _free.Count;

        public void Prewarm(int count)
        {
            for (int i = _free.Count; i < count && i < _maxSize; i++)
            {
                _free.Push(_create());
                CountAll++;
            }
        }

        public T Get()
        {
            T item;
            if (_free.Count > 0)
            {
                item = _free.Pop();
            }
            else
            {
                item = _create();
                CountAll++;
            }

            _onGet?.Invoke(item);
            if (item is IPoolable poolable)
                poolable.OnSpawn();
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item is IPoolable poolable)
                poolable.OnDespawn();
            _onRelease?.Invoke(item);

            if (_free.Count >= _maxSize)
            {
                _onDestroy?.Invoke(item);
                CountAll--;
                return;
            }

            _free.Push(item);
        }

        public void Clear()
        {
            while (_free.Count > 0)
            {
                _onDestroy?.Invoke(_free.Pop());
                CountAll--;
            }
        }
    }
}
