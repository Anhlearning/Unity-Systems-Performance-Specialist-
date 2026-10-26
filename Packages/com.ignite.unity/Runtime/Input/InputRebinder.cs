using System;
using Ignite.Core.Logging;
using Ignite.Core.Settings;
using UnityEngine.InputSystem;

namespace Ignite.Unity.Input
{
    /// <summary>
    /// Đổi phím cho một binding: chờ người chơi bấm phím mới (Esc để huỷ), rồi lưu toàn bộ override vào settings.
    /// Khởi động lại game thì SettingsService nạp lại override, nên phím đã đổi vẫn còn.
    /// </summary>
    public sealed class InputRebinder : IDisposable
    {
        readonly InputService _input;
        readonly SettingsStore _settings;
        InputActionRebindingExtensions.RebindingOperation _operation;

        public InputRebinder(InputService input, SettingsStore settings)
        {
            _input = input;
            _settings = settings;
        }

        public bool IsRebinding => _operation != null;

        /// <summary>Index binding đầu tiên thuộc scheme (ví dụ binding bàn phím của "Player/Jump").</summary>
        public int FindBindingIndex(string actionPath, InputScheme scheme)
        {
            var action = _input.Find(actionPath);
            if (action == null || _input.Reader == null)
                return -1;
            string group = _input.Reader.SchemeName(scheme);
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite && binding.groups != null && binding.groups.Contains(group))
                    return i;
            }
            return -1;
        }

        public void Start(string actionPath, int bindingIndex, Action<bool> done = null)
        {
            if (IsRebinding)
                throw new InvalidOperationException("Đang đổi phím một binding khác.");

            var action = _input.Find(actionPath) ?? throw new ArgumentException($"Không có action '{actionPath}'.");
            bool wasEnabled = action.enabled;
            action.Disable(); // Input System yêu cầu action tắt trong lúc rebind

            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(_ => Finish(action, wasEnabled, true, done))
                .OnCancel(_ => Finish(action, wasEnabled, false, done))
                .Start();
        }

        public void Cancel() => _operation?.Cancel();

        public void ResetAll()
        {
            _input.LoadOverrides(string.Empty);
            _settings.Apply(s => s.BindingOverrides = string.Empty);
        }

        void Finish(InputAction action, bool wasEnabled, bool completed, Action<bool> done)
        {
            _operation?.Dispose();
            _operation = null;
            if (wasEnabled)
                action.Enable();

            if (completed)
            {
                _settings.Apply(s => s.BindingOverrides = _input.SaveOverrides());
                GameLog.Info(InputService.Log, $"Đổi phím {action.name}: {action.GetBindingDisplayString()}");
            }
            done?.Invoke(completed);
        }

        public void Dispose() => Cancel();
    }
}
