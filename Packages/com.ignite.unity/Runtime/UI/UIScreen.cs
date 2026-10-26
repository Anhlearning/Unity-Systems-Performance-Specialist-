using Ignite.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ignite.Unity.UI
{
    /// <summary>
    /// Một màn hình trong UI Stack. Focus cho gamepad: lần đầu chọn sẵn firstSelected;
    /// quay lại màn này thì focus trở về đúng phần tử đã chọn trước đó.
    /// </summary>
    public class UIScreen : MonoBehaviour, IScreen
    {
        [SerializeField] bool isPopup;
        [SerializeField] Selectable firstSelected;

        CanvasGroup _group;
        GameObject _lastSelected;
        UIStackService _owner;

        public bool IsPopup => isPopup;
        public GameObject LastSelected => _lastSelected;
        public Selectable FirstSelected => firstSelected;

        CanvasGroup Group
        {
            get
            {
                // Không dùng "??" với component: Unity trả về object "null giả" mà toán tử ?? không nhận ra.
                if (_group == null && !TryGetComponent(out _group))
                    _group = gameObject.AddComponent<CanvasGroup>();
                return _group;
            }
        }

        /// <summary>Cho UI dựng bằng code.</summary>
        public void Configure(bool popup, Selectable first)
        {
            isPopup = popup;
            firstSelected = first;
        }

        public virtual void OnShow() => gameObject.SetActive(true);

        public virtual void OnHide() => gameObject.SetActive(false);

        public virtual void OnFocus()
        {
            Group.interactable = true;
            Group.blocksRaycasts = true;

            var target = _lastSelected != null && _lastSelected.activeInHierarchy && _lastSelected.transform.IsChildOf(transform)
                ? _lastSelected
                : firstSelected != null ? firstSelected.gameObject : null;
            if (target != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        public virtual void OnBlur()
        {
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null && events.currentSelectedGameObject.transform.IsChildOf(transform))
                _lastSelected = events.currentSelectedGameObject;

            // Màn bên dưới popup vẫn hiện nhưng không nhận click hay focus.
            Group.interactable = false;
            Group.blocksRaycasts = false;
        }

        internal void Attach(UIStackService owner) => _owner = owner;

        protected virtual void OnDestroy() => _owner?.OnScreenDestroyed(this);
    }
}
