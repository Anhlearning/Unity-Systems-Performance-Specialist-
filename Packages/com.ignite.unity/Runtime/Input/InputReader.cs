using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ignite.Unity.Input
{
    public enum InputScheme
    {
        KeyboardMouse,
        Gamepad,
        Touch,
    }

    /// <summary>
    /// Dữ liệu input của game: InputActionAsset, tên control scheme, map dùng cho UI, bộ icon gợi ý phím.
    /// Chỉ là dữ liệu; trạng thái lúc chạy (layer nào đang bật) nằm ở InputService.
    /// </summary>
    [CreateAssetMenu(menuName = "Ignite/Input Reader", fileName = "InputReader")]
    public sealed class InputReader : ScriptableObject
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] string keyboardMouseScheme = "Keyboard&Mouse";
        [SerializeField] string gamepadScheme = "Gamepad";
        [SerializeField] string touchScheme = "Touch";
        [SerializeField] string uiMap = "UI";
        [SerializeField] string cancelAction = "Cancel";
        [SerializeField] List<PromptIconSet> promptIcons = new List<PromptIconSet>();

        public InputActionAsset Actions => actions;
        public string UiMap => uiMap;
        public string CancelActionPath => $"{uiMap}/{cancelAction}";
        public IReadOnlyList<PromptIconSet> PromptIcons => promptIcons;

        public string SchemeName(InputScheme scheme)
        {
            switch (scheme)
            {
                case InputScheme.Gamepad: return gamepadScheme;
                case InputScheme.Touch: return touchScheme;
                default: return keyboardMouseScheme;
            }
        }

        /// <summary>Dùng cho test và tool dựng sẵn.</summary>
        public static InputReader Create(InputActionAsset asset)
        {
            var reader = CreateInstance<InputReader>();
            reader.actions = asset;
            return reader;
        }
    }

}
