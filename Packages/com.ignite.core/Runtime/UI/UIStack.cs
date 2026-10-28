using System;
using System.Collections.Generic;

namespace Ignite.Core.UI
{
    /// <summary>Một màn hình trong UI Stack. Popup thì màn bên dưới vẫn hiện (chỉ mất focus).</summary>
    public interface IScreen
    {
        bool IsPopup { get; }
        void OnShow();
        void OnHide();
        void OnFocus();
        void OnBlur();
    }

    /// <summary>
    /// Ngăn xếp màn hình: Push mở màn mới, Pop/Back quay lại màn trước.
    /// Màn thường che màn dưới (OnHide); popup chỉ làm màn dưới mất focus (OnBlur).
    /// </summary>
    public sealed class UIStack
    {
        readonly List<IScreen> _screens = new List<IScreen>();

        public int Count => _screens.Count;
        public IScreen Top => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        public event Action<IScreen> TopChanged;

        public bool Contains(IScreen screen) => _screens.Contains(screen);

        public void Push(IScreen screen)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));
            if (_screens.Contains(screen))
                throw new InvalidOperationException("Màn hình đã có trong stack.");

            var below = Top;
            if (below != null)
            {
                below.OnBlur();
                if (!screen.IsPopup)
                    below.OnHide();
            }

            _screens.Add(screen);
            screen.OnShow();
            screen.OnFocus();
            TopChanged?.Invoke(screen);
        }

        public IScreen Pop()
        {
            var top = Top;
            if (top == null)
                return null;

            top.OnBlur();
            top.OnHide();
            _screens.RemoveAt(_screens.Count - 1);

            var below = Top;
            if (below != null)
            {
                if (!top.IsPopup)
                    below.OnShow();
                below.OnFocus();
            }
            TopChanged?.Invoke(below);
            return top;
        }

        /// <summary>Nút Back dùng chung. Không đóng màn gốc: trả về false để game tự quyết (ví dụ mở Pause).</summary>
        public bool Back()
        {
            if (_screens.Count <= 1)
                return false;
            Pop();
            return true;
        }

        /// <summary>Gỡ một màn bị huỷ ngoài ý muốn (scene của nó bị unload) mà không gọi callback lên nó.</summary>
        public void Remove(IScreen screen)
        {
            int index = _screens.IndexOf(screen);
            if (index < 0)
                return;

            bool wasTop = index == _screens.Count - 1;
            _screens.RemoveAt(index);
            if (wasTop && Top != null)
            {
                Top.OnShow();
                Top.OnFocus();
                TopChanged?.Invoke(Top);
            }
        }

        public void Clear()
        {
            while (Top != null)
                Pop();
        }
    }
}
