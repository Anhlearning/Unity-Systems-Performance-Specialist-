using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Assets
{
    /// <summary>Một lần load đang chạy hoặc đã xong. Backend giữ handle thật (AsyncOperationHandle của Addressables).</summary>
    public sealed class AssetRequest
    {
        public AssetRequest(UniTask<Object> task, object handle)
        {
            Task = task.Preserve(); // Preserve: cho phép nhiều bên cùng await một lần load
            Handle = handle;
        }

        public UniTask<Object> Task { get; }
        public object Handle { get; }
    }

    public interface IAssetBackend
    {
        AssetRequest Load<T>(string key) where T : Object;
        void Release(AssetRequest request);
    }

    public sealed class AddressablesBackend : IAssetBackend
    {
        public AssetRequest Load<T>(string key) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            return new AssetRequest(Await(handle), handle);
        }

        public void Release(AssetRequest request)
        {
            if (request.Handle is AsyncOperationHandle handle && handle.IsValid())
                Addressables.Release(handle);
        }

        static async UniTask<Object> Await<T>(AsyncOperationHandle<T> handle) where T : Object
        {
            var asset = await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw handle.OperationException ?? new InvalidOperationException("Addressables load thất bại.");
            return asset;
        }
    }

    /// <summary>
    /// Load/giải phóng asset theo key, có đếm tham chiếu: hai bên cùng Load một key thì chỉ load một lần,
    /// asset chỉ bị giải phóng khi bên cuối cùng Release. Thay cho Resources.Load.
    /// </summary>
    public sealed class AssetService : IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Assets");

        sealed class Entry
        {
            public AssetRequest Request;
            public int Count;
        }

        readonly IAssetBackend _backend;
        readonly Dictionary<(string key, Type type), Entry> _entries = new Dictionary<(string, Type), Entry>();

        public AssetService(IAssetBackend backend = null)
        {
            _backend = backend ?? new AddressablesBackend();
        }

        public int RefCount<T>(string key) where T : Object
            => _entries.TryGetValue((key, typeof(T)), out var entry) ? entry.Count : 0;

        public int LoadedCount => _entries.Count;

        public async UniTask<T> LoadAsync<T>(string key) where T : Object
        {
            var id = (key, typeof(T));
            if (!_entries.TryGetValue(id, out var entry))
            {
                entry = new Entry { Request = _backend.Load<T>(key) };
                _entries.Add(id, entry);
            }
            entry.Count++;

            try
            {
                return (T)await entry.Request.Task;
            }
            catch
            {
                Release<T>(key);
                throw;
            }
        }

        public void Release<T>(string key) where T : Object
        {
            var id = (key, typeof(T));
            if (!_entries.TryGetValue(id, out var entry))
            {
                GameLog.Warning(Log, $"Release '{key}' ({typeof(T).Name}) nhưng asset này chưa được load.");
                return;
            }

            entry.Count--;
            if (entry.Count > 0)
                return;

            _entries.Remove(id);
            _backend.Release(entry.Request);
        }

        public void Dispose()
        {
            foreach (var pair in _entries)
            {
                GameLog.Warning(Log, $"Asset '{pair.Key.key}' còn {pair.Value.Count} tham chiếu lúc tắt, giải phóng luôn.");
                _backend.Release(pair.Value.Request);
            }
            _entries.Clear();
        }
    }
}
