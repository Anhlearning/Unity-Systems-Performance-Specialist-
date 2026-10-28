using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Ignite.Unity.Input
{
    /// <summary>
    /// Theo dõi thiết bị người chơi dùng gần nhất (bấm nút nào thì thiết bị đó là hiện tại),
    /// để icon gợi ý phím đổi theo: bàn phím/chuột, gamepad, Steam Deck, cảm ứng.
    /// </summary>
    public sealed class DeviceTracker : IDisposable
    {
        readonly IDisposable _listener;

        public DeviceTracker(bool listen = true)
        {
            if (listen)
                _listener = InputSystem.onAnyButtonPress.Call(control => Observe(control.device));
        }

        public InputScheme Current { get; private set; } = InputScheme.KeyboardMouse;
        public bool IsSteamDeck { get; private set; }
        public InputDevice LastDevice { get; private set; }

        public event Action<InputScheme> Changed;

        public void Observe(InputDevice device)
        {
            if (device == null)
                return;

            LastDevice = device;
            var scheme = Classify(device);
            bool deck = IsSteamDeckDevice(device);
            if (scheme == Current && deck == IsSteamDeck)
                return;

            Current = scheme;
            IsSteamDeck = deck;
            Changed?.Invoke(scheme);
        }

        public static InputScheme Classify(InputDevice device)
        {
            if (device is Gamepad || device is Joystick)
                return InputScheme.Gamepad;
            if (device is Touchscreen)
                return InputScheme.Touch;
            return InputScheme.KeyboardMouse;
        }

        public static bool IsSteamDeckDevice(InputDevice device)
        {
            string product = device.description.product ?? string.Empty;
            return device is Gamepad && product.IndexOf("Steam Deck", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Dispose() => _listener?.Dispose();
    }
}
