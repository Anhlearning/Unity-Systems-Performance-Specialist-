using System;
using System.IO;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class SaveTests
    {
        sealed class Counter : ISaveParticipant
        {
            public Counter(string key) => SaveKey = key;
            public string SaveKey { get; }
            public int Value;
            public JToken Capture() => new JObject { ["value"] = Value };
            public void Restore(JToken data) => Value = data.Value<int>("value");
        }

        sealed class RenameGoldMigration : ISaveMigration
        {
            public int FromVersion => 1;

            public void Migrate(JObject data)
            {
                // v1 lưu "money", v2 đổi tên thành "gold".
                var wallet = (JObject)data["wallet"];
                wallet["gold"] = wallet["money"];
                wallet.Remove("money");
            }
        }

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
        public void SaveThenLoad_RestoresParticipants()
        {
            var storage = new MemorySaveStorage();
            var save = new SaveService(storage);
            var counter = new Counter("counter") { Value = 7 };
            save.Register(counter);

            save.Save("slot1");
            counter.Value = 0;
            Assert.That(save.Load("slot1"), Is.True);

            Assert.That(counter.Value, Is.EqualTo(7));
        }

        [Test]
        public void LateParticipant_ReceivesLoadedData_OnRegister()
        {
            // Module ECS chỉ sống trong nhóm Gameplay: đăng ký sau khi save đã được tải ở Menu.
            var storage = new MemorySaveStorage();
            var first = new SaveService(storage);
            first.Register(new Counter("fight") { Value = 3 });
            first.Save("auto");

            var second = new SaveService(storage);
            second.Load("auto");
            var late = new Counter("fight");
            second.Register(late);

            Assert.That(late.Value, Is.EqualTo(3));
        }

        [Test]
        public void SavingWhileParticipantIsGone_KeepsItsLastData()
        {
            var storage = new MemorySaveStorage();
            var save = new SaveService(storage);
            var fight = new Counter("fight") { Value = 5 };
            var registration = save.Register(fight);
            registration.Dispose(); // rời nhóm Gameplay: chụp lần cuối rồi gỡ

            save.Save("auto");

            var reload = new SaveService(storage);
            reload.Load("auto");
            var check = new Counter("fight");
            reload.Register(check);
            Assert.That(check.Value, Is.EqualTo(5));
        }

        [Test]
        public void OldSave_IsMigratedToCurrentVersion()
        {
            var storage = new MemorySaveStorage();
            storage.Write("slot_old", "{\"version\":1,\"savedAtUtc\":\"2026-01-01T00:00:00Z\",\"data\":{\"wallet\":{\"money\":42}}}");

            var options = new SaveOptions { Version = 2 };
            options.Migrations.Add(new RenameGoldMigration());
            var save = new SaveService(storage, options);
            var wallet = new WalletParticipant();
            save.Register(wallet);

            Assert.That(save.Load("old"), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(42));
        }

        sealed class WalletParticipant : ISaveParticipant
        {
            public int Gold;
            public string SaveKey => "wallet";
            public JToken Capture() => new JObject { ["gold"] = Gold };
            public void Restore(JToken data) => Gold = data.Value<int>("gold");
        }

        [Test]
        public void MissingMigration_Throws()
        {
            var storage = new MemorySaveStorage();
            storage.Write("slot_old", "{\"version\":1,\"data\":{}}");
            var save = new SaveService(storage, new SaveOptions { Version = 3 });

            Assert.Throws<SaveCorruptedException>(() => save.Load("old"));
        }

        [Test]
        public void SaveFromNewerGame_IsRejected()
        {
            var storage = new MemorySaveStorage();
            storage.Write("slot_new", "{\"version\":9,\"data\":{}}");

            Assert.Throws<SaveCorruptedException>(() => new SaveService(storage).Load("new"));
        }

        [Test]
        public void CorruptJson_IsRejected_WithoutTouchingParticipants()
        {
            var storage = new MemorySaveStorage();
            storage.Write("slot_bad", "{ not json");
            var save = new SaveService(storage);
            var counter = new Counter("counter") { Value = 1 };
            save.Register(counter);

            Assert.Throws<SaveCorruptedException>(() => save.Load("bad"));
            Assert.That(counter.Value, Is.EqualTo(1));
        }

        [Test]
        public void MissingSlot_ReturnsFalse()
        {
            Assert.That(new SaveService(new MemorySaveStorage()).Load("nothing"), Is.False);
        }

        [Test]
        public void ListSlots_ReturnsVersionAndTime()
        {
            var save = new SaveService(new MemorySaveStorage(), new SaveOptions { Version = 2 });
            save.Save("a");
            save.Save("b");

            var slots = save.ListSlots();

            Assert.That(slots, Has.Count.EqualTo(2));
            Assert.That(slots[0].Version, Is.EqualTo(2));
            Assert.That(slots[0].SavedAtUtc, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public void DuplicateSaveKey_Throws()
        {
            var save = new SaveService(new MemorySaveStorage());
            save.Register(new Counter("x"));
            Assert.Throws<InvalidOperationException>(() => save.Register(new Counter("x")));
        }
    }

    public class FileSaveStorageTests
    {
        string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "ignite-save-test-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        [Test]
        public void Write_Overwrite_Read_RoundTrips_AndLeavesNoTempFile()
        {
            var storage = new FileSaveStorage(_dir);

            storage.Write("slot_1", "first");
            storage.Write("slot_1", "second");

            Assert.That(storage.Read("slot_1"), Is.EqualTo("second"));
            Assert.That(Directory.GetFiles(_dir, "*.tmp"), Is.Empty);
            Assert.That(storage.Keys(), Is.EquivalentTo(new[] { "slot_1" }));
        }

        [Test]
        public void CrashBetweenTempAndReplace_KeepsOldFileIntact()
        {
            // Giả lập tắt game sau khi ghi .tmp nhưng trước khi thay file: file cũ phải còn nguyên.
            var storage = new FileSaveStorage(_dir);
            storage.Write("slot_1", "good");
            File.WriteAllText(Path.Combine(_dir, "slot_1.json.tmp"), "half-writ");

            Assert.That(storage.Read("slot_1"), Is.EqualTo("good"));

            storage.Write("slot_1", "next"); // lần ghi sau ghi đè file tạm cũ
            Assert.That(storage.Read("slot_1"), Is.EqualTo("next"));
        }

        [Test]
        public void InvalidKey_Throws()
        {
            var storage = new FileSaveStorage(_dir);
            Assert.Throws<ArgumentException>(() => storage.Write("../evil", "x"));
        }

        [Test]
        public void Delete_RemovesFile()
        {
            var storage = new FileSaveStorage(_dir);
            storage.Write("slot_1", "x");

            Assert.That(storage.Delete("slot_1"), Is.True);
            Assert.That(storage.Exists("slot_1"), Is.False);
            Assert.That(storage.Delete("slot_1"), Is.False);
        }
    }
}
