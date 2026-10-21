using Ignite.Core.Logging;
using Ignite.Unity.Flow;
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

        protected override void Configure(IContainerBuilder builder)
        {
            FrameworkInstaller.RegisterCore(builder);
            FrameworkInstaller.RegisterRuntime(builder, this, ResolveLoadingScreen(), gameFlow);

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
