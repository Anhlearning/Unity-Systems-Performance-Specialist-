using System;
using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using Ignite.Core.Platform;
using Ignite.Unity.Flow;
using VContainer;
using VContainer.Unity;

namespace Ignite.Unity.Platform
{
    public static class PlatformInstaller
    {
        public static readonly LogChannel Log = LogChannels.Register("Platform");

        /// <summary>Nền tảng mặc định: không có SDK nào, achievement/stats nhớ trong bộ nhớ.</summary>
        public static void RegisterNull(IContainerBuilder builder)
        {
            builder.RegisterInstance(new NullPlatform()).AsImplementedInterfaces().AsSelf();
            RegisterCommon(builder);
        }

        /// <summary>Phần dùng chung cho mọi nền tảng. Package nền tảng (Steam…) gọi sau khi đăng ký facet của mình.</summary>
        public static void RegisterCommon(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<OverlayPauseBridge>().AsSelf();
        }
    }

    /// <summary>Mở overlay của nền tảng (Shift+Tab trên Steam) thì chuyển sang state Pause của Game Flow, nếu đang được phép.</summary>
    public sealed class OverlayPauseBridge : IStartable, IDisposable
    {
        readonly IPlatformOverlay _overlay;
        readonly FlowController _flow;

        public OverlayPauseBridge(IPlatformOverlay overlay, FlowController flow)
        {
            _overlay = overlay;
            _flow = flow;
        }

        public int PausesTriggered { get; private set; }

        public void Start() => _overlay.OverlayChanged += OnOverlayChanged;

        void OnOverlayChanged(bool active)
        {
            string pause = _flow.PauseState;
            if (!active || string.IsNullOrEmpty(pause) || !_flow.CanGo(pause))
                return;

            PausesTriggered++;
            GameLog.Info(PlatformInstaller.Log, $"Overlay mở: chuyển sang {pause}");
            _flow.GoAsync(pause).Forget();
        }

        public void Dispose() => _overlay.OverlayChanged -= OnOverlayChanged;
    }
}
