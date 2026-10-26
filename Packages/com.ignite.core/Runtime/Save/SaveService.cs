using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ignite.Core.Save
{
    /// <summary>Một phần dữ liệu tự lưu/đọc state của mình (một service, một module ECS…).</summary>
    public interface ISaveParticipant
    {
        /// <summary>Key duy nhất trong file save, ví dụ "fight" hay "session".</summary>
        string SaveKey { get; }

        JToken Capture();

        void Restore(JToken data);
    }

    /// <summary>Nâng dữ liệu save từ version FromVersion lên FromVersion + 1. Sửa thẳng vào object "data".</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        void Migrate(JObject data);
    }

    /// <summary>Game khai báo trong InstallerAsset.ConfigureSave: version hiện tại và các migration.</summary>
    public sealed class SaveOptions
    {
        public int Version = 1;
        public readonly List<ISaveMigration> Migrations = new List<ISaveMigration>();

        /// <summary>
        /// Bọc nơi lưu mặc định (file local), ví dụ để đồng bộ lên Steam Cloud:
        /// <c>options.WrapStorage = local =&gt; new SteamCloudSaveStorage(local);</c>
        /// </summary>
        public Func<ISaveStorage, ISaveStorage> WrapStorage;
    }

    public readonly struct SaveSlotInfo
    {
        public readonly string Slot;
        public readonly int Version;
        public readonly DateTime SavedAtUtc;

        public SaveSlotInfo(string slot, int version, DateTime savedAtUtc)
        {
            Slot = slot;
            Version = version;
            SavedAtUtc = savedAtUtc;
        }
    }

    public sealed class SaveCorruptedException : Exception
    {
        public SaveCorruptedException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// File save gồm: version, thời điểm lưu, và "data" là object {SaveKey: dữ liệu của participant}.
    /// Participant đăng ký muộn (ví dụ module ECS chỉ sống trong nhóm Gameplay) vẫn nhận đúng dữ liệu đã load;
    /// khi lưu lúc participant không còn sống, dữ liệu cũ của nó được giữ nguyên.
    /// </summary>
    public sealed class SaveService
    {
        public static readonly LogChannel Log = LogChannels.Register("Save");
        const string SlotPrefix = "slot_";

        readonly ISaveStorage _storage;
        readonly SaveOptions _options;
        readonly List<ISaveParticipant> _participants = new List<ISaveParticipant>();
        JObject _data = new JObject();

        public SaveService(ISaveStorage storage, SaveOptions options = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _options = options ?? new SaveOptions();
        }

        public int CurrentVersion => _options.Version;
        public string CurrentSlot { get; private set; }

        /// <summary>Đăng ký participant. Nếu đã có dữ liệu cho key này thì Restore ngay. Dispose để huỷ đăng ký.</summary>
        public IDisposable Register(ISaveParticipant participant)
        {
            foreach (var existing in _participants)
            {
                if (existing.SaveKey == participant.SaveKey)
                    throw new InvalidOperationException($"Đã có participant với SaveKey '{participant.SaveKey}'.");
            }

            _participants.Add(participant);
            if (_data.TryGetValue(participant.SaveKey, out var token))
                participant.Restore(token);
            return new Registration(this, participant);
        }

        public void Save(string slot)
        {
            foreach (var participant in _participants)
                _data[participant.SaveKey] = participant.Capture() ?? JValue.CreateNull();

            var file = new JObject
            {
                ["version"] = _options.Version,
                ["savedAtUtc"] = DateTime.UtcNow.ToString("o"),
                ["data"] = _data,
            };
            _storage.Write(SlotPrefix + slot, file.ToString(Formatting.Indented));
            CurrentSlot = slot;
            GameLog.Info(Log, $"Đã lưu slot '{slot}' (v{_options.Version}, {_data.Count} mục).");
        }

        /// <summary>Đọc slot, nâng version nếu cần, rồi Restore mọi participant đang sống. False nếu slot không tồn tại.</summary>
        public bool Load(string slot)
        {
            string raw = _storage.Read(SlotPrefix + slot);
            if (raw == null)
                return false;

            JObject file;
            try
            {
                file = JObject.Parse(raw);
            }
            catch (JsonException e)
            {
                throw new SaveCorruptedException($"Slot '{slot}' không phải JSON hợp lệ.", e);
            }

            int version = file.Value<int?>("version") ?? 0;
            if (version > _options.Version)
                throw new SaveCorruptedException($"Slot '{slot}' có version {version}, mới hơn game ({_options.Version}).");
            if (!(file["data"] is JObject data))
                throw new SaveCorruptedException($"Slot '{slot}' thiếu mục 'data'.");

            Migrate(data, version);
            _data = data;
            CurrentSlot = slot;

            foreach (var participant in _participants)
            {
                if (_data.TryGetValue(participant.SaveKey, out var token))
                    participant.Restore(token);
            }
            GameLog.Info(Log, $"Đã tải slot '{slot}' (v{version} → v{_options.Version}).");
            return true;
        }

        public bool Exists(string slot) => _storage.Exists(SlotPrefix + slot);

        public bool Delete(string slot) => _storage.Delete(SlotPrefix + slot);

        public IReadOnlyList<SaveSlotInfo> ListSlots()
        {
            var slots = new List<SaveSlotInfo>();
            foreach (string key in _storage.Keys())
            {
                if (!key.StartsWith(SlotPrefix, StringComparison.Ordinal))
                    continue;
                try
                {
                    var file = JObject.Parse(_storage.Read(key));
                    var savedAt = file.Value<DateTime?>("savedAtUtc") ?? DateTime.MinValue;
                    slots.Add(new SaveSlotInfo(key.Substring(SlotPrefix.Length), file.Value<int?>("version") ?? 0, savedAt.ToUniversalTime()));
                }
                catch (JsonException)
                {
                    GameLog.Warning(Log, $"Bỏ qua file save hỏng '{key}'.");
                }
            }
            return slots;
        }

        void Migrate(JObject data, int fromVersion)
        {
            for (int version = fromVersion; version < _options.Version; version++)
            {
                var migration = _options.Migrations.Find(m => m.FromVersion == version);
                if (migration == null)
                    throw new SaveCorruptedException($"Thiếu migration từ v{version} lên v{version + 1}.");
                migration.Migrate(data);
            }
        }

        sealed class Registration : IDisposable
        {
            SaveService _service;
            readonly ISaveParticipant _participant;

            public Registration(SaveService service, ISaveParticipant participant)
            {
                _service = service;
                _participant = participant;
            }

            public void Dispose()
            {
                if (_service == null)
                    return;

                // Chụp lại lần cuối để dữ liệu vẫn được ghi khi save lúc participant đã đi (ví dụ đang ở Menu).
                _service._data[_participant.SaveKey] = _participant.Capture() ?? JValue.CreateNull();
                _service._participants.Remove(_participant);
                _service = null;
            }
        }
    }
}
