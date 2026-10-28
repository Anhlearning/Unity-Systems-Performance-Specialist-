using System;
using Ignite.Core.UI;
using Ignite.Unity.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace Ignite.Unity.UI
{
    /// <summary>
    /// UI Stack dùng chung cả game, với một nút Back thống nhất: action UI/Cancel (Esc, nút B của gamepad).
    /// Back khi chỉ còn màn gốc (hoặc stack rỗng) thì phát BackAtRoot để game tự quyết, ví dụ mở Pause.
    /// </summary>
    public sealed class UIStackService : IDisposable
    {
        readonly UIStack _stack = new UIStack();
        readonly IDisposable _uiLayer;
        readonly InputAction _cancel;

        public UIStackService(InputService input)
        {
            EnsureEventSystem();

            if (input != null && input.HasActions && input.Reader != null && input.Actions.FindActionMap(input.Reader.UiMap) != null)
            {
                _uiLayer = input.PushLayer(input.Reader.UiMap);
                _cancel = input.Find(input.Reader.CancelActionPath);
                if (_cancel != null)
                    _cancel.performed += OnCancel;
            }
        }

        public int Count => _stack.Count;
        public IScreen Top => _stack.Top;

        public event Action BackAtRoot;

        public bool Contains(UIScreen screen) => _stack.Contains(screen);

        public void Push(UIScreen screen)
        {
            screen.Attach(this);
            _stack.Push(screen);
        }

        public IScreen Pop() => _stack.Pop();

        /// <summary>Bấm Back (bàn phím, gamepad, hay nút Back trên màn hình đều gọi hàm này).</summary>
        public void Back()
        {
            if (!_stack.Back())
                BackAtRoot?.Invoke();
        }

        /// <summary>Đóng một màn cụ thể nếu nó đang là màn trên cùng.</summary>
        public void Close(UIScreen screen)
        {
            if (ReferenceEquals(_stack.Top, screen))
                _stack.Pop();
            else
                _stack.Remove(screen);
        }

        internal void OnScreenDestroyed(UIScreen screen) => _stack.Remove(screen);

        void OnCancel(InputAction.CallbackContext context) => Back();

        /// <summary>
        /// UI Stack cần EventSystem để chọn sẵn phần tử cho gamepad ngay từ màn đầu tiên.
        /// Tạo một cái sống suốt game nếu chưa có, thay vì trông vào scene nào đó load trước.
        /// </summary>
        static void EnsureEventSystem()
        {
            if (!Application.isPlaying || EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("[EventSystem]", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public void Dispose()
        {
            if (_cancel != null)
                _cancel.performed -= OnCancel;
            _uiLayer?.Dispose();
        }
    }
}
