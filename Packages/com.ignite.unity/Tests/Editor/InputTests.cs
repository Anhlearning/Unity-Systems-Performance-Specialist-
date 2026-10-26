using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Core.Settings;
using Ignite.Unity.Input;
using Ignite.Unity.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ignite.Unity.Tests
{
    /// <summary>InputTestFixture tạo một Input System riêng cho test: thiết bị ảo, không đụng thiết bị thật.</summary>
    public class InputTests : InputTestFixture
    {
        InputActionAsset _asset;
        InputReader _reader;
        ILogSink _sink;

        public override void Setup()
        {
            base.Setup();
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;

            _asset = ScriptableObject.CreateInstance<InputActionAsset>();
            _asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>");
            _asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");
            var player = _asset.AddActionMap("Player");
            var jump = player.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
            jump.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
            var ui = _asset.AddActionMap("UI");
            ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            _reader = InputReader.Create(_asset);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(_reader);
            Object.DestroyImmediate(_asset);
            GameLog.Sink = _sink;
            base.TearDown();
        }

        [Test]
        public void Layer_StaysOnUntilLastOwnerReleases()
        {
            using var input = new InputService(_reader, trackDevices: false);

            var hud = input.PushLayer("Player");
            var cutscene = input.PushLayer("Player");
            hud.Dispose();
            Assert.That(input.IsLayerEnabled("Player"), Is.True, "Còn một bên đang giữ layer.");

            cutscene.Dispose();
            Assert.That(input.IsLayerEnabled("Player"), Is.False);
        }

        [Test]
        public void DisposingHandleTwice_DoesNotUnderflow()
        {
            using var input = new InputService(_reader, trackDevices: false);
            var a = input.PushLayer("Player");
            var b = input.PushLayer("Player");

            a.Dispose();
            a.Dispose();

            Assert.That(input.LayerCount("Player"), Is.EqualTo(1));
            b.Dispose();
        }

        [Test]
        public void Service_WorksOnCopy_OriginalAssetUntouched()
        {
            using var input = new InputService(_reader, trackDevices: false);
            input.Find("Player/Jump").ApplyBindingOverride(0, "<Keyboard>/q");

            Assert.That(_asset.FindAction("Player/Jump").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
        }

        [Test]
        public void DeviceTracker_FollowsLastUsedDevice()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            using var tracker = new DeviceTracker(listen: true);
            InputScheme? changedTo = null;
            tracker.Changed += s => changedTo = s;

            Press(gamepad.buttonSouth);
            Assert.That(tracker.Current, Is.EqualTo(InputScheme.Gamepad));
            Assert.That(changedTo, Is.EqualTo(InputScheme.Gamepad));

            Press(keyboard.aKey);
            Assert.That(tracker.Current, Is.EqualTo(InputScheme.KeyboardMouse));
        }

        [Test]
        public void PromptText_ChangesWithDevice()
        {
            using var input = new InputService(_reader, trackDevices: false);

            string keyboard = input.DisplayFor("Player/Jump", InputScheme.KeyboardMouse);
            string gamepad = input.DisplayFor("Player/Jump", InputScheme.Gamepad);

            Assert.That(keyboard, Is.EqualTo("Space"));
            Assert.That(gamepad, Is.Not.Empty);
            Assert.That(gamepad, Is.Not.EqualTo(keyboard));
        }

        [Test]
        public void Rebind_ChangesKey_SavesToSettings_AndSurvivesRestart()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var storage = new MemorySaveStorage();
            var settings = new SettingsStore(storage);
            using (var input = new InputService(_reader, trackDevices: false))
            using (var rebinder = new InputRebinder(input, settings))
            {
                int index = rebinder.FindBindingIndex("Player/Jump", InputScheme.KeyboardMouse);
                bool? result = null;
                rebinder.Start("Player/Jump", index, ok => result = ok);

                Press(keyboard.qKey);
                // Input System chốt phím mới ở lần update sau khi thời gian trôi; trong game thật frame tự chạy.
                currentTime += 1;
                InputSystem.Update();

                Assert.That(result, Is.True);
                Assert.That(input.Find("Player/Jump").bindings[index].effectivePath, Is.EqualTo("<Keyboard>/q"));
            }

            // "Khởi động lại": settings mới đọc từ storage, InputService mới từ asset gốc.
            var reloaded = new SettingsStore(storage);
            reloaded.Load();
            using var restarted = new InputService(_reader, trackDevices: false);
            restarted.LoadOverrides(reloaded.Current.BindingOverrides);

            Assert.That(restarted.Find("Player/Jump").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/q"));
            Assert.That(restarted.DisplayFor("Player/Jump", InputScheme.KeyboardMouse), Is.EqualTo("Q"));
        }

        [Test]
        public void Rebind_EscapeCancels_KeepsOldKey()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var settings = new SettingsStore(new MemorySaveStorage());
            using var input = new InputService(_reader, trackDevices: false);
            using var rebinder = new InputRebinder(input, settings);
            bool? result = null;

            rebinder.Start("Player/Jump", 0, ok => result = ok);
            Press(keyboard.escapeKey);
            currentTime += 1;
            InputSystem.Update();

            Assert.That(result, Is.False);
            Assert.That(input.Find("Player/Jump").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
        }

        [Test]
        public void UiCancel_IsTheSharedBackButton()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            using var input = new InputService(_reader, trackDevices: false);
            using var ui = new UIStackService(input);
            int backAtRoot = 0;
            ui.BackAtRoot += () => backAtRoot++;

            PressAndRelease(keyboard.escapeKey);

            Assert.That(backAtRoot, Is.EqualTo(1), "Stack rỗng: Back báo lên để game tự quyết (ví dụ mở Pause).");
        }
    }
}
