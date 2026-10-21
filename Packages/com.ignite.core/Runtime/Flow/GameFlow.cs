using System;
using System.Collections.Generic;

namespace Ignite.Core.Flow
{
    /// <summary>Danh sách state và chuyển state hợp lệ. Game khai báo, framework chỉ kiểm tra.</summary>
    public sealed class FlowDefinition
    {
        readonly HashSet<string> _states = new HashSet<string>();
        readonly HashSet<(string from, string to)> _transitions = new HashSet<(string, string)>();

        public IReadOnlyCollection<string> States => _states;

        public FlowDefinition AddState(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id state không được rỗng.", nameof(id));
            if (!_states.Add(id))
                throw new ArgumentException($"State '{id}' bị khai báo hai lần.", nameof(id));
            return this;
        }

        public FlowDefinition AddTransition(string from, string to)
        {
            if (!_states.Contains(from))
                throw new ArgumentException($"Chuyển state từ '{from}' nhưng state này chưa được khai báo.", nameof(from));
            if (!_states.Contains(to))
                throw new ArgumentException($"Chuyển state tới '{to}' nhưng state này chưa được khai báo.", nameof(to));
            _transitions.Add((from, to));
            return this;
        }

        public bool HasState(string id) => id != null && _states.Contains(id);

        public bool CanTransition(string from, string to) => _transitions.Contains((from, to));
    }

    /// <summary>Code của game chạy khi vào/ra một state (ví dụ Pause đặt timeScale).</summary>
    public interface IFlowStateHandler
    {
        void Enter();
        void Exit();
    }

    /// <summary>Phát qua event bus mỗi khi đổi state xong. From là null ở lần vào state đầu tiên.</summary>
    public readonly struct FlowStateChanged
    {
        public readonly string From;
        public readonly string To;

        public FlowStateChanged(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// State machine cấp game, chia chuyển state làm hai pha để tầng Unity chèn việc load scene vào giữa:
    /// BeginTransition (kiểm tra + Exit state cũ) → load scene → CompleteTransition (Enter state mới).
    /// </summary>
    public sealed class GameFlow
    {
        readonly FlowDefinition _definition;
        readonly Dictionary<string, IFlowStateHandler> _handlers = new Dictionary<string, IFlowStateHandler>();

        public GameFlow(FlowDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>State hiện tại; null trước khi vào state đầu tiên.</summary>
        public string Current { get; private set; }

        /// <summary>State đang chuyển tới; null khi không có chuyển state nào dở dang.</summary>
        public string Pending { get; private set; }

        public bool IsTransitioning => Pending != null;

        public event Action<string, string> StateChanged;

        public void SetHandler(string state, IFlowStateHandler handler)
        {
            if (!_definition.HasState(state))
                throw new ArgumentException($"State '{state}' chưa được khai báo.", nameof(state));
            _handlers[state] = handler;
        }

        /// <summary>Lần đầu được vào bất kỳ state nào (Play từ scene Gameplay trong Editor); sau đó chỉ đi theo chuyển state đã khai báo.</summary>
        public bool CanGo(string to)
        {
            if (IsTransitioning || !_definition.HasState(to))
                return false;
            return Current == null || _definition.CanTransition(Current, to);
        }

        public void BeginTransition(string to)
        {
            if (!CanGo(to))
            {
                string reason = IsTransitioning ? $"đang chuyển tới '{Pending}'" : "chuyển state không được khai báo";
                throw new InvalidOperationException($"Không thể chuyển '{Current ?? "(chưa bắt đầu)"}' → '{to}': {reason}.");
            }

            Pending = to;
            if (Current != null && _handlers.TryGetValue(Current, out var handler))
                handler?.Exit();
        }

        public void CompleteTransition()
        {
            if (!IsTransitioning)
                throw new InvalidOperationException("Không có chuyển state nào để hoàn tất.");

            string from = Current;
            Current = Pending;
            Pending = null;

            if (_handlers.TryGetValue(Current, out var handler))
                handler?.Enter();
            StateChanged?.Invoke(from, Current);
        }

        /// <summary>Huỷ chuyển state dở dang (ví dụ load scene lỗi): vẫn ở state cũ và Enter lại nó.</summary>
        public void AbortTransition()
        {
            if (!IsTransitioning)
                return;

            Pending = null;
            if (Current != null && _handlers.TryGetValue(Current, out var handler))
                handler?.Enter();
        }

        public void Go(string to)
        {
            BeginTransition(to);
            CompleteTransition();
        }
    }
}
