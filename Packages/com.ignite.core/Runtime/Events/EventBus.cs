using System;
using System.Collections.Generic;
using Ignite.Core.Logging;

namespace Ignite.Core.Events
{
    public interface IEventPublisher
    {
        void Publish<T>(T evt) where T : struct;
    }

    public interface IEventSubscriber
    {
        /// <summary>Đăng ký nhận event kiểu T. Dispose kết quả trả về để huỷ đăng ký.</summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }

    /// <summary>
    /// Event bus cấp app, event là struct nên Publish không sinh rác.
    /// Một handler ném exception không chặn các handler còn lại.
    /// Huỷ đăng ký trong lúc đang Publish là an toàn; handler đăng ký trong lúc Publish chỉ nhận từ lần sau.
    /// </summary>
    public sealed class EventBus : IEventPublisher, IEventSubscriber
    {
        readonly Dictionary<Type, object> _lists = new Dictionary<Type, object>();

        public void Publish<T>(T evt) where T : struct
        {
            if (_lists.TryGetValue(typeof(T), out object list))
                ((HandlerList<T>)list).Invoke(evt);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_lists.TryGetValue(typeof(T), out object list))
            {
                list = new HandlerList<T>();
                _lists.Add(typeof(T), list);
            }

            var typed = (HandlerList<T>)list;
            typed.Add(handler);
            return new Subscription<T>(typed, handler);
        }

        public int CountHandlers<T>() where T : struct
        {
            return _lists.TryGetValue(typeof(T), out object list) ? ((HandlerList<T>)list).Count : 0;
        }

        sealed class HandlerList<T> where T : struct
        {
            Action<T>[] _items = new Action<T>[4];
            int _count;
            int _publishDepth;
            bool _hasHoles;

            public int Count
            {
                get
                {
                    int n = 0;
                    for (int i = 0; i < _count; i++)
                    {
                        if (_items[i] != null)
                            n++;
                    }
                    return n;
                }
            }

            public void Add(Action<T> handler)
            {
                if (_count == _items.Length)
                    Array.Resize(ref _items, _count * 2);
                _items[_count++] = handler;
            }

            public void Remove(Action<T> handler)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (ReferenceEquals(_items[i], handler))
                    {
                        _items[i] = null;
                        _hasHoles = true;
                        break;
                    }
                }

                if (_publishDepth == 0)
                    Compact();
            }

            public void Invoke(T evt)
            {
                _publishDepth++;
                try
                {
                    int count = _count;
                    for (int i = 0; i < count; i++)
                    {
                        var handler = _items[i];
                        if (handler == null)
                            continue;

                        try
                        {
                            handler(evt);
                        }
                        catch (Exception e)
                        {
                            GameLog.Error(LogChannels.Events, $"Handler của {typeof(T).Name} ném exception: {e}");
                        }
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0 && _hasHoles)
                        Compact();
                }
            }

            void Compact()
            {
                if (!_hasHoles)
                    return;

                int write = 0;
                for (int read = 0; read < _count; read++)
                {
                    if (_items[read] != null)
                        _items[write++] = _items[read];
                }
                for (int i = write; i < _count; i++)
                    _items[i] = null;

                _count = write;
                _hasHoles = false;
            }
        }

        sealed class Subscription<T> : IDisposable where T : struct
        {
            HandlerList<T> _list;
            Action<T> _handler;

            public Subscription(HandlerList<T> list, Action<T> handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_list == null)
                    return;
                _list.Remove(_handler);
                _list = null;
                _handler = null;
            }
        }
    }

    /// <summary>
    /// Bản IEventSubscriber gắn với một scope: mọi đăng ký qua nó bị huỷ khi scope bị huỷ.
    /// Framework đăng ký nó là Scoped, nên mỗi nhóm scene có một bản riêng.
    /// </summary>
    public sealed class ScopedEventSubscriber : IEventSubscriber, IDisposable
    {
        readonly IEventSubscriber _inner;
        readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        bool _disposed;

        public ScopedEventSubscriber(EventBus bus)
        {
            _inner = bus;
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScopedEventSubscriber));

            var subscription = _inner.Subscribe(handler);
            _subscriptions.Add(subscription);
            return subscription;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _subscriptions.Clear();
        }
    }
}
