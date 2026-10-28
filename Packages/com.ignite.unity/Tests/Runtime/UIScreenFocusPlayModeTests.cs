using System.Collections;
using Ignite.Unity.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Ignite.Unity.Tests
{
    /// <summary>Focus cho gamepad: chọn sẵn phần tử đầu tiên; quay lại màn cũ thì focus vẫn ở chỗ cũ.</summary>
    public class UIScreenFocusPlayModeTests
    {
        GameObject _root;

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        static Button AddButton(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Button>();
        }

        UIScreen AddScreen(string name, out Button first, out Button second, bool popup = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            first = AddButton(go.transform, name + ".first");
            second = AddButton(go.transform, name + ".second");
            var screen = go.AddComponent<UIScreen>();
            screen.Configure(popup, first);
            go.SetActive(false);
            return screen;
        }

        [UnityTest]
        public IEnumerator ReturningToScreen_RestoresPreviousSelection()
        {
            _root = new GameObject("UIRoot", typeof(Canvas), typeof(EventSystem));
            var events = _root.GetComponent<EventSystem>();
            var ui = new UIStackService(null);
            var menu = AddScreen("Menu", out var menuFirst, out var menuSecond);
            var settings = AddScreen("Settings", out var settingsFirst, out _);
            yield return null;

            ui.Push(menu);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(menuFirst.gameObject), "Mở màn thì chọn sẵn phần tử đầu tiên.");

            events.SetSelectedGameObject(menuSecond.gameObject); // người chơi di chuyển xuống nút thứ hai
            ui.Push(settings);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(settingsFirst.gameObject));
            Assert.That(menu.gameObject.activeSelf, Is.False);

            ui.Back();
            yield return null;

            Assert.That(menu.gameObject.activeSelf, Is.True);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(menuSecond.gameObject), "Quay lại thì focus về đúng chỗ cũ.");
            ui.Dispose();
        }

        [UnityTest]
        public IEnumerator Popup_KeepsScreenBelowVisible_ButNotInteractable()
        {
            _root = new GameObject("UIRoot", typeof(Canvas), typeof(EventSystem));
            var ui = new UIStackService(null);
            var game = AddScreen("Game", out _, out _);
            var confirm = AddScreen("Confirm", out _, out _, popup: true);
            yield return null;

            ui.Push(game);
            ui.Push(confirm);

            Assert.That(game.gameObject.activeSelf, Is.True);
            Assert.That(game.GetComponent<CanvasGroup>().interactable, Is.False);
            ui.Dispose();
        }

        [UnityTest]
        public IEnumerator DestroyedScreen_IsRemovedFromStack()
        {
            _root = new GameObject("UIRoot", typeof(Canvas), typeof(EventSystem));
            var ui = new UIStackService(null);
            var menu = AddScreen("Menu", out _, out _);
            var settings = AddScreen("Settings", out _, out _);
            ui.Push(menu);
            ui.Push(settings);

            Object.Destroy(settings.gameObject); // scene của nó bị unload
            yield return null;

            Assert.That(ui.Count, Is.EqualTo(1));
            Assert.That(ui.Top, Is.SameAs(menu));
            ui.Dispose();
        }
    }
}
