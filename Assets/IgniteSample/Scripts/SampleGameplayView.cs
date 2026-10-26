using Cysharp.Threading.Tasks;
using Ignite.Core.Events;
using Ignite.Core.Flow;
using Ignite.Unity.Audio;
using Ignite.Unity.Flow;
using Ignite.Unity.Localization;
using Ignite.Unity.UI;
using UnityEngine;
using VContainer;

namespace IgniteSample
{
    // Quy ước: MonoBehaviour trong scene nhận dependency qua method [Inject] và khởi tạo ở đó,
    // vì scope của nhóm scene chỉ được build sau khi cả nhóm load xong.

    /// <summary>
    /// Scene Gameplay. Nút Back (Esc / nút B gamepad) khi không có màn nào mở thì Pause.
    /// Pause là popup trong UI Stack: Resume, Settings, Menu. Back trong Pause thì chơi tiếp.
    /// </summary>
    public sealed class SampleGameplayView : MonoBehaviour
    {
        FlowController _flow;
        UIStackService _ui;
        UIScreen _pause;

        [Inject]
        void Construct(FlowController flow, IEventSubscriber events, UIStackService ui, SampleScreens screens, AudioService audio,
            LocalizationService localization)
        {
            audio.StopMusic();
            _flow = flow;
            _ui = ui;

            var canvas = SampleUi.CreateCanvas(transform, "GameplayCanvas", 10);
            SampleUi.Localize(SampleUi.CreateLabel(canvas.transform, "", new Vector2(0.5f, 0.93f), 26), localization, "gameplay.hint");

            _pause = SampleUi.CreateScreen<UIScreen>(canvas.transform, "PauseScreen", new Color(0f, 0f, 0f, 0.7f));
            SampleUi.Localize(SampleUi.CreateLabel(_pause.transform, "", new Vector2(0.5f, 0.68f), 48), localization, "pause.title");
            var resume = SampleUi.CreateButton(_pause.transform, "", new Vector2(0.5f, 0.52f), () => _flow.GoAsync("Gameplay").Forget());
            SampleUi.Localize(resume, localization, "pause.resume");
            var settings = SampleUi.CreateButton(_pause.transform, "", new Vector2(0.5f, 0.42f), () =>
            {
                if (screens.Settings != null && !_ui.Contains(screens.Settings))
                    _ui.Push(screens.Settings);
            });
            SampleUi.Localize(settings, localization, "menu.settings");
            SampleUi.Localize(SampleUi.CreateButton(_pause.transform, "", new Vector2(0.5f, 0.32f), () => _flow.GoAsync("Menu").Forget()), localization, "pause.menu");
            _pause.Configure(popup: true, first: resume);
            _pause.gameObject.SetActive(false);

            // Đăng ký qua IEventSubscriber của scope nhóm Gameplay: rời nhóm là tự huỷ.
            events.Subscribe<FlowStateChanged>(OnFlowChanged);
            _ui.BackAtRoot += OnBackAtRoot;
        }

        public UIScreen PauseScreen => _pause;

        void OnBackAtRoot()
        {
            if (_flow.Current == "Gameplay")
                _flow.GoAsync("Pause").Forget();
            else if (_flow.Current == "Pause")
                _flow.GoAsync("Gameplay").Forget();
        }

        void OnFlowChanged(FlowStateChanged change)
        {
            if (change.To == "Pause")
            {
                Time.timeScale = 0f;
                if (!_ui.Contains(_pause))
                    _ui.Push(_pause);
            }
            else if (change.From == "Pause")
            {
                Time.timeScale = 1f;
                while (_ui.Contains(_pause))
                    _ui.Close(_ui.Top as UIScreen);
            }
        }

        void OnDestroy()
        {
            // Event C# thường của service gốc không tự huỷ như IEventSubscriber, nên phải gỡ tay.
            if (_ui != null)
                _ui.BackAtRoot -= OnBackAtRoot;
            Time.timeScale = 1f;
        }
    }
}
