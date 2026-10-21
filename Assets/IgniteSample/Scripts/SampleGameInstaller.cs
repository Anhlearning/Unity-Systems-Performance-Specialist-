using Ignite.Core.Logging;
using Ignite.Unity.Services;
using UnityEngine;
using VContainer;

namespace IgniteSample
{
    /// <summary>Kênh log riêng của game mẫu, khai báo giống hệt kênh của framework.</summary>
    public static class SampleLog
    {
        public static readonly LogChannel Sample = LogChannels.Register("Sample");
    }

    /// <summary>Đếm số trận đã vào, sống ở scope gốc nên còn nguyên khi đổi nhóm scene.</summary>
    public sealed class SampleSession
    {
        public int RunsStarted { get; private set; }

        public void StartRun()
        {
            RunsStarted++;
            GameLog.Info(SampleLog.Sample, $"Bắt đầu trận thứ {RunsStarted}");
        }
    }

    /// <summary>GameInstaller của game mẫu: liệt kê service của game theo thứ tự tường minh.</summary>
    [CreateAssetMenu(menuName = "Ignite Sample/Game Installer", fileName = "SampleGameInstaller")]
    public sealed class SampleGameInstaller : InstallerAsset
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<SampleSession>(Lifetime.Singleton);
        }
    }
}
