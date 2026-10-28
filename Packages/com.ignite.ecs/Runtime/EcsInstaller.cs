using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace Ignite.Ecs
{
    /// <summary>
    /// Gọi trong installer của nhóm scene gameplay (SceneGroupAsset.installer), liệt kê module theo thứ tự chạy:
    /// <code>EcsInstaller.Install(builder, new InputModule(), new FightModule(config));</code>
    /// World sống cùng scope của nhóm: vào nhóm thì tạo, rời nhóm thì huỷ.
    /// </summary>
    public static class EcsInstaller
    {
        public static void Install(IContainerBuilder builder, params IGameModule[] modules)
        {
            builder.RegisterInstance<IReadOnlyList<IGameModule>>(modules);
            builder.Register<EntityViewRegistry>(Lifetime.Singleton);

            foreach (var module in modules)
                module.Install(builder);

            builder.RegisterEntryPoint<EcsWorldHost>().AsSelf();
        }
    }
}
