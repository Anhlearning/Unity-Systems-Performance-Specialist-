using System;
using Ignite.Core.Flow;
using Ignite.Unity.Flow;
using Ignite.Unity.Scenes;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Tests
{
    public class GameFlowAssetTests
    {
        GameFlowAsset _flow;
        SceneGroupAsset _menu;
        SceneGroupAsset _gameplay;

        [SetUp]
        public void SetUp()
        {
            _menu = ScriptableObject.CreateInstance<SceneGroupAsset>();
            _menu.EditorSetScenes(new[]
            {
                new SceneEntry { path = "Assets/Menu.unity", role = SceneRole.Active },
                new SceneEntry { path = "Assets/Shared.unity", role = SceneRole.UI },
            });
            _gameplay = ScriptableObject.CreateInstance<SceneGroupAsset>();
            _gameplay.EditorSetScenes(new[]
            {
                new SceneEntry { path = "Assets/Shared.unity", role = SceneRole.UI },
                new SceneEntry { path = "Assets/Gameplay.unity", role = SceneRole.Active },
            });
            _flow = ScriptableObject.CreateInstance<GameFlowAsset>();
            _flow.ApplyTemplate(_menu, _gameplay);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flow);
            Object.DestroyImmediate(_menu);
            Object.DestroyImmediate(_gameplay);
        }

        [Test]
        public void Template_BuildsValidDefinition()
        {
            var definition = _flow.ToDefinition();
            var flow = new GameFlow(definition);

            flow.Go(_flow.InitialState);
            flow.Go("Gameplay");
            flow.Go("Pause");
            flow.Go("Menu");

            Assert.That(_flow.InitialState, Is.EqualTo("Menu"));
            Assert.That(flow.Current, Is.EqualTo("Menu"));
        }

        [Test]
        public void PauseState_HasNoSceneGroup()
        {
            Assert.That(_flow.GroupOf("Pause"), Is.Null);
            Assert.That(_flow.GroupOf("Gameplay"), Is.SameAs(_gameplay));
        }

        [Test]
        public void SceneGroup_ActiveScene_FollowsRole_NotOrder()
        {
            Assert.That(_gameplay.ActiveScenePath, Is.EqualTo("Assets/Gameplay.unity"));
        }

        [Test]
        public void FlowController_FindsStateContainingScene()
        {
            var controller = new FlowController(_flow, null, null);

            Assert.That(controller.FindStateContaining("Assets/Gameplay.unity"), Is.EqualTo("Gameplay"));
            Assert.That(controller.FindStateContaining("Assets/Shared.unity"), Is.EqualTo("Menu"));
            Assert.That(controller.FindStateContaining("Assets/Other.unity"), Is.Null);
        }

        [Test]
        public void InitialStateMissing_Throws()
        {
            var broken = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => broken.ToDefinition());
            }
            finally
            {
                Object.DestroyImmediate(broken);
            }
        }
    }
}
