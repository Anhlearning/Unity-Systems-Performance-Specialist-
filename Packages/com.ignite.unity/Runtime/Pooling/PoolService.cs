using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Ignite.Core.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Pooling
{
    /// <summary>Pool cho một prefab. Sau khi Prewarm đủ, Spawn/Despawn không sinh rác.</summary>
    public sealed class GameObjectPool : IDisposable
    {
        readonly GameObject _prefab;
        readonly Transform _storage;
        readonly Stack<PooledObject> _free = new Stack<PooledObject>();
        readonly List<PooledObject> _all = new List<PooledObject>();

        public GameObjectPool(GameObject prefab, Transform storage)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _storage = storage;
        }

        public bool IsDisposed { get; private set; }
        public int CountAll => _all.Count;
        public int CountInactive => _free.Count;

        public void Prewarm(int count)
        {
            while (_all.Count < count)
                _free.Push(Create());
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(GameObjectPool));

            PooledObject item = null;
            while (_free.Count > 0 && item == null)
                item = _free.Pop(); // bỏ qua object đã bị Destroy từ bên ngoài
            if (item == null)
                item = Create();

            item.InPool = false;
            var t = item.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);

            foreach (var poolable in item.Poolables)
                poolable.OnSpawn();
            return item.gameObject;
        }

        public void Despawn(PooledObject item)
        {
            if (item.InPool)
                return;
            MarkReturned(item);
            item.gameObject.SetActive(false);
        }

        internal void ReturnFromDisable(PooledObject item)
        {
            // Đổi parent trong OnDisable bị Unity cấm, nên object được để nguyên chỗ, chỉ đánh dấu là rảnh.
            if (item.InPool || IsDisposed || !item.gameObject.scene.isLoaded)
                return;
            MarkReturned(item);
        }

        internal void Forget(PooledObject item)
        {
            if (!IsDisposed)
                _all.Remove(item);
        }

        void MarkReturned(PooledObject item)
        {
            item.InPool = true;
            foreach (var poolable in item.Poolables)
                poolable.OnDespawn();
            _free.Push(item);
        }

        PooledObject Create()
        {
            // storage là GameObject tắt, nên object mới tạo chưa chạy Awake/OnEnable cho tới lần Spawn đầu tiên.
            var go = Object.Instantiate(_prefab, _storage);
            go.SetActive(false);
            go.name = _prefab.name;

            var item = go.GetComponent<PooledObject>();
            if (item == null)
                item = go.AddComponent<PooledObject>();
            item.Owner = this;
            item.InPool = true;
            item.Poolables = go.GetComponentsInChildren<IPoolable>(true);
            _all.Add(item);
            return item;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            foreach (var item in _all)
            {
                if (item != null)
                    PoolService.DestroyObject(item.gameObject);
            }
            _all.Clear();
            _free.Clear();
        }
    }

    /// <summary>
    /// Pool thuộc scope: mỗi nhóm scene có một PoolService riêng, rời nhóm thì mọi object của pool bị huỷ.
    /// </summary>
    public sealed class PoolService : IDisposable
    {
        readonly Dictionary<GameObject, GameObjectPool> _pools = new Dictionary<GameObject, GameObjectPool>();
        Transform _storage;
        bool _disposed;

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => GetPool(prefab).Spawn(position, rotation, parent);

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
            => GetPool(prefab.gameObject).Spawn(position, rotation, parent).GetComponent<T>();

        public void Despawn(GameObject instance)
        {
            var item = instance.GetComponent<PooledObject>();
            if (item == null || item.Owner == null)
            {
                GameLog.Warning(LogChannels.Pool, $"'{instance.name}' không do pool tạo, Destroy thay vì Despawn.");
                DestroyObject(instance);
                return;
            }
            item.Owner.Despawn(item);
        }

        public void Prewarm(GameObject prefab, int count) => GetPool(prefab).Prewarm(count);

        public GameObjectPool GetPool(GameObject prefab)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolService));

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, Storage);
                _pools.Add(prefab, pool);
            }
            return pool;
        }

        Transform Storage
        {
            get
            {
                if (_storage == null)
                {
                    var go = new GameObject("[Pool]");
                    go.SetActive(false);
                    _storage = go.transform;
                }
                return _storage;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach (var pool in _pools.Values)
                pool.Dispose();
            _pools.Clear();
            if (_storage != null)
                DestroyObject(_storage.gameObject);
        }

        internal static void DestroyObject(GameObject go)
        {
            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }
    }
}
