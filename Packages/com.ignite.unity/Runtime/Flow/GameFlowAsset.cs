using System;
using System.Collections.Generic;
using Ignite.Core.Flow;
using Ignite.Unity.Scenes;
using UnityEngine;

namespace Ignite.Unity.Flow
{
    /// <summary>
    /// Game khai báo state của mình ở đây: id, nhóm scene, và được phép chuyển tới state nào.
    /// State không có nhóm scene (ví dụ Pause) thì giữ nguyên các scene đang mở.
    /// </summary>
    [CreateAssetMenu(menuName = "Ignite/Game Flow", fileName = "GameFlow")]
    public sealed class GameFlowAsset : ScriptableObject
    {
        [Serializable]
        public sealed class State
        {
            public string id;
            [Tooltip("Để trống nếu state này không đổi scene (ví dụ Pause).")]
            public SceneGroupAsset sceneGroup;
            public List<string> canGoTo = new List<string>();
        }

        [Tooltip("State đầu tiên sau Boot khi chạy bản build hoặc bấm Play từ scene Boot.")]
        [SerializeField] string initialState;
        [SerializeField] List<State> states = new List<State>();
        [Tooltip("State dùng khi phải tự Pause (mở overlay Steam…). Để trống nếu game không có Pause.")]
        [SerializeField] string pauseState = "Pause";

        public string InitialState => initialState;
        public string PauseState => pauseState;
        public IReadOnlyList<State> States => states;

        public State Find(string id)
        {
            foreach (var state in states)
            {
                if (state.id == id)
                    return state;
            }
            return null;
        }

        public SceneGroupAsset GroupOf(string id) => Find(id)?.sceneGroup;

        public FlowDefinition ToDefinition()
        {
            var definition = new FlowDefinition();
            foreach (var state in states)
                definition.AddState(state.id);
            foreach (var state in states)
            {
                foreach (string to in state.canGoTo)
                    definition.AddTransition(state.id, to);
            }

            if (!definition.HasState(initialState))
                throw new InvalidOperationException($"{name}: InitialState '{initialState}' không có trong danh sách state.");
            return definition;
        }

        /// <summary>
        /// Template dùng ngay cho game mới: Menu, Gameplay, Pause, Results.
        /// Boot và Loading của kế hoạch không cần là state: Boot là scene luôn mở, màn loading tự hiện mỗi lần đổi nhóm scene.
        /// </summary>
        public void ApplyTemplate(SceneGroupAsset menuGroup, SceneGroupAsset gameplayGroup)
        {
            initialState = "Menu";
            pauseState = "Pause";
            states = new List<State>
            {
                new State { id = "Menu", sceneGroup = menuGroup, canGoTo = new List<string> { "Gameplay" } },
                new State { id = "Gameplay", sceneGroup = gameplayGroup, canGoTo = new List<string> { "Pause", "Results", "Menu" } },
                new State { id = "Pause", canGoTo = new List<string> { "Gameplay", "Menu" } },
                new State { id = "Results", canGoTo = new List<string> { "Gameplay", "Menu" } },
            };
        }

        [ContextMenu("Áp template Menu/Gameplay/Pause/Results")]
        void ApplyTemplateFromMenu() => ApplyTemplate(GroupOf("Menu"), GroupOf("Gameplay"));
    }
}
