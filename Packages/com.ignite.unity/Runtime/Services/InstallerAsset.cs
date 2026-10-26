using Ignite.Core.Save;
using Ignite.Unity.Platform;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Ignite.Unity.Services
{
    /// <summary>
    /// Installer dạng asset. Game viết một GameInstaller kế thừa class này, liệt kê module theo thứ tự tường minh,
    /// rồi kéo asset vào RootLifetimeScope (scene Boot) hoặc vào một SceneGroupAsset (service riêng của nhóm scene).
    /// </summary>
    public abstract class InstallerAsset : ScriptableObject, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);

        /// <summary>Chỉ GameInstaller ở scope gốc: khai báo version file save và migration. Mặc định version 1.</summary>
        public virtual void ConfigureSave(SaveOptions options) { }

        /// <summary>
        /// Chỉ GameInstaller ở scope gốc: đăng ký nền tảng. Mặc định NullPlatform (không có SDK).
        /// Game lên Steam override và gọi <c>SteamInstaller.Install(builder, appId)</c>.
        /// </summary>
        public virtual void ConfigurePlatform(IContainerBuilder builder) => PlatformInstaller.RegisterNull(builder);
    }
}
