using System.Collections.Generic;
using Scellecs.Morpeh;
using VContainer;

namespace Ignite.Ecs
{
    /// <summary>
    /// Một khối gameplay (Fight, Spawning, Trading…). Module tự khai báo service/queue nó cần và system của nó.
    /// Game liệt kê module theo thứ tự tường minh khi gọi <see cref="EcsInstaller.Install"/>.
    /// </summary>
    public interface IGameModule
    {
        string Name { get; }

        /// <summary>Đăng ký queue, config, service của module. Chạy trong scope của nhóm scene gameplay.</summary>
        void Install(IContainerBuilder builder);

        /// <summary>Thêm system theo đúng thứ tự chạy. Không có tự quét hay attribute.</summary>
        void AddSystems(SystemsBuilder systems);
    }

    /// <summary>Giúp module thêm system vào SystemsGroup của mình và lấy dependency từ scope.</summary>
    public sealed class SystemsBuilder
    {
        readonly SystemsGroup _group;
        readonly IObjectResolver _resolver;
        readonly List<string> _systemNames;

        internal SystemsBuilder(World world, SystemsGroup group, IObjectResolver resolver, List<string> systemNames)
        {
            World = world;
            _group = group;
            _resolver = resolver;
            _systemNames = systemNames;
        }

        public World World { get; }

        public T Resolve<T>() => _resolver.Resolve<T>();

        public SystemsBuilder Add<T>(T system) where T : class, ISystem
        {
            _group.AddSystem(system);
            _systemNames.Add(typeof(T).Name);
            return this;
        }

        public SystemsBuilder AddInitializer<T>(T initializer) where T : class, IInitializer
        {
            _group.AddInitializer(initializer);
            _systemNames.Add(typeof(T).Name);
            return this;
        }
    }
}
