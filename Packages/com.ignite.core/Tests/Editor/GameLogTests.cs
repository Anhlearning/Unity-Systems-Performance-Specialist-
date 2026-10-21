using System.Collections.Generic;
using Ignite.Core.Logging;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class GameLogTests
    {
        sealed class RecordingSink : ILogSink
        {
            public readonly List<(LogLevel level, string channel, string message)> Lines = new List<(LogLevel, string, string)>();
            public void Write(LogLevel level, LogChannel channel, string message) => Lines.Add((level, channel.Name, message));
        }

        RecordingSink _sink;
        ILogSink _previousSink;

        [SetUp]
        public void SetUp()
        {
            _previousSink = GameLog.Sink;
            _sink = new RecordingSink();
            GameLog.Sink = _sink;
            GameLog.SetDisabledChannels(null);
        }

        [TearDown]
        public void TearDown()
        {
            GameLog.Sink = _previousSink;
            GameLog.SetDisabledChannels(null);
            LogChannels.ResetToBuiltInForTests();
        }

        [Test]
        public void BuiltInChannels_HaveDistinctBits()
        {
            var bits = new HashSet<int> { LogChannels.Default.Bit, LogChannels.Flow.Bit, LogChannels.Scene.Bit, LogChannels.Services.Bit, LogChannels.Events.Bit, LogChannels.Pool.Bit };
            Assert.That(bits.Count, Is.EqualTo(6));
        }

        [Test]
        public void Register_GameChannel_GetsNewBitAndName()
        {
            var fight = LogChannels.Register("Fight");

            Assert.That(fight.Name, Is.EqualTo("Fight"));
            Assert.That(fight.Bit, Is.EqualTo(LogChannels.Count - 1));
            Assert.That(fight, Is.Not.EqualTo(LogChannels.Flow));
        }

        [Test]
        public void Register_SameNameTwice_ReturnsSameChannel()
        {
            var a = LogChannels.Register("AI");
            var b = LogChannels.Register("AI");

            Assert.That(b, Is.EqualTo(a));
        }

        [Test]
        public void Register_MoreThan64Channels_Throws()
        {
            for (int i = LogChannels.Count; i < LogChannels.MaxChannels; i++)
                LogChannels.Register($"Extra{i}");

            Assert.Throws<System.InvalidOperationException>(() => LogChannels.Register("OneTooMany"));
        }

        [Test]
        public void DisabledChannel_DropsInfoAndWarning_ButKeepsError()
        {
            GameLog.SetEnabled(LogChannels.Flow, false);

            GameLog.Info(LogChannels.Flow, "info");
            GameLog.Warning(LogChannels.Flow, "warning");
            GameLog.Error(LogChannels.Flow, "error");
            GameLog.Info(LogChannels.Scene, "other channel");

            Assert.That(_sink.Lines, Has.Count.EqualTo(2));
            Assert.That(_sink.Lines[0], Is.EqualTo((LogLevel.Error, "Flow", "error")));
            Assert.That(_sink.Lines[1], Is.EqualTo((LogLevel.Info, "Scene", "other channel")));
        }

        [Test]
        public void DisabledByName_AppliesToChannelRegisteredLater()
        {
            // Tools/GameLog lưu theo tên; kênh của game có thể đăng ký sau khi danh sách đã được nạp.
            GameLog.SetDisabledChannels(new[] { "LateChannel" });

            var late = LogChannels.Register("LateChannel");
            GameLog.Info(late, "hidden");

            Assert.That(GameLog.IsEnabled(late), Is.False);
            Assert.That(_sink.Lines, Is.Empty);
        }

        [Test]
        public void ReEnable_RemovesNameFromDisabledList()
        {
            GameLog.SetEnabled(LogChannels.Pool, false);
            GameLog.SetEnabled(LogChannels.Pool, true);

            Assert.That(GameLog.IsEnabled(LogChannels.Pool), Is.True);
            Assert.That(GameLog.DisabledChannelNames, Does.Not.Contain("Pool"));
        }
    }
}
