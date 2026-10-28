using Ignite.Ecs;
using Ignite.Unity.Services;
using UnityEngine;
using VContainer;

namespace IgniteSample.Gameplay.View
{
    /// <summary>
    /// Installer của nhóm scene Gameplay: liệt kê module ECS theo thứ tự chạy.
    /// World sống cùng nhóm này: vào Gameplay thì tạo, về Menu thì huỷ.
    /// </summary>
    [CreateAssetMenu(menuName = "Ignite Sample/Gameplay Installer", fileName = "SampleGameplayInstaller")]
    public sealed class SampleGameplayInstaller : InstallerAsset
    {
        public override void Install(IContainerBuilder builder)
        {
            EcsInstaller.Install(builder,
                new HitDemoModule(),
                new FightModule(new FightConfig()));
        }
    }
}
