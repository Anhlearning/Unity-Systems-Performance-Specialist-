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

    /// <summary>Scene SharedUI có trong cả hai nhóm, nên được giữ lại (không load lại) khi đổi nhóm.</summary>
    public sealed class SampleSharedHud : MonoBehaviour
    {
        Text _state;
        int _injections;

        [Inject]
        void Construct(IEventSubscriber events, FlowController flow)
        {
            _injections++;
            if (_state == null)
            {
                SampleUi.EnsureEventSystem(transform);
                var canvas = SampleUi.CreateCanvas(transform, "SharedCanvas", 100);
                _state = SampleUi.CreateLabel(canvas.transform, string.Empty, new Vector2(0.5f, 0.05f), 20);
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
