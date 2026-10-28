using UnityEngine;
using VContainer;

namespace Ignite.Ecs
{
    /// <summary>
    /// View nhận event từ ECS. Là consumer duy nhất của EventQueue&lt;TEvent&gt;, đọc hết queue trong LateUpdate
    /// (sau khi system đã chạy trong Update). State liên tục (thanh máu…) thì đọc thẳng component qua <see cref="World"/>.
    /// </summary>
    public abstract class PresenterBase<TEvent> : MonoBehaviour where TEvent : struct
    {
        BridgeQueue<TEvent>.Reader _reader;

        protected EcsWorldHost Host { get; private set; }
        protected EntityViewRegistry Views { get; private set; }
        protected Scellecs.Morpeh.World World => Host != null ? Host.World : null;

        [Inject]
        void InjectPresenter(EventQueue<TEvent> events, EcsWorldHost host, EntityViewRegistry views)
        {
            // Có owner = this: hai object cùng loại presenter trong scene sẽ báo lỗi thay vì âm thầm chia nhau event.
            _reader = events.Claim(GetType().Name, this);
            Host = host;
            Views = views;
            OnInjected();
        }

        /// <summary>Thay cho Start: chạy khi đã có World và queue.</summary>
        protected virtual void OnInjected() { }

        protected abstract void OnEvent(in TEvent evt);

        /// <summary>Chạy sau khi đã đọc hết event của frame này. Dùng để vẽ state liên tục.</summary>
        protected virtual void OnLateFrame() { }

        void LateUpdate()
        {
            if (_reader == null || Host == null || !Host.IsAlive)
                return;

            while (_reader.TryRead(out var evt))
                OnEvent(evt);
            OnLateFrame();
        }
    }
}
