using System;
using System.Collections.Generic;
using Ignite.Core.Logging;

namespace Ignite.Ecs
{
    /// <summary>
    /// Hàng đợi một chiều giữa Mono và ECS. Mỗi queue chỉ có 1 consumer, và consumer là bên xoá queue (đọc là lấy ra).
    /// Consumer thứ hai đòi đọc cùng queue sẽ bị báo lỗi ngay, thay vì âm thầm chia nhau mất message.
    /// </summary>
    public abstract class BridgeQueue<T> where T : struct
    {
        /// <summary>Quá số này mà chưa ai đọc thì gần như chắc chắn queue thiếu consumer (presenter chưa đặt vào scene, bị tắt…).</summary>
        public const int WarnCount = 1024;

        readonly Queue<T> _items = new Queue<T>(32);
        string _consumer;
        object _owner;
        bool _warned;

        public int Count => _items.Count;
        public string Consumer => _consumer;

        protected void Push(in T item)
        {
            _items.Enqueue(item);
            if (!_warned && _items.Count >= WarnCount)
                WarnBacklog();
        }

        void WarnBacklog()
        {
            _warned = true;
            string who = _consumer != null ? $"consumer '{_consumer}' không đọc kịp" : "chưa có consumer nào Claim";
            GameLog.Warning(EcsWorldHost.Log,
                $"{GetType().Name.Split('`')[0]}<{typeof(T).Name}> đang giữ {_items.Count} message: {who}. Queue sẽ phình mãi nếu không ai đọc.");
        }

        /// <summary>
        /// Nhận quyền đọc. Cùng tên và cùng owner gọi lại là hợp lệ (ví dụ presenter được inject lại).
        /// Có owner thì quyền gắn với object đó: hai instance cùng class bị chặn; owner cũ là Unity object đã Destroy thì owner mới được nhận lại.
        /// </summary>
        public Reader Claim(string consumer, object owner = null)
        {
            if (string.IsNullOrEmpty(consumer))
                throw new ArgumentException("Tên consumer không được rỗng.", nameof(consumer));
            if (_consumer != null && !CanClaim(consumer, owner))
                throw new InvalidOperationException(
                    $"{GetType().Name.Split('`')[0]}<{typeof(T).Name}> đã có consumer '{_consumer}'{DescribeOwner(_owner)}, không thể thêm '{consumer}'{DescribeOwner(owner)}. Mỗi queue chỉ có 1 consumer.");

            _consumer = consumer;
            _owner = owner;
            return new Reader(this);
        }

        bool CanClaim(string consumer, object owner)
        {
            // Owner cũ đã bị Destroy (presenter bị huỷ rồi tạo lại): nhường quyền.
            if (_owner is UnityEngine.Object unityOwner && unityOwner == null)
                return true;
            if (_consumer != consumer)
                return false;
            return _owner == null || owner == null || ReferenceEquals(_owner, owner);
        }

        static string DescribeOwner(object owner) =>
            owner is UnityEngine.Object unityOwner && unityOwner != null ? $" ({unityOwner.name})" : string.Empty;

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
