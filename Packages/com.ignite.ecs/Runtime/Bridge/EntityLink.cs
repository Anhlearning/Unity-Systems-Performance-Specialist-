using Scellecs.Morpeh;
using UnityEngine;

namespace Ignite.Ecs
{
    /// <summary>Gắn lên GameObject hiển thị một entity. Bị Destroy thì tự gỡ khỏi EntityViewRegistry.</summary>
    [DisallowMultipleComponent]
    public sealed class EntityLink : MonoBehaviour
    {
        EntityViewRegistry _registry;

        public Entity Entity { get; private set; }

        /// <summary>Đang đại diện một entity còn sống. Entity bị huỷ trong World là false ngay, view không được gửi command cho nó nữa.</summary>
        public bool IsLinked => _registry != null && _registry.IsAlive(Entity);

        internal EntityViewRegistry Registry => _registry;

        internal void Bind(Entity entity, EntityViewRegistry registry)
        {
            Entity = entity;
            _registry = registry;
        }

        internal void ClearBinding()
        {
            _registry = null;
            Entity = default;
        }

        void OnDestroy()
        {
            _registry?.OnViewDestroyed(Entity, this);
        }
    }
}
