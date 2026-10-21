using Cysharp.Threading.Tasks;
using Ignite.Core.Events;
using Ignite.Core.Flow;
using Ignite.Core.Logging;
using Ignite.Unity.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace IgniteSample
{
    // Quy ước: MonoBehaviour trong scene nhận dependency qua method [Inject] và khởi tạo ở đó,
    // vì scope của nhóm scene chỉ được build sau khi cả nhóm load xong.

    /// <summary>Scene Gameplay: rỗng, Esc để Pause; màn Pause có Resume và về Menu.</summary>
    public sealed class SampleGameplayView : MonoBehaviour
    {
        FlowController _flow;
        GameObject _pausePanel;

        [Inject]
        void Construct(FlowController flow, IEventSubscriber events)
        {
            _flow = flow;

            var canvas = SampleUi.CreateCanvas(transform, "GameplayCanvas", 10);
            SampleUi.CreateLabel(canvas.transform, "GAMEPLAY (rỗng) · bấm Esc để Pause", new Vector2(0.5f, 0.9f), 28);

            _pausePanel = SampleUi.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.7f)).gameObject;
            SampleUi.CreateLabel(_pausePanel.transform, "PAUSE", new Vector2(0.5f, 0.65f), 48);
            SampleUi.CreateButton(_pausePanel.transform, "Resume", new Vector2(0.5f, 0.48f), () => _flow.GoAsync("Gameplay").Forget());
            SampleUi.CreateButton(_pausePanel.transform, "Menu", new Vector2(0.5f, 0.36f), () => _flow.GoAsync("Menu").Forget());
            _pausePanel.SetActive(false);

            // Đăng ký qua IEventSubscriber của scope nhóm Gameplay: rời nhóm là tự huỷ.
            events.Subscribe<FlowStateChanged>(OnFlowChanged);
        }

        void OnFlowChanged(FlowStateChanged change)
        {
            bool paused = change.To == "Pause";
            _pausePanel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (_flow == null || keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            if (_flow.Current == "Gameplay")
                _flow.GoAsync("Pause").Forget();
            else if (_flow.Current == "Pause")
                _flow.GoAsync("Gameplay").Forget();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
