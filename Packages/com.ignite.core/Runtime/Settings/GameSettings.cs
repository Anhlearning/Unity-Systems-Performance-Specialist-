using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Newtonsoft.Json;

namespace Ignite.Core.Settings
{
    /// <summary>Giống UnityEngine.FullScreenMode, khai báo lại vì Core không biết Unity.</summary>
    public enum DisplayMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    /// <summary>
    /// Cài đặt của người chơi, lưu ra file riêng (không nằm trong save game, không bị mất khi xoá save).
    /// Setting riêng của game để trong Custom.
    /// </summary>
    public sealed class GameSettings
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public float UiVolume = 1f;
        public string Language = "vi";

        /// <summary>0 = giữ độ phân giải gốc của màn hình.</summary>
        public int ResolutionWidth;
        public int ResolutionHeight;
        public DisplayMode DisplayMode = DisplayMode.FullScreenWindow;
        public bool VSync = true;

        /// <summary>-1 = giữ mức chất lượng mặc định của project.</summary>
        public int QualityLevel = -1;

        /// <summary>JSON của InputActionAsset.SaveBindingOverridesAsJson().</summary>
        public string BindingOverrides = string.Empty;

        public Dictionary<string, string> Custom = new Dictionary<string, string>();

        public GameSettings Clone() => JsonConvert.DeserializeObject<GameSettings>(JsonConvert.SerializeObject(this));
    }

    public sealed class SettingsStore
    {
        public static readonly LogChannel Log = LogChannels.Register("Settings");
        const string Key = "settings";

        readonly ISaveStorage _storage;

        public SettingsStore(ISaveStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public GameSettings Current { get; private set; } = new GameSettings();

        /// <summary>Phát sau mỗi lần Apply/Load: service khác (audio, màn hình, input) cập nhật theo.</summary>
        public event Action<GameSettings> Changed;

        /// <summary>File thiếu hoặc hỏng thì dùng mặc định, không chặn game khởi động.</summary>
        public void Load()
        {
            string raw = _storage.Read(Key);
            if (raw != null)
            {
                try
                {
                    Current = JsonConvert.DeserializeObject<GameSettings>(raw) ?? new GameSettings();
                }
                catch (JsonException e)
                {
                    GameLog.Warning(Log, $"File settings hỏng, dùng mặc định: {e.Message}");
                    Current = new GameSettings();
                }
            }
            Changed?.Invoke(Current);
        }

        public void Save() => _storage.Write(Key, JsonConvert.SerializeObject(Current, Formatting.Indented));

        /// <summary>Sửa và áp dụng ngay nhưng chưa ghi file (ví dụ đang kéo thanh âm lượng). Gọi Save khi đóng màn Settings.</summary>
        public void Preview(Action<GameSettings> change)
        {
            change(Current);
            Changed?.Invoke(Current);
        }

        /// <summary>Sửa settings, lưu ra file, báo cho các service áp dụng.</summary>
        public void Apply(Action<GameSettings> change)
        {
            change(Current);
            Save();
            Changed?.Invoke(Current);
        }
    }
}
