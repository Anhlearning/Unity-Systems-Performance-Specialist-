using System;
using System.Collections.Generic;
using Ignite.Core.Flow;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class GameFlowTests
    {
        sealed class RecordingHandler : IFlowStateHandler
        {
            readonly string _name;
            readonly List<string> _log;

            public RecordingHandler(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Enter() => _log.Add($"enter {_name}");
            public void Exit() => _log.Add($"exit {_name}");
        }

        static FlowDefinition Template()
        {
            return new FlowDefinition()
                .AddState("Menu").AddState("Gameplay").AddState("Pause").AddState("Results")
                .AddTransition("Menu", "Gameplay")
                .AddTransition("Gameplay", "Pause").AddTransition("Gameplay", "Results").AddTransition("Gameplay", "Menu")
                .AddTransition("Pause", "Gameplay").AddTransition("Pause", "Menu")
                .AddTransition("Results", "Menu");
        }

        [Test]
        public void ValidPath_MenuGameplayPauseMenu()
        {
            var flow = new GameFlow(Template());

            flow.Go("Menu");
            flow.Go("Gameplay");
            flow.Go("Pause");
            flow.Go("Menu");

            Assert.That(flow.Current, Is.EqualTo("Menu"));
        }

        [Test]
        public void InvalidTransition_Throws_AndKeepsState()
        {
            var flow = new GameFlow(Template());
            flow.Go("Menu");

            Assert.That(flow.CanGo("Pause"), Is.False);
            Assert.Throws<InvalidOperationException>(() => flow.Go("Pause"));
            Assert.That(flow.Current, Is.EqualTo("Menu"));
            Assert.That(flow.IsTransitioning, Is.False);
        }

        [Test]
        public void UnknownState_CannotBeEntered()
        {
            var flow = new GameFlow(Template());
            Assert.That(flow.CanGo("Shop"), Is.False);
        }

        [Test]
        public void FirstState_CanBeAnyDeclaredState()
        {
            // Editor: bấm Play từ scene Gameplay thì vào thẳng Gameplay.
            var flow = new GameFlow(Template());
            flow.Go("Gameplay");
            Assert.That(flow.Current, Is.EqualTo("Gameplay"));
        }

        [Test]
        public void GameDeclaredFlow_WithCustomShopState_Works()
        {
            var roguelike = new FlowDefinition()
                .AddState("Map").AddState("Battle").AddState("Shop")
                .AddTransition("Map", "Battle").AddTransition("Map", "Shop")
                .AddTransition("Battle", "Map").AddTransition("Shop", "Map");
            var flow = new GameFlow(roguelike);

            flow.Go("Map");
            flow.Go("Shop");
            flow.Go("Map");
            flow.Go("Battle");

            Assert.That(flow.Current, Is.EqualTo("Battle"));
            Assert.Throws<InvalidOperationException>(() => flow.Go("Shop"));
        }

        [Test]
        public void Handlers_ExitOldBeforeLoad_EnterNewAfterComplete()
        {
            var log = new List<string>();
            var flow = new GameFlow(Template());
            flow.SetHandler("Menu", new RecordingHandler("Menu", log));
            flow.SetHandler("Gameplay", new RecordingHandler("Gameplay", log));
            flow.Go("Menu");
            log.Clear();

            flow.BeginTransition("Gameplay");
            log.Add("load scenes");
            flow.CompleteTransition();

            Assert.That(log, Is.EqualTo(new[] { "exit Menu", "load scenes", "enter Gameplay" }));
        }

        [Test]
        public void WhileTransitioning_OtherTransitionsAreRejected()
        {
            var flow = new GameFlow(Template());
            flow.Go("Menu");

            flow.BeginTransition("Gameplay");

            Assert.That(flow.CanGo("Gameplay"), Is.False);
            Assert.Throws<InvalidOperationException>(() => flow.BeginTransition("Gameplay"));
        }

        [Test]
        public void Abort_StaysInOldState_AndReEntersIt()
        {
            var log = new List<string>();
            var flow = new GameFlow(Template());
            flow.SetHandler("Menu", new RecordingHandler("Menu", log));
            flow.Go("Menu");
            log.Clear();

            flow.BeginTransition("Gameplay");
            flow.AbortTransition();

            Assert.That(flow.Current, Is.EqualTo("Menu"));
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(log, Is.EqualTo(new[] { "exit Menu", "enter Menu" }));
        }

        [Test]
        public void StateChanged_ReportsFromAndTo()
        {
            var flow = new GameFlow(Template());
            var changes = new List<(string, string)>();
            flow.StateChanged += (from, to) => changes.Add((from, to));

            flow.Go("Menu");
            flow.Go("Gameplay");

            Assert.That(changes, Is.EqualTo(new[] { ((string)null, "Menu"), ("Menu", "Gameplay") }));
        }

        [Test]
        public void Definition_TransitionToUndeclaredState_Throws()
        {
            var definition = new FlowDefinition().AddState("Menu");
            Assert.Throws<ArgumentException>(() => definition.AddTransition("Menu", "Gameplay"));
        }

        [Test]
        public void Definition_DuplicateState_Throws()
        {
            var definition = new FlowDefinition().AddState("Menu");
            Assert.Throws<ArgumentException>(() => definition.AddState("Menu"));
        }
    }
}
