using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using Ignite.Unity.Bootstrap;
using Ignite.Unity.Flow;
using VContainer.Unity;

namespace Ignite.Unity.Services
{
    /// <summary>
    /// Chạy khi scope gốc build xong: quyết định vào state nào đầu tiên.
    /// Editor: nếu bấm Play ở một scene thuộc Game Flow thì vào thẳng state chứa scene đó;
    /// scene ngoài Game Flow thì chỉ load lại scene đó. Build: vào InitialState.
    /// </summary>
    public sealed class GameBootstrapper : IStartable
    {
        readonly FlowController _flow;

        public GameBootstrapper(FlowController flow)
        {
            _flow = flow;
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        async UniTaskVoid RunAsync()
        {
            string[] pending = BootLoader.TakePendingScenes();

            if (!_flow.HasFlow)
            {
                GameLog.Warning(LogChannels.Flow, "RootLifetimeScope chưa có GameFlowAsset, chỉ load lại các scene đang mở.");
                await BootLoader.LoadStartScenesAsync(pending);
                return;
            }

            if (pending.Length > 0)
            {
                string state = _flow.FindStateContaining(pending[0]);
                if (state == null)
                {
                    GameLog.Warning(LogChannels.Flow, $"'{pending[0]}' không thuộc state nào của Game Flow, load không qua Game Flow.");
                    await BootLoader.LoadStartScenesAsync(pending);
                    return;
                }

                await _flow.GoAsync(state);
                return;
            }

            await _flow.GoAsync(_flow.InitialState);
        }
    }
}
