using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Unity.Audio;
using Ignite.Unity.Flow;
using Ignite.Unity.Input;
using Ignite.Unity.Platform;
using Ignite.Unity.Scenes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Ignite.Unity.Services
{
    /// <summary>
    /// Scope gốc, nằm trong scene Boot và sống suốt game.
    /// Thứ tự: service framework → GameInstaller của game. Mỗi nhóm scene được SceneGroupLoader tạo một scope con.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] InstallerAsset gameInstaller;
        [SerializeField] GameFlowAsset gameFlow;
        [Tooltip("MonoBehaviour cài ILoadingScreen. Để trống thì dùng DefaultLoadingScreen.")]
        [SerializeField] MonoBehaviour loadingScreen;
        [SerializeField] InputReader inputReader;
        [SerializeField] AudioConfig audioConfig;
        [Tooltip("Console lệnh, cheat, overlay FPS/GC. Chỉ có trong Editor và Development Build.")]
        [SerializeField] bool debugTools = true;

        protected override void Configure(IContainerBuilder builder)
        {
            var saveOptions = new SaveOptions();
            if (gameInstaller != null)
                gameInstaller.ConfigureSave(saveOptions);

            FrameworkInstaller.RegisterCore(builder);
            FrameworkInstaller.RegisterRuntime(builder, this, ResolveLoadingScreen(), gameFlow);
            FrameworkInstaller.RegisterPlayer(builder, inputReader, audioConfig, saveOptions, debugTools);

            if (gameInstaller != null)
                gameInstaller.ConfigurePlatform(builder);
            else
                PlatformInstaller.RegisterNull(builder);

            if (gameInstaller != null)
                gameInstaller.Install(builder);
            else
                GameLog.Warning(LogChannels.Services, "RootLifetimeScope chưa có GameInstaller.");
        }

        ILoadingScreen ResolveLoadingScreen()
        {
            if (loadingScreen is ILoadingScreen custom)
                return custom;
            if (loadingScreen != null)
                GameLog.Error(LogChannels.Services, $"{loadingScreen.GetType().Name} không cài ILoadingScreen, dùng DefaultLoadingScreen.");

            var fallback = GetComponentInChildren<DefaultLoadingScreen>(true);
            return fallback != null ? fallback : gameObject.AddComponent<DefaultLoadingScreen>();
        }
    }
}
