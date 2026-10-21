using System;
using System.Collections.Generic;
using Ignite.Core.Events;
using Ignite.Core.Logging;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class EventBusTests
    {
        struct Damaged
        {
            public int Amount;
        }

        struct Healed
        {
            public int Amount;
        }

        ILogSink _previousSink;

        [SetUp]
        public void SetUp()
        {
            // Exception của handler được log dạng Error; tắt sink để không làm bẩn Console khi test cố ý ném lỗi.
            _previousSink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
        }

        [TearDown]
        public void TearDown() => GameLog.Sink = _previousSink;

        [Test]
        public void Publish_ReachesOnlySubscribersOfThatType()
        {
            var bus = new EventBus();
            int damage = 0, heal = 0;
            bus.Subscribe<Damaged>(e => damage += e.Amount);
            bus.Subscribe<Healed>(e => heal += e.Amount);

            bus.Publish(new Damaged { Amount = 5 });

            Assert.That(damage, Is.EqualTo(5));
            Assert.That(heal, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_Subscription_StopsDelivery()
        {
            var bus = new EventBus();
            int calls = 0;
            var subscription = bus.Subscribe<Damaged>(_ => calls++);

            subscription.Dispose();
            bus.Publish(new Damaged());

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(bus.CountHandlers<Damaged>(), Is.EqualTo(0));
        }

        [Test]
        public void Unsubscribe_DuringPublish_IsSafe_AndOthersStillRun()
        {
            var bus = new EventBus();
            var order = new List<string>();
            IDisposable first = null;
            first = bus.Subscribe<Damaged>(_ => { order.Add("first"); first.Dispose(); });
            bus.Subscribe<Damaged>(_ => order.Add("second"));

            bus.Publish(new Damaged());
            bus.Publish(new Damaged());

            Assert.That(order, Is.EqualTo(new[] { "first", "second", "second" }));
        }

        [Test]
        public void Subscribe_DuringPublish_ReceivesFromNextPublish()
        {
            var bus = new EventBus();
            int late = 0;
            bus.Subscribe<Damaged>(_ => bus.Subscribe<Damaged>(__ => late++));

            bus.Publish(new Damaged());
            Assert.That(late, Is.EqualTo(0));

            bus.Publish(new Damaged());
            Assert.That(late, Is.EqualTo(1));
        }

        [Test]
        public void HandlerException_DoesNotStopOtherHandlers()
        {
            var bus = new EventBus();
            int reached = 0;
            bus.Subscribe<Damaged>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<Damaged>(_ => reached++);

            Assert.DoesNotThrow(() => bus.Publish(new Damaged()));
            Assert.That(reached, Is.EqualTo(1));
        }

        [Test]
        public void ScopedSubscriber_Dispose_RemovesAllItsSubscriptions_KeepsOthers()
        {
            var bus = new EventBus();
            var scope = new ScopedEventSubscriber(bus);
            int scoped = 0, global = 0;
            scope.Subscribe<Damaged>(_ => scoped++);
            scope.Subscribe<Healed>(_ => scoped++);
            bus.Subscribe<Damaged>(_ => global++);

            scope.Dispose();
            bus.Publish(new Damaged());
            bus.Publish(new Healed());

            Assert.That(scoped, Is.EqualTo(0));
            Assert.That(global, Is.EqualTo(1));
        }

        [Test]
        public void Publish_DoesNotAllocate()
        {
            var bus = new EventBus();
            int total = 0;
            bus.Subscribe<Damaged>(e => total += e.Amount);
            bus.Subscribe<Damaged>(e => total -= 1);
            bus.Publish(new Damaged { Amount = 1 }); // làm nóng JIT

            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                    bus.Publish(new Damaged { Amount = 2 });
            }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }
    }
}
