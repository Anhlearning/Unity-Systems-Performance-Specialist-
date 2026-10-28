using System;
using System.Collections.Generic;

namespace Ignite.Ecs
{
    /// <summary>
    /// Hàng đợi một chiều giữa Mono và ECS. Mỗi queue chỉ có 1 consumer, và consumer là bên xoá queue (đọc là lấy ra).
    /// Consumer thứ hai đòi đọc cùng queue sẽ bị báo lỗi ngay, thay vì âm thầm chia nhau mất message.
    /// </summary>
    public abstract class BridgeQueue<T> where T : struct
    {
        readonly Queue<T> _items = new Queue<T>(32);
        string _consumer;

        public int Count => _items.Count;
        public string Consumer => _consumer;

        protected void Push(in T item) => _items.Enqueue(item);

        /// <summary>Nhận quyền đọc. Gọi lại với cùng tên là hợp lệ (ví dụ presenter được inject lại).</summary>
        public Reader Claim(string consumer)
        {
            if (string.IsNullOrEmpty(consumer))
                throw new ArgumentException("Tên consumer không được rỗng.", nameof(consumer));
            if (_consumer != null && _consumer != consumer)
                throw new InvalidOperationException(
                    $"{GetType().Name.Split('`')[0]}<{typeof(T).Name}> đã có consumer '{_consumer}', không thể thêm '{consumer}'. Mỗi queue chỉ có 1 consumer.");

            _consumer = consumer;
            return new Reader(this);
        }

        public sealed class Reader
        {
            readonly BridgeQueue<T> _queue;

            internal Reader(BridgeQueue<T> queue) => _queue = queue;

            public bool TryRead(out T item) => _queue._items.TryDequeue(out item);
            public void Clear() => _queue._items.Clear();
        }
    }

    /// <summary>Mono → ECS: chỉ chở ý định (bấm nút, va chạm), không bao giờ ghi thẳng vào component.</summary>
    public sealed class CommandQueue<T> : BridgeQueue<T> where T : struct
    {
        public void Enqueue(in T command) => Push(command);
    }

    /// <summary>ECS → Mono: event tự đủ data, presenter đọc trong LateUpdate.</summary>
    public sealed class EventQueue<T> : BridgeQueue<T> where T : struct
    {
        public void Publish(in T evt) => Push(evt);
    }
}
