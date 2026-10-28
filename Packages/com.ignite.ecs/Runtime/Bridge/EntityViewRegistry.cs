using System.Collections.Generic;
using Scellecs.Morpeh;

namespace Ignite.Ecs
{
    /// <summary>
    /// Bảng tra hai chiều Entity ↔ View (EntityLink trên GameObject). Mỗi nhóm scene gameplay có một bảng.
    /// Không quét mỗi frame: entity còn sống hay không được hỏi World ngay lúc dùng (<see cref="IsAlive"/>), nên chi phí không tăng theo số view.
    /// </summary>
    public sealed class EntityViewRegistry
    {
        readonly Dictionary<Entity, EntityLink> _views = new Dictionary<Entity, EntityLink>();
        World _world;

        public int Count => _views.Count;

        /// <summary>EcsWorldHost gắn World của nhóm vào ngay khi tạo. Chưa gắn (test dựng tay) thì coi mọi entity là còn sống.</summary>
        internal void Attach(World world) => _world = world;

        public bool IsAlive(Entity entity)
        {
            if (_world == null)
                return true;
            return !_world.IsDisposed && !_world.IsDisposed(entity);
        }

        public void Link(Entity entity, EntityLink view)
        {
            if (_views.TryGetValue(entity, out var previous) && previous != view && previous != null)
                previous.ClearBinding();

            // View lấy từ pool, đang đại diện entity khác: gỡ khoá cũ, không để nó trỏ nhầm sang entity mới.
            if (view.Registry == this && !view.Entity.Equals(entity)
                && _views.TryGetValue(view.Entity, out var mapped) && mapped == view)
                _views.Remove(view.Entity);

            _views[entity] = view;
            view.Bind(entity, this);
        }

        public void Unlink(Entity entity)
        {
            if (_views.Remove(entity, out var view) && view != null)
                view.ClearBinding();
        }

        /// <summary>
        /// View đang hoặc vừa đại diện entity. Vẫn trả về cho entity đã chết (cho tới khi view bị Destroy, link sang entity khác hoặc Unlink),
        /// để presenter nhận event "chết" còn tìm được view mà chạy hiệu ứng. Muốn biết entity còn sống thì dùng <see cref="IsAlive"/>.
        /// </summary>
        public bool TryGetView(Entity entity, out EntityLink view)
        {
            if (_views.TryGetValue(entity, out view) && view != null)
                return true;
            view = null;
            return false;
        }

        /// <summary>Host gọi khi World bị huỷ: view ở scene được giữ lại thôi trỏ vào World cũ cho tới khi được inject lại.</summary>
        internal void UnlinkAll()
        {
            foreach (var view in _views.Values)
            {
                if (view != null)
                    view.ClearBinding();
            }
            _views.Clear();
            _world = null;
        }

        internal void OnViewDestroyed(Entity entity, EntityLink view)
        {
            if (_views.TryGetValue(entity, out var current) && current == view)
                _views.Remove(entity);
        }
    }
}
