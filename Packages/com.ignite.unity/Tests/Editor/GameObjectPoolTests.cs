using Ignite.Core.Pooling;
using Ignite.Unity.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace Ignite.Unity.Tests
{
    public class GameObjectPoolTests
    {
        sealed class Counter : MonoBehaviour, IPoolable
        {
            public int Spawns;
            public int Despawns;
            public void OnSpawn() => Spawns++;
            public void OnDespawn() => Despawns++;
        }

        GameObject _prefab;
        PoolService _pools;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("BulletPrefab");
            _prefab.AddComponent<Counter>();
            _pools = new PoolService();
        }

        [TearDown]
        public void TearDown()
        {
            _pools.Dispose();
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void Despawn_ThenSpawn_ReusesInstance()
        {
            var a = _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            _pools.Despawn(a);
            var b = _pools.Spawn(_prefab, Vector3.one, Quaternion.identity);

            Assert.That(b, Is.SameAs(a));
            Assert.That(b.activeSelf, Is.True);
            Assert.That(b.transform.position, Is.EqualTo(Vector3.one));
            Assert.That(_pools.GetPool(_prefab).CountAll, Is.EqualTo(1));
        }

        [Test]
        public void IPoolable_OnPrefab_ReceivesCallbacks()
        {
            var go = _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            _pools.Despawn(go);
            _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);

            var counter = go.GetComponent<Counter>();
            Assert.That(counter.Spawns, Is.EqualTo(2));
            Assert.That(counter.Despawns, Is.EqualTo(1));
        }

        [Test]
        public void DoubleDespawn_IsIgnored()
        {
            var go = _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            _pools.Despawn(go);
            _pools.Despawn(go);

            Assert.That(_pools.GetPool(_prefab).CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_DestroysEveryPooledObject()
        {
            var go = _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);

            _pools.Dispose();

            Assert.That(go == null, Is.True);
        }

        [Test]
        public void SpawnDespawn_1000Times_AfterPrewarm_DoesNotAllocate()
        {
            var pool = _pools.GetPool(_prefab);
            pool.Prewarm(8);
            _pools.Despawn(_pools.Spawn(_prefab, Vector3.zero, Quaternion.identity)); // làm nóng JIT

            Assert.That(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var go = _pools.Spawn(_prefab, Vector3.zero, Quaternion.identity);
                    _pools.Despawn(go);
                }
            }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));

            Assert.That(pool.CountAll, Is.EqualTo(8));
        }
    }
}
