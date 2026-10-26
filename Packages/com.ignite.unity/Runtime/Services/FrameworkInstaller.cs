using System.IO;
using Ignite.Core.Debugging;
using Ignite.Core.Events;
using Ignite.Core.Save;
using Ignite.Core.Settings;
using Ignite.Unity.Assets;
using Ignite.Unity.Audio;
using Ignite.Unity.Debugging;
using Ignite.Unity.Flow;
using Ignite.Unity.Input;
using Ignite.Unity.Localization;
using Ignite.Unity.Pooling;
using Ignite.Unity.Scenes;
using Ignite.Unity.Settings;
using Ignite.Unity.UI;
using UnityEngine;
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
            builder.Register<DebugCommands>(Lifetime.Singleton);
        }

        public static void RegisterRuntime(IContainerBuilder builder, LifetimeScope root, ILoadingScreen loadingScreen, GameFlowAsset flow)
        {
            builder.RegisterInstance(loadingScreen);
            builder.Register<SceneGroupLoader>(Lifetime.Singleton).WithParameter(root);
            builder.Register<FlowController>(Lifetime.Singleton).WithParameter(flow);
            builder.RegisterEntryPoint<GameBootstrapper>();
        }

        /// <summary>Những thứ người chơi chạm vào ngoài gameplay: input, UI, asset, âm thanh, settings, save, debug.</summary>
        public static void RegisterPlayer(IContainerBuilder builder, InputReader input, AudioConfig audio, SaveOptions saveOptions, bool debugTools)
        {
            builder.Register<InputService>(Lifetime.Singleton).WithParameter(input).WithParameter(true);
            builder.Register<InputRebinder>(Lifetime.Singleton);
            builder.Register<UIStackService>(Lifetime.Singleton);
            builder.Register<AssetService>(Lifetime.Singleton).WithParameter<IAssetBackend>(new AddressablesBackend());

            // Entry point để được tạo ngay lúc khởi động (trong scene Boot) và nhận Tick trả AudioSource về pool.
            builder.RegisterEntryPoint<AudioService>().WithParameter(audio).WithParameter((AudioLibrary)null).AsSelf();

            string data = Application.persistentDataPath;
            builder.Register<SettingsStore>(Lifetime.Singleton).WithParameter<ISaveStorage>(new FileSaveStorage(Path.Combine(data, "Settings")));
            builder.Register<SettingsService>(Lifetime.Singleton);
            ISaveStorage saves = new FileSaveStorage(Path.Combine(data, "Saves"));
            if (saveOptions.WrapStorage != null)
                saves = saveOptions.WrapStorage(saves);
            builder.Register<SaveService>(Lifetime.Singleton)
                .WithParameter(saves)
                .WithParameter(saveOptions);

            // Settings phải được nạp và áp dụng ngay khi khởi động, trước khi màn đầu tiên hiện ra.
            builder.RegisterBuildCallback(container => container.Resolve<SettingsService>());
            builder.RegisterEntryPoint<LocalizationService>().AsSelf();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugTools)
                builder.RegisterEntryPoint<DebugTools>().AsSelf();
#endif
        }
    }
}
