using System;
using Cysharp.Threading.Tasks;
using Ignite.Core.Events;
using Ignite.Core.Flow;
using Ignite.Core.Logging;
using Ignite.Unity.Scenes;

namespace Ignite.Unity.Flow
{
    /// <summary>
    /// Nối Game Flow với Scene Group: mỗi lần chuyển state, nếu state mới có nhóm scene khác nhóm đang mở thì load nhóm đó.
    /// Exit state cũ chạy trước khi load, Enter state mới chạy sau khi load xong, rồi phát FlowStateChanged.
    /// </summary>
    public sealed class FlowController
    {
        readonly GameFlowAsset _asset;
        readonly GameFlow _flow;
        readonly SceneGroupLoader _loader;
        readonly IEventPublisher _events;

        public FlowController(GameFlowAsset asset, SceneGroupLoader loader, IEventPublisher events)
        {
            _asset = asset;
            _loader = loader;
            _events = events;
            if (asset != null)
                _flow = new GameFlow(asset.ToDefinition());
        }

        public bool HasFlow => _flow != null;
        public string InitialState => _asset != null ? _asset.InitialState : null;
        public string Current => _flow?.Current;
        public bool IsBusy { get; private set; }

        public bool CanGo(string to) => _flow != null && !IsBusy && _flow.CanGo(to);

        public void SetHandler(string state, IFlowStateHandler handler) => _flow.SetHandler(state, handler);

        /// <summary>State đầu tiên có nhóm scene chứa scene này; null nếu không state nào chứa.</summary>
        public string FindStateContaining(string scenePath)
        {
            if (_asset == null)
                return null;
            foreach (var state in _asset.States)
            {
                if (state.sceneGroup != null && state.sceneGroup.Contains(scenePath))
                    return state.id;
            }
            return null;
        }

        /// <summary>Trả về false (kèm cảnh báo) nếu chuyển state không hợp lệ hoặc đang chuyển dở.</summary>
        public async UniTask<bool> GoAsync(string to)
        {
            if (!CanGo(to))
            {
                GameLog.Warning(LogChannels.Flow, $"Bỏ qua chuyển '{Current ?? "(chưa bắt đầu)"}' → '{to}'" + (IsBusy ? ": đang chuyển state khác." : ": không được khai báo trong Game Flow."));
                return false;
            }

            IsBusy = true;
            string from = _flow.Current;
            try
            {
                _flow.BeginTransition(to);

                var group = _asset.GroupOf(to);
                if (group != null && group != _loader.Current)
                    await _loader.LoadAsync(group);

                _flow.CompleteTransition();
                GameLog.Info(LogChannels.Flow, $"{from ?? "(bắt đầu)"} → {to}");
                _events.Publish(new FlowStateChanged(from, to));
                return true;
            }
            catch (Exception e)
            {
                _flow.AbortTransition();
                GameLog.Error(LogChannels.Flow, $"Chuyển '{from}' → '{to}' thất bại: {e}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
