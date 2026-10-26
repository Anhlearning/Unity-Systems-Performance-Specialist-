using Ignite.Core.Events;
using Ignite.Core.Logging;
using Ignite.Core.Platform;
using Ignite.Core.Save;
using Ignite.Unity.Flow;
using Ignite.Unity.Platform;
using NUnit.Framework;
using UnityEngine;

namespace Ignite.Steam.Tests
{
    public class PlatformTests
    {
        ILogSink _sink;
        GameFlowAsset _flowAsset;

        [SetUp]
        public void SetUp()
        {
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
            _flowAsset = ScriptableObject.CreateInstance<GameFlowAsset>();
            _flowAsset.ApplyTemplate(null, null); // không có nhóm scene: đổi state không cần load scene
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flowAsset);
            GameLog.Sink = _sink;
        }

        FlowController CreateFlow(string start)
        {
            var flow = new FlowController(_flowAsset, null, new EventBus());
            Assert.That(flow.GoAsync(start).GetAwaiter().GetResult(), Is.True);
            return flow;
        }

        [Test]
        public void NullPlatform_RemembersAchievementsAndStats()
        {
            var platform = new NullPlatform();

            Assert.That(platform.Unlock("ACH_WIN_ONE_GAME"), Is.True);
            Assert.That(platform.Unlock("ACH_WIN_ONE_GAME"), Is.False, "Mở lại lần hai không tính.");
            platform.SetInt("NumWins", 3);

            Assert.That(platform.IsUnlocked("ACH_WIN_ONE_GAME"), Is.True);
            Assert.That(platform.GetInt("NumWins"), Is.EqualTo(3));
            Assert.That(platform.IsAvailable, Is.False);
        }

        [Test]
        public void OverlayOpened_DuringGameplay_PausesGame()
        {
            var platform = new NullPlatform();
            var flow = CreateFlow("Gameplay");
            var bridge = new OverlayPauseBridge(platform, flow);
            bridge.Start();

            platform.SimulateOverlay(true);

            Assert.That(flow.Current, Is.EqualTo("Pause"));
            Assert.That(bridge.PausesTriggered, Is.EqualTo(1));
            bridge.Dispose();
        }

        [Test]
        public void OverlayOpened_InMenu_DoesNothing()
        {
            var platform = new NullPlatform();
            var flow = CreateFlow("Menu"); // Menu → Pause không được khai báo
            var bridge = new OverlayPauseBridge(platform, flow);
            bridge.Start();

            platform.SimulateOverlay(true);
            platform.SimulateOverlay(false);

            Assert.That(flow.Current, Is.EqualTo("Menu"));
            Assert.That(bridge.PausesTriggered, Is.EqualTo(0));
            bridge.Dispose();
        }

        [Test]
        public void SteamPlatform_WithoutSteamRunning_FallsBackAndGameKeepsWorking()
        {
            // Máy test không chạy Steam: Init phải thất bại an toàn, không ném exception.
            var steam = new SteamPlatform(new SteamOptions { AppId = 480, RestartIfNecessary = false });
            try
            {
                if (steam.IsAvailable)
                    Assert.Ignore("Steam đang chạy trên máy này: test đường 'không có Steam' không áp dụng.");

                Assert.That(steam.Name, Is.EqualTo("Steam (offline)"));
                Assert.That(steam.InitError, Is.Not.Empty);
                Assert.That(steam.Unlock("ACH_WIN_ONE_GAME"), Is.True, "Rơi về NullPlatform: vẫn nhớ trong bộ nhớ.");
                Assert.That(steam.IsUnlocked("ACH_WIN_ONE_GAME"), Is.True);
                Assert.DoesNotThrow(() => steam.Tick());
                Assert.That(SteamPlatform.IsInitialized, Is.False);
            }
            finally
            {
                steam.Dispose();
            }
        }

        [Test]
        public void SteamPlatform_Offline_ForwardsSimulatedOverlay()
        {
            var steam = new SteamPlatform(new SteamOptions { RestartIfNecessary = false });
            try
            {
                bool? seen = null;
                steam.OverlayChanged += active => seen = active;
                steam.SimulateOverlay(true);
                Assert.That(seen, Is.True);
            }
            finally
            {
                steam.Dispose();
            }
        }

        [Test]
        public void CloudStorage_WithoutSteam_UsesLocalStorage()
        {
            var local = new MemorySaveStorage();
            var cloud = new SteamCloudSaveStorage(local);

            cloud.Write("slot_auto", "{\"version\":1,\"data\":{}}");

            Assert.That(cloud.CloudReady, Is.False);
            Assert.That(local.Read("slot_auto"), Is.EqualTo("{\"version\":1,\"data\":{}}"), "Bản local luôn được ghi.");
            Assert.That(new SaveService(cloud).Load("auto"), Is.True);
            Assert.That(cloud.Keys(), Is.EquivalentTo(new[] { "slot_auto" }));
        }
    }
}
