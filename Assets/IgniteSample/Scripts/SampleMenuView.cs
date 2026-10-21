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

    /// <summary>Scene Menu: một nút Play.</summary>
    public sealed class SampleMenuView : MonoBehaviour
    {
        [Inject]
        void Construct(FlowController flow, SampleSession session)
        {
            var canvas = SampleUi.CreateCanvas(transform, "MenuCanvas", 0);
            SampleUi.CreatePanel(canvas.transform, new Color(0.08f, 0.1f, 0.16f));
            SampleUi.CreateLabel(canvas.transform, "IGNITE SAMPLE · MENU", new Vector2(0.5f, 0.65f), 44);
            SampleUi.CreateLabel(canvas.transform, $"Số trận đã chơi: {session.RunsStarted}", new Vector2(0.5f, 0.55f), 24);
            SampleUi.CreateButton(canvas.transform, "Play", new Vector2(0.5f, 0.4f), () =>
            {
                session.StartRun();
                flow.GoAsync("Gameplay").Forget();
            });
        }
    }
}
