using Ignite.Core.Logging;
using Ignite.Unity.Logging;
using UnityEditor;
using UnityEngine;

namespace Ignite.Unity.Editor.Logging
{
    /// <summary>Tools/GameLog: bật tắt từng kênh log, lưu trong EditorPrefs.</summary>
    public sealed class GameLogWindow : EditorWindow
    {
        Vector2 _scroll;

        [InitializeOnLoadMethod]
        static void InstallEditorSink()
        {
            // Log trong edit mode (tool, test) cũng in ra Console.
            GameLog.Sink = new UnityLogSink();
            GameLog.SetDisabledChannels(EditorPrefs.GetString(GameLogBootstrap.DisabledChannelsPrefsKey, string.Empty).Split(';'));
        }

        [MenuItem("Tools/GameLog")]
        static void Open() => GetWindow<GameLogWindow>("GameLog");

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Kênh của game chỉ hiện sau khi class khai báo nó được dùng tới (thường là sau một lần Play).", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bật tất cả"))
                    SetAll(true);
                if (GUILayout.Button("Tắt tất cả"))
                    SetAll(false);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < LogChannels.Count; i++)
            {
                var channel = LogChannels.At(i);
                bool enabled = GameLog.IsEnabled(channel);
                bool next = EditorGUILayout.ToggleLeft(channel.Name, enabled);
                if (next != enabled)
                {
                    GameLog.SetEnabled(channel, next);
                    Save();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        static void SetAll(bool enabled)
        {
            for (int i = 0; i < LogChannels.Count; i++)
                GameLog.SetEnabled(LogChannels.At(i), enabled);
            Save();
        }

        static void Save()
        {
            EditorPrefs.SetString(GameLogBootstrap.DisabledChannelsPrefsKey, string.Join(";", GameLog.DisabledChannelNames));
        }
    }
}
