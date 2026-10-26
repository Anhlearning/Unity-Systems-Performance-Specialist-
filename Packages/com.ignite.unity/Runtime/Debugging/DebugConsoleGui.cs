using System.Collections.Generic;
using Ignite.Core.Debugging;
using UnityEngine;

namespace Ignite.Unity.Debugging
{
    /// <summary>Console vẽ bằng IMGUI (chỉ là tool debug). Chỉ được bật khi đang mở.</summary>
    public sealed class DebugConsoleGui : MonoBehaviour
    {
        readonly List<string> _lines = new List<string>();
        DebugCommands _commands;
        string _input = string.Empty;
        Vector2 _scroll;

        public IReadOnlyList<string> Lines => _lines;

        public void Bind(DebugCommands commands) => _commands = commands;

        public string Submit(string line)
        {
            string output = _commands.Execute(line);
            _lines.Add("> " + line);
            if (!string.IsNullOrEmpty(output))
                _lines.Add(output);
            if (_lines.Count > 200)
                _lines.RemoveRange(0, _lines.Count - 200);
            return output;
        }

        void OnGUI()
        {
            const float height = 260f;
            GUILayout.BeginArea(new Rect(10f, 10f, Screen.width - 20f, height), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (string line in _lines)
                GUILayout.Label(line);
            GUILayout.EndScrollView();

            GUI.SetNextControlName("DebugInput");
            bool submit = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            _input = GUILayout.TextField(_input);
            GUI.FocusControl("DebugInput");
            GUILayout.EndArea();

            if (submit && !string.IsNullOrWhiteSpace(_input))
            {
                Submit(_input);
                _input = string.Empty;
                _scroll.y = float.MaxValue;
            }
        }
    }
}
