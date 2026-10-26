using System.Collections.Generic;
using Ignite.Core.Debugging;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Core.Settings;
using Ignite.Core.UI;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class SettingsStoreTests
    {
        ILogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
        }

        [TearDown]
        public void TearDown() => GameLog.Sink = _sink;

        [Test]
        public void Apply_SavesToStorage_AndReloadsInNewStore()
        {
            var storage = new MemorySaveStorage();
            var store = new SettingsStore(storage);
            store.Load();
            store.Apply(s =>
            {
                s.MasterVolume = 0.3f;
                s.Language = "en";
                s.DisplayMode = DisplayMode.Windowed;
                s.Custom["difficulty"] = "hard";
            });

            var restarted = new SettingsStore(storage);
            restarted.Load();

            Assert.That(restarted.Current.MasterVolume, Is.EqualTo(0.3f));
            Assert.That(restarted.Current.Language, Is.EqualTo("en"));
            Assert.That(restarted.Current.DisplayMode, Is.EqualTo(DisplayMode.Windowed));
            Assert.That(restarted.Current.Custom["difficulty"], Is.EqualTo("hard"));
        }

        [Test]
        public void Preview_AppliesButDoesNotWrite()
        {
            var storage = new MemorySaveStorage();
            var store = new SettingsStore(storage);
            int changes = 0;
            store.Changed += _ => changes++;

            store.Preview(s => s.MusicVolume = 0.1f);

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(storage.Exists("settings"), Is.False);
        }

        [Test]
        public void CorruptFile_FallsBackToDefaults()
        {
            var storage = new MemorySaveStorage();
            storage.Write("settings", "{broken");
            var store = new SettingsStore(storage);

            store.Load();

            Assert.That(store.Current.MasterVolume, Is.EqualTo(1f));
        }

        [Test]
        public void SettingsFile_IsSeparateFromSaveSlots()
        {
            var storage = new MemorySaveStorage();
            new SettingsStore(storage).Apply(s => s.VSync = false);
            var save = new SaveService(storage);

            Assert.That(save.ListSlots(), Is.Empty);
        }
    }

    public class UIStackTests
    {
        sealed class FakeScreen : IScreen
        {
            readonly string _name;
            readonly List<string> _log;

            public FakeScreen(string name, List<string> log, bool popup = false)
            {
                _name = name;
                _log = log;
                IsPopup = popup;
            }

            public bool IsPopup { get; }
            public void OnShow() => _log.Add($"{_name}.show");
            public void OnHide() => _log.Add($"{_name}.hide");
            public void OnFocus() => _log.Add($"{_name}.focus");
            public void OnBlur() => _log.Add($"{_name}.blur");
        }

        [Test]
        public void PushScreen_HidesBelow_PopShowsItAgain()
        {
            var log = new List<string>();
            var stack = new UIStack();
            var menu = new FakeScreen("menu", log);
            var settings = new FakeScreen("settings", log);
            stack.Push(menu);
            log.Clear();

            stack.Push(settings);
            stack.Pop();

            Assert.That(log, Is.EqualTo(new[]
            {
                "menu.blur", "menu.hide", "settings.show", "settings.focus",
                "settings.blur", "settings.hide", "menu.show", "menu.focus",
            }));
        }

        [Test]
        public void Popup_KeepsBelowVisible_OnlyBlursIt()
        {
            var log = new List<string>();
            var stack = new UIStack();
            stack.Push(new FakeScreen("game", log));
            log.Clear();

            stack.Push(new FakeScreen("confirm", log, popup: true));
            stack.Pop();

            Assert.That(log, Does.Not.Contain("game.hide"));
            Assert.That(log, Does.Not.Contain("game.show"));
            Assert.That(log, Does.Contain("game.blur"));
            Assert.That(log[log.Count - 1], Is.EqualTo("game.focus"));
        }

        [Test]
        public void Back_DoesNotCloseRootScreen()
        {
            var log = new List<string>();
            var stack = new UIStack();
            stack.Push(new FakeScreen("menu", log));
            stack.Push(new FakeScreen("settings", log));

            Assert.That(stack.Back(), Is.True);
            Assert.That(stack.Back(), Is.False);
            Assert.That(stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_DestroyedTop_RefocusesBelow()
        {
            var log = new List<string>();
            var stack = new UIStack();
            var menu = new FakeScreen("menu", log);
            var gone = new FakeScreen("gone", log);
            stack.Push(menu);
            stack.Push(gone);
            log.Clear();

            stack.Remove(gone);

            Assert.That(stack.Top, Is.SameAs(menu));
            Assert.That(log, Is.EqualTo(new[] { "menu.show", "menu.focus" }));
        }
    }

    public class DebugCommandsTests
    {
        [Test]
        public void Execute_PassesArguments()
        {
            var commands = new DebugCommands();
            commands.Register("add", "cộng hai số", a => (int.Parse(a[0]) + int.Parse(a[1])).ToString());

            Assert.That(commands.Execute("add 2 3"), Is.EqualTo("5"));
        }

        [Test]
        public void UnknownCommand_ReportsIt()
        {
            StringAssert.Contains("Không có lệnh", new DebugCommands().Execute("fly"));
        }

        [Test]
        public void ThrowingCommand_ReturnsError_DoesNotThrow()
        {
            var commands = new DebugCommands();
            commands.Register("boom", "", _ => throw new System.InvalidOperationException("nổ"));

            StringAssert.Contains("nổ", commands.Execute("boom"));
        }

        [Test]
        public void Help_ListsRegisteredCommands_AndDisposeRemoves()
        {
            var commands = new DebugCommands();
            var registration = commands.Register("god", "bất tử", _ => "ok");
            StringAssert.Contains("god — bất tử", commands.Execute("help"));

            registration.Dispose();

            Assert.That(commands.Names, Does.Not.Contain("god"));
        }
    }
}
