using System;
using Ignite.Core.Events;
using Ignite.Core.Flow;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Steam;
using Ignite.Unity.Services;
using Newtonsoft.Json.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace IgniteSample
{
    /// <summary>Kênh log riêng của game mẫu, khai báo giống hệt kênh của framework.</summary>
    public static class SampleLog
    {
        public static readonly LogChannel Sample = LogChannels.Register("Sample");
    }

    /// <summary>Đếm số trận đã vào. Sống ở scope gốc và tự lưu vào file save (key "session").</summary>
    public sealed class SampleSession : ISaveParticipant, IDisposable
    {
        readonly IDisposable _registration;

        public SampleSession(SaveService save)
        {
            _registration = save.Register(this);
        }

        public int RunsStarted { get; private set; }

        public void StartRun()
        {
            RunsStarted++;
            GameLog.Info(SampleLog.Sample, $"Bắt đầu trận thứ {RunsStarted}");
        }

        public string SaveKey => "session";
        public JToken Capture() => new JObject { ["runs"] = RunsStarted };
        public void Restore(JToken data) => RunsStarted = data.Value<int?>("runs") ?? 0;

        public void Dispose() => _registration.Dispose();
    }

    /// <summary>Màn hình dùng chung giữa các scene (màn Settings nằm ở SharedUI, mở được từ Menu và Pause).</summary>
    public sealed class SampleScreens
    {
        public SampleSettingsScreen Settings { get; set; }
    }

    /// <summary>Khởi động thì tải slot "auto"; mỗi lần về Menu thì tự lưu.</summary>
    public sealed class SampleAutoSave : IStartable, IDisposable
    {
        public const string Slot = "auto";

        readonly SaveService _save;
        readonly IEventSubscriber _events;
        readonly SampleSession _session;
        IDisposable _subscription;

        public SampleAutoSave(SaveService save, IEventSubscriber events, SampleSession session)
        {
            _save = save;
            _events = events;
            _session = session; // giữ để participant "session" đăng ký trước khi tải
        }

        public void Start()
        {
            if (_save.Exists(Slot))
            {
                try
                {
                    _save.Load(Slot);
                }
                catch (SaveCorruptedException e)
                {
                    GameLog.Error(SaveService.Log, $"Không đọc được save tự động: {e.Message}");
                }
            }

            _subscription = _events.Subscribe<FlowStateChanged>(change =>
            {
                if (change.To == "Menu" && change.From != null)
                    _save.Save(Slot);
            });
        }

        public void Dispose() => _subscription?.Dispose();
    }

    /// <summary>GameInstaller của game mẫu: liệt kê service của game theo thứ tự tường minh.</summary>
    [CreateAssetMenu(menuName = "Ignite Sample/Game Installer", fileName = "SampleGameInstaller")]
    public sealed class SampleGameInstaller : InstallerAsset
    {
        public override void ConfigureSave(SaveOptions options)
        {
            // v1: {"session": {"runs"}}; v2 thêm tiến trình săn quái {"fight": {...}}.
            options.Version = 2;
            options.Migrations.Add(new AddFightProgressMigration());
            // Lưu song song lên Steam Cloud khi Steam chạy; không có Steam thì chỉ file local.
            options.WrapStorage = local => new SteamCloudSaveStorage(local);
        }

        /// <summary>App ID 480 (Spacewar) để thử. Không có Steam thì tự rơi về NullPlatform.</summary>
        public override void ConfigurePlatform(IContainerBuilder builder) => SteamInstaller.Install(builder, 480);

        public override void Install(IContainerBuilder builder)
        {
            builder.Register<SampleSession>(Lifetime.Singleton);
            builder.Register<SampleScreens>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SampleAutoSave>();
        }
    }

    /// <summary>Save v1 chưa có mục "fight": thêm vào với giá trị 0.</summary>
    public sealed class AddFightProgressMigration : ISaveMigration
    {
        public int FromVersion => 1;

        public void Migrate(JObject data)
        {
            if (data["fight"] == null)
                data["fight"] = new JObject { ["caught"] = 0, ["escaped"] = 0, ["linesLost"] = 0 };
        }
    }
}
