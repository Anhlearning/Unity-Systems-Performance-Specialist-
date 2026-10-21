using Ignite.Core.Events;
using Ignite.Unity.Flow;
using Ignite.Unity.Pooling;
using Ignite.Unity.Scenes;
using VContainer;
using VContainer.Unity;

namespace Ignite.Unity.Services
{
    /// <summary>Toàn bộ service framework đăng ký ở scope gốc. Đọc file này là thấy framework cung cấp gì.</summary>
    public static class FrameworkInstaller
    {
        /// <summary>Service không cần scene: dùng được trong EditMode test.</summary>
        public static void RegisterCore(IContainerBuilder builder)
        {
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventPublisher>().AsSelf();

            // Scoped: mỗi scope (gốc và từng nhóm scene) có bản riêng, huỷ scope là huỷ hết đăng ký event của nó.
            builder.Register<ScopedEventSubscriber>(Lifetime.Scoped).As<IEventSubscriber>();
            builder.Register<PoolService>(Lifetime.Scoped);
        }

        public static void RegisterRuntime(IContainerBuilder builder, LifetimeScope root, ILoadingScreen loadingScreen, GameFlowAsset flow)
        {
            builder.RegisterInstance(loadingScreen);
            builder.Register<SceneGroupLoader>(Lifetime.Singleton).WithParameter(root);
            builder.Register<FlowController>(Lifetime.Singleton).WithParameter(flow);
            builder.RegisterEntryPoint<GameBootstrapper>();
        }
    }
}
