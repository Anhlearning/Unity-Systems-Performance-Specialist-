using System;

namespace Ignite.Core.Logging
{
    /// <summary>Danh bạ kênh log. Framework khai báo sẵn các kênh dưới đây; game gọi Register để thêm kênh riêng.</summary>
    public static class LogChannels
    {
        public const int MaxChannels = 64;

        static readonly object s_lock = new object();
        static readonly string[] s_names = new string[MaxChannels];
        static int s_count;

        public static readonly LogChannel Default = Register("Default");
        public static readonly LogChannel Flow = Register("Flow");
        public static readonly LogChannel Scene = Register("Scene");
        public static readonly LogChannel Services = Register("Services");
        public static readonly LogChannel Events = Register("Events");
        public static readonly LogChannel Pool = Register("Pool");

        // Phải đứng sau các kênh có sẵn: số kênh của framework, dùng để reset trong test.
        static readonly int s_builtInCount = s_count;

        public static int Count => s_count;

        /// <summary>Đăng ký kênh mới. Gọi lại với cùng tên thì trả về kênh cũ.</summary>
        public static LogChannel Register(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên kênh log không được rỗng.", nameof(name));

            LogChannel channel;
            lock (s_lock)
            {
                for (int i = 0; i < s_count; i++)
                {
                    if (s_names[i] == name)
                        return new LogChannel(i);
                }

                if (s_count == MaxChannels)
                    throw new InvalidOperationException($"Đã đủ {MaxChannels} kênh log, không thể thêm '{name}'.");

                s_names[s_count] = name;
                channel = new LogChannel(s_count);
                s_count++;
            }

            GameLog.OnChannelRegistered(channel);
            return channel;
        }

        public static LogChannel At(int index)
        {
            if ((uint)index >= (uint)s_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new LogChannel(index);
        }

        public static string NameOf(LogChannel channel)
        {
            return channel.Bit < s_count ? s_names[channel.Bit] : $"#{channel.Bit}";
        }

        internal static void ResetToBuiltInForTests()
        {
            lock (s_lock)
            {
                for (int i = s_builtInCount; i < s_count; i++)
                    s_names[i] = null;
                s_count = s_builtInCount;
            }
        }
    }
}
