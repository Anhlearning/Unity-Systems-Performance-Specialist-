using Ignite.Core.Events;
using Ignite.Unity.Pooling;
using Ignite.Unity.Services;
using NUnit.Framework;
using VContainer;

namespace Ignite.Unity.Tests
{
    public class ServicesTests
    {
        struct Ping { }

        sealed class ScoreService
        {
            public readonly IEventPublisher Events;
            public ScoreService(IEventPublisher events) => Events = events;
        }

        /// <summary>Installer của game giả: đăng ký service phụ thuộc vào service framework.</summary>
        sealed class FakeGameInstaller : VContainer.Unity.IInstaller
        {
            public void Install(IContainerBuilder builder) => builder.Register<ScoreService>(Lifetime.Singleton);
        }

        static IObjectResolver BuildRoot()
        {
            var builder = new ContainerBuilder();
            FrameworkInstaller.RegisterCore(builder);
            new FakeGameInstaller().Install(builder);
            return builder.Build();
        }

        [Test]
        public void Root_ResolvesFrameworkServices()
        {
            using var root = BuildRoot();

            Assert.That(root.Resolve<IEventPublisher>(), Is.InstanceOf<EventBus>());
            Assert.That(root.Resolve<IEventSubscriber>(), Is.InstanceOf<ScopedEventSubscriber>());
            Assert.That(root.Resolve<PoolService>(), Is.Not.Null);
        }

        [Test]
        public void GameInstaller_ServiceGetsFrameworkDependency()
        {
            using var root = BuildRoot();

            var score = root.Resolve<ScoreService>();

            Assert.That(score.Events, Is.SameAs(root.Resolve<IEventPublisher>()));
        }

        [Test]
        public void EventBus_IsSharedBetweenRootAndChildScopes()
        {
            using var root = BuildRoot();
            using var child = root.CreateScope(_ => { });

            Assert.That(child.Resolve<EventBus>(), Is.SameAs(root.Resolve<EventBus>()));
        }

        [Test]
        public void ScopedServices_AreSeparatePerScope()
        {
            using var root = BuildRoot();
            using var a = root.CreateScope(_ => { });
            using var b = root.CreateScope(_ => { });

            Assert.That(a.Resolve<IEventSubscriber>(), Is.Not.SameAs(b.Resolve<IEventSubscriber>()));
            Assert.That(a.Resolve<PoolService>(), Is.Not.SameAs(b.Resolve<PoolService>()));
        }

        [Test]
        public void DisposingChildScope_UnsubscribesItsHandlers_RootHandlersStay()
        {
            using var root = BuildRoot();
            var bus = root.Resolve<IEventPublisher>();
            int rootCalls = 0, childCalls = 0;
            root.Resolve<IEventSubscriber>().Subscribe<Ping>(_ => rootCalls++);

            var child = root.CreateScope(_ => { });
            child.Resolve<IEventSubscriber>().Subscribe<Ping>(_ => childCalls++);
            bus.Publish(new Ping());

            child.Dispose();
            bus.Publish(new Ping());

            Assert.That(childCalls, Is.EqualTo(1));
            Assert.That(rootCalls, Is.EqualTo(2));
        }
    }
}
