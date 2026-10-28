using System.Collections.Generic;
using Scellecs.Morpeh;

namespace Ignite.Ecs
{
    /// <summary>Bảng tra hai chiều Entity ↔ View (EntityLink trên GameObject). Mỗi nhóm scene gameplay có một bảng.</summary>
    public sealed class EntityViewRegistry
    {
        readonly Dictionary<Entity, EntityLink> _views = new Dictionary<Entity, EntityLink>();

        public int Count => _views.Count;

        public void Link(Entity entity, EntityLink view)
        {
            if (_views.TryGetValue(entity, out var previous) && previous != view && previous != null)
                previous.ClearBinding();

            _views[entity] = view;
            view.Bind(entity, this);
        }

        public void Unlink(Entity entity)
        {
            if (_views.Remove(entity, out var view) && view != null)
                view.ClearBinding();
        }

        public bool TryGetView(Entity entity, out EntityLink view)
        {
            if (_views.TryGetValue(entity, out view) && view != null)
                return true;
            view = null;
            return false;
        }

        internal void OnViewDestroyed(Entity entity, EntityLink view)
        {
            if (_views.TryGetValue(entity, out var current) && current == view)
                _views.Remove(entity);
        }
    }
}
