using Ignite.Core.Events;
using Ignite.Core.Flow;
using Ignite.Core.Logging;
using Ignite.Unity.Flow;
using Ignite.Unity.Input;
using Ignite.Unity.Localization;
using Ignite.Unity.Settings;
using Ignite.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace IgniteSample
{
    /// <summary>
    /// Scene SharedUI có trong cả hai nhóm, nên được giữ lại (không load lại) khi đổi nhóm.
    /// Chứa EventSystem và màn Settings dùng chung cho Menu và Pause.
    /// </summary>
    public sealed class SampleSharedHud : MonoBehaviour
    {
        Text _state;
        int _injections;

        [Inject]
        void Construct(IEventSubscriber events, FlowController flow, SampleScreens screens,
            SettingsService settings, InputService input, InputRebinder rebinder, UIStackService ui, LocalizationService localization)
        {
            _injections++;
            if (_state == null)
            {
                SampleUi.EnsureEventSystem(transform);
                var canvas = SampleUi.CreateCanvas(transform, "SharedCanvas", 100);
                _state = SampleUi.CreateLabel(canvas.transform, string.Empty, new Vector2(0.5f, 0.03f), 18);

                var settingsCanvas = SampleUi.CreateCanvas(transform, "SettingsCanvas", 50);
                var settingsScreen = SampleUi.CreateScreen<SampleSettingsScreen>(settingsCanvas.transform, "SettingsScreen", new Color(0.05f, 0.06f, 0.1f, 0.97f));
                settingsScreen.Build(settings, input, rebinder, ui, localization);
                settingsScreen.gameObject.SetActive(false);
                screens.Settings = settingsScreen;
            }

            // Mỗi lần đổi nhóm, scope cũ bị huỷ (kéo theo đăng ký cũ) và scene được inject lại từ scope mới.
            events.Subscribe<FlowStateChanged>(change => Show(change.To));
            Show(flow.Current ?? "(đang vào)");
            GameLog.Info(SampleLog.Sample, $"SharedUI được inject lần {_injections} (scene giữ nguyên, không load lại)");
        }

        void Show(string state)
        {
            _state.text = $"Shared UI · state: {state} · inject #{_injections}";
        }
    }
}
