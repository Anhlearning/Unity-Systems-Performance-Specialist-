using System.Collections.Generic;
using Ignite.Core.Pooling;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class ObjectPoolTests
    {
        sealed class Bullet : IPoolable
        {
            public int Spawns;
            public int Despawns;
            public void OnSpawn() => Spawns++;
            public void OnDespawn() => Despawns++;
        }

        [Test]
        public void Release_ThenGet_ReusesSameInstance()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());

            var a = pool.Get();
            pool.Release(a);
            var b = pool.Get();

            Assert.That(b, Is.SameAs(a));
            Assert.That(pool.CountAll, Is.EqualTo(1));
        }

        [Test]
        public void IPoolable_ReceivesSpawnAndDespawn()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());

            var bullet = pool.Get();
            pool.Release(bullet);
            pool.Get();

            Assert.That(bullet.Spawns, Is.EqualTo(2));
            Assert.That(bullet.Despawns, Is.EqualTo(1));
        }

        [Test]
        public void Release_BeyondMaxSize_DestroysExtra()
        {
            var destroyed = new List<Bullet>();
            var pool = new ObjectPool<Bullet>(() => new Bullet(), onDestroy: destroyed.Add, maxSize: 2);
            var items = new[] { pool.Get(), pool.Get(), pool.Get() };

            foreach (var item in items)
                pool.Release(item);

            Assert.That(pool.CountInactive, Is.EqualTo(2));
            Assert.That(destroyed, Is.EqualTo(new[] { items[2] }));
            Assert.That(pool.CountAll, Is.EqualTo(2));
        }

        [Test]
        public void Prewarm_CreatesUpFront()
        {
            int created = 0;
            var pool = new ObjectPool<Bullet>(() => { created++; return new Bullet(); });

            pool.Prewarm(10);
            pool.Get();

            Assert.That(created, Is.EqualTo(10));
            Assert.That(pool.CountActive, Is.EqualTo(1));
        }

        [Test]
        public void GetRelease_AfterPrewarm_DoesNotAllocate()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            pool.Prewarm(4);
            pool.Release(pool.Get()); // làm nóng JIT

            Assert.That(() =>
            {
                for (int i = 0; i < 1000; i++)
                    pool.Release(pool.Get());
            }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }
    }
}
