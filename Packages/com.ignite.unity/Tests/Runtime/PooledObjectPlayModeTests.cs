using System.Collections;
using Ignite.Unity.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ignite.Unity.Tests
{
    // OnDisable chỉ chạy trong Play mode, nên phần "tự trả về pool khi bị tắt" phải test ở đây.
    public class PooledObjectPlayModeTests
    {
        [UnityTest]
        public IEnumerator SetActiveFalse_ReturnsObjectToPool()
        {
            var prefab = new GameObject("Prefab");
            var pools = new PoolService();
            try
            {
                var go = pools.Spawn(prefab, Vector3.zero, Quaternion.identity);
                yield return null;

                go.SetActive(false);
                yield return null;

                Assert.That(pools.GetPool(prefab).CountInactive, Is.EqualTo(1));
                Assert.That(pools.Spawn(prefab, Vector3.zero, Quaternion.identity), Is.SameAs(go));
            }
            finally
            {
                pools.Dispose();
                Object.Destroy(prefab);
            }
        }

        [UnityTest]
        public IEnumerator DisposingPool_DoesNotReturnObjectsWhileDestroying()
        {
            var prefab = new GameObject("Prefab");
            var pools = new PoolService();
            var pool = pools.GetPool(prefab);
            pools.Spawn(prefab, Vector3.zero, Quaternion.identity);

            pools.Dispose();
            yield return null;

            Assert.That(pool.IsDisposed, Is.True);
            Assert.That(pool.CountInactive, Is.EqualTo(0));
            Object.Destroy(prefab);
        }
    }
}
