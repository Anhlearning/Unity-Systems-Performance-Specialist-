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
        public bool IsLinked => _registry != null;

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
