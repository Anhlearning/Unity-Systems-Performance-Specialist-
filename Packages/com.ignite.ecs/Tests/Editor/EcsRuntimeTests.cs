using System;
using System.Collections.Generic;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Ignite.Ecs.Tests
{
    public class EcsRuntimeTests
    {
        struct Counter : IComponent
        {
            public int Value;
        }

        readonly struct Tick { }

        /// <summary>System ghi tên mình vào log mỗi frame, để kiểm tra thứ tự chạy.</summary>
        sealed class RecordingSystem : ISystem
        {
            readonly string _name;
            readonly List<string> _log;

            public RecordingSystem(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public World World { get; set; }
            public void OnAwake() { }
            public void OnUpdate(float deltaTime) => _log.Add(_name);
            public void Dispose() { }
        }

        sealed class RecordingModule : IGameModule
        {
            readonly string _name;
            readonly List<string> _log;

            public RecordingModule(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public string Name => _name;
            public void Install(IContainerBuilder builder) { }

            public void AddSystems(SystemsBuilder systems)
            {
                systems.Add(new RecordingSystem(_name + ".A", _log)).Add(new RecordingSystem(_name + ".B", _log));
            }
        }

        static EcsWorldHost CreateHost(params IGameModule[] modules)
        {
            var builder = new ContainerBuilder();
            builder.Register<EntityViewRegistry>(Lifetime.Singleton);
            foreach (var module in modules)
                module.Install(builder);
            return new EcsWorldHost(modules, builder.Build());
        }

        [Test]
        public void Host_CreatesWorld_AndDisposeDestroysIt()
        {
            var host = CreateHost();
            var world = host.World;

            Assert.That(host.IsAlive, Is.True);
            Assert.That(world.UpdateByUnity, Is.False, "Framework tự update World, Morpeh không được update thêm.");

            host.Dispose();

            Assert.That(host.IsAlive, Is.False);
            Assert.That(world.IsDisposed, Is.True);
        }

        [Test]
        public void Systems_RunInExplicitModuleAndListOrder()
        {
            var log = new List<string>();
            var host = CreateHost(new RecordingModule("Input", log), new RecordingModule("Fight", log));
            try
            {
                host.Step(0.016f);

                Assert.That(log, Is.EqualTo(new[] { "Input.A", "Input.B", "Fight.A", "Fight.B" }));
                Assert.That(host.SystemOrder, Has.Count.EqualTo(4));
            }
            finally
            {
                host.Dispose();
            }
        }

        [Test]
        public void CommandQueue_SecondConsumer_Throws()
        {
            var queue = new CommandQueue<Tick>();
            queue.Claim("HitSystem");

            var error = Assert.Throws<InvalidOperationException>(() => queue.Claim("OtherSystem"));
            StringAssert.Contains("HitSystem", error.Message);
        }

        [Test]
        public void CommandQueue_SameConsumerAgain_IsAllowed()
        {
            // Presenter ở scene được giữ lại sẽ được inject lại với cùng tên.
            var queue = new EventQueue<Tick>();
            queue.Claim("HudPresenter");
            Assert.DoesNotThrow(() => queue.Claim("HudPresenter"));
        }

        [Test]
        public void Reader_DrainsQueue_ConsumerClearsIt()
        {
            var queue = new CommandQueue<int>();
            var reader = queue.Claim("Consumer");
            queue.Enqueue(1);
            queue.Enqueue(2);

            var read = new List<int>();
            while (reader.TryRead(out int value))
                read.Add(value);

            Assert.That(read, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnqueueAndRead_DoNotAllocate()
        {
            var queue = new CommandQueue<int>();
            var reader = queue.Claim("Consumer");
            queue.Enqueue(0);
            reader.TryRead(out _); // làm nóng JIT

            Assert.That(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    queue.Enqueue(i);
                    reader.TryRead(out _);
                }
            }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }

        [Test]
        public void Registry_LinksEntityAndView_BothWays_AndUnlinks()
        {
            var host = CreateHost();
            var go = new GameObject("View");
            try
            {
                var entity = host.World.CreateEntity();
                var link = go.AddComponent<EntityLink>();
                var registry = new EntityViewRegistry();

                registry.Link(entity, link);

                Assert.That(registry.TryGetView(entity, out var found), Is.True);
                Assert.That(found, Is.SameAs(link));
                Assert.That(link.Entity, Is.EqualTo(entity));
                Assert.That(link.IsLinked, Is.True);

                registry.Unlink(entity);

                Assert.That(registry.TryGetView(entity, out _), Is.False);
                Assert.That(link.IsLinked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
                host.Dispose();
            }
        }

        [Test]
        public void Components_AreReadableThroughWorld()
        {
            var host = CreateHost();
            try
            {
                var entity = host.World.CreateEntity();
                host.World.GetStash<Counter>().Set(entity, new Counter { Value = 3 });
                host.World.Commit();

                Assert.That(host.World.GetStash<Counter>().Get(entity).Value, Is.EqualTo(3));
            }
            finally
            {
                host.Dispose();
            }
        }
    }
}
