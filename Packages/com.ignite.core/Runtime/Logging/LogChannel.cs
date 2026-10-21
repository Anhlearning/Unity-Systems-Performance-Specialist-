using System;

namespace Ignite.Core.Logging
{
    /// <summary>
    /// Một kênh log, mang một bit trong mask 64 bit. Tạo bằng <see cref="LogChannels.Register"/>,
    /// game khai báo kênh của mình như framework khai báo kênh có sẵn:
    /// <code>public static readonly LogChannel Fight = LogChannels.Register("Fight");</code>
    /// </summary>
    public readonly struct LogChannel : IEquatable<LogChannel>
    {
        public readonly int Bit;

        internal LogChannel(int bit)
        {
            Bit = bit;
        }

        public ulong Mask => 1UL << Bit;
        public string Name => LogChannels.NameOf(this);

        public bool Equals(LogChannel other) => Bit == other.Bit;
        public override bool Equals(object obj) => obj is LogChannel other && Equals(other);
        public override int GetHashCode() => Bit;
        public override string ToString() => Name;

        public static bool operator ==(LogChannel a, LogChannel b) => a.Bit == b.Bit;
        public static bool operator !=(LogChannel a, LogChannel b) => a.Bit != b.Bit;
    }
}
