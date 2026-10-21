using System.Collections.Generic;
using System.Diagnostics;

namespace Ignite.Core.Logging
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>Nơi log thật sự được in ra. Core không biết Unity, nên tầng Unity cài UnityLogSink vào đây.</summary>
    public interface ILogSink
    {
        void Write(LogLevel level, LogChannel channel, string message);
    }

    public sealed class NullLogSink : ILogSink
    {
        public static readonly NullLogSink Instance = new NullLogSink();
        public void Write(LogLevel level, LogChannel channel, string message) { }
    }

    /// <summary>
    /// Log theo kênh, bật tắt bằng bitmask.
    /// Info bị compiler xoá khỏi bản release (kể cả phần dựng chuỗi ở chỗ gọi).
    /// Warning lọc theo kênh; Error luôn được in để lỗi không bao giờ bị giấu.
    /// </summary>
    public static class GameLog
    {
        static ulong s_enabledMask = ulong.MaxValue;
        static readonly HashSet<string> s_disabledNames = new HashSet<string>();

        public static ILogSink Sink { get; set; } = NullLogSink.Instance;

        public static IReadOnlyCollection<string> DisabledChannelNames => s_disabledNames;

        public static bool IsEnabled(LogChannel channel) => (s_enabledMask & channel.Mask) != 0;

        public static void SetEnabled(LogChannel channel, bool enabled)
        {
            if (enabled)
            {
                s_enabledMask |= channel.Mask;
                s_disabledNames.Remove(channel.Name);
            }
            else
            {
                s_enabledMask &= ~channel.Mask;
                s_disabledNames.Add(channel.Name);
            }
        }

        /// <summary>
        /// Đặt lại danh sách kênh bị tắt theo tên. Kênh chưa đăng ký cũng được nhớ,
        /// để kênh của game khai báo muộn vẫn nhận đúng trạng thái đã lưu.
        /// </summary>
        public static void SetDisabledChannels(IEnumerable<string> names)
        {
            s_enabledMask = ulong.MaxValue;
            s_disabledNames.Clear();
            if (names == null)
                return;

            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                    s_disabledNames.Add(name);
            }

            for (int i = 0; i < LogChannels.Count; i++)
                OnChannelRegistered(LogChannels.At(i));
        }

        internal static void OnChannelRegistered(LogChannel channel)
        {
            if (s_disabledNames.Contains(channel.Name))
                s_enabledMask &= ~channel.Mask;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("IGNITE_LOG_INFO")]
        public static void Info(LogChannel channel, string message)
        {
            if (IsEnabled(channel))
                Sink.Write(LogLevel.Info, channel, message);
        }

        public static void Warning(LogChannel channel, string message)
        {
            if (IsEnabled(channel))
                Sink.Write(LogLevel.Warning, channel, message);
        }

        public static void Error(LogChannel channel, string message)
        {
            Sink.Write(LogLevel.Error, channel, message);
        }
    }
}
