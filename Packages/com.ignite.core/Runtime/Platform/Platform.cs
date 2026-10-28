using System;
using System.Collections.Generic;

namespace Ignite.Core.Platform
{
    /// <summary>Achievement của nền tảng (Steam…). Game gọi qua interface này, không gọi thẳng SDK.</summary>
    public interface IAchievements
    {
        bool Unlock(string id);
        bool IsUnlocked(string id);
    }

    /// <summary>Stats số nguyên (số quái bắt được…), dùng để mở achievement theo tiến trình.</summary>
    public interface IStats
    {
        bool SetInt(string id, int value);
        int GetInt(string id);
        bool Store();
    }

    /// <summary>Overlay của nền tảng (Shift+Tab của Steam). Mở overlay thì game nên Pause.</summary>
    public interface IPlatformOverlay
    {
        event Action<bool> OverlayChanged;
    }

    public interface IPlatformInfo
    {
        /// <summary>"Steam", "Null"…</summary>
        string Name { get; }

        /// <summary>SDK đã khởi động được (đang chạy qua client của nền tảng).</summary>
        bool IsAvailable { get; }

        bool IsSteamDeck { get; }
        string UserName { get; }
    }

    /// <summary>
    /// Nền tảng rỗng: dùng khi game không lên Steam, hoặc khi Steam không chạy.
    /// Achievement/stats được nhớ trong bộ nhớ để logic game và test vẫn chạy đúng.
    /// </summary>
    public sealed class NullPlatform : IAchievements, IStats, IPlatformOverlay, IPlatformInfo
    {
        readonly HashSet<string> _achievements = new HashSet<string>();
        readonly Dictionary<string, int> _stats = new Dictionary<string, int>();

        public string Name => "Null";
        public bool IsAvailable => false;
        public bool IsSteamDeck => false;
        public string UserName => Environment.UserName;

        public event Action<bool> OverlayChanged;

        public bool Unlock(string id) => _achievements.Add(id);
        public bool IsUnlocked(string id) => _achievements.Contains(id);

        public bool SetInt(string id, int value)
        {
            _stats[id] = value;
            return true;
        }

        public int GetInt(string id) => _stats.TryGetValue(id, out int value) ? value : 0;
        public bool Store() => true;

        /// <summary>Giả lập mở/đóng overlay (test, console debug).</summary>
        public void SimulateOverlay(bool active) => OverlayChanged?.Invoke(active);
    }
}
