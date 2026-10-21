using Ignite.Core.Logging;
using UnityEngine;

namespace Ignite.Unity.Logging
{
    /// <summary>In log ra Console của Unity, kèm số frame và tên kênh.</summary>
    public sealed class UnityLogSink : ILogSink
    {
        public void Write(LogLevel level, LogChannel channel, string message)
        {
            string line = $"[F{Time.frameCount}][{channel.Name}] {message}";
            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(line);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(line);
                    break;
                default:
                    Debug.LogError(line);
                    break;
            }
        }
    }

    /// <summary>Cài UnityLogSink trước khi scene đầu tiên load, và nạp các kênh đã tắt trong Tools/GameLog.</summary>
    public static class GameLogBootstrap
    {
        public const string DisabledChannelsPrefsKey = "Ignite.GameLog.DisabledChannels";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            GameLog.Sink = new UnityLogSink();
#if UNITY_EDITOR
            string saved = UnityEditor.EditorPrefs.GetString(DisabledChannelsPrefsKey, string.Empty);
            GameLog.SetDisabledChannels(saved.Split(';'));
#endif
        }
    }
}
