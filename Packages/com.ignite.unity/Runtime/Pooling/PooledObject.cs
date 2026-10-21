using Ignite.Core.Pooling;
using UnityEngine;

namespace Ignite.Unity.Pooling
{
    /// <summary>
    /// Gắn tự động vào object do pool tạo. Object bị tắt (SetActive(false)) thì tự trả về pool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        internal GameObjectPool Owner;
        internal bool InPool;
        internal IPoolable[] Poolables;

        void OnDisable()
        {
            Owner?.ReturnFromDisable(this);
        }

        void OnDestroy()
        {
            Owner?.Forget(this);
        }
    }
}
