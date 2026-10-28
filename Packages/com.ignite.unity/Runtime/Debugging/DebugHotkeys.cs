using UnityEngine;
using UnityEngine.InputSystem;

namespace Ignite.Unity.Debugging
{
    /// <summary>Phím ` mở/đóng console, F3 bật/tắt overlay. Tách khỏi phần vẽ để lúc đóng không tốn OnGUI mỗi frame.</summary>
    public sealed class DebugHotkeys : MonoBehaviour
    {
        DebugConsoleGui _console;
        StatsOverlay _overlay;

        public void Bind(DebugConsoleGui console, StatsOverlay overlay)
        {
            _console = console;
            _overlay = overlay;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (_console != null && keyboard.backquoteKey.wasPressedThisFrame)
                _console.enabled = !_console.enabled;
            if (_overlay != null && keyboard.f3Key.wasPressedThisFrame)
                _overlay.Visible = !_overlay.Visible;
        }
    }
}
