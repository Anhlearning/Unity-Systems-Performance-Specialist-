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
    }
}
