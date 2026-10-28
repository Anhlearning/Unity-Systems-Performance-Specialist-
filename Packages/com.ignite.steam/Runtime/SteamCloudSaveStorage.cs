#if IGNITE_STEAMWORKS && (UNITY_EDITOR || UNITY_STANDALONE)
#define IGNITE_STEAM_ACTIVE
#endif
using System.Collections.Generic;
using System.Text;
using Ignite.Core.Logging;
using Ignite.Core.Save;
#if IGNITE_STEAM_ACTIVE
using Steamworks;
#endif

namespace Ignite.Steam
{
    /// <summary>
    /// Lưu song song: file local (luôn có) và Steam Remote Storage (khi Steam chạy và người chơi bật Cloud).
    /// Đọc thì ưu tiên bản trên Cloud để chơi tiếp được trên máy khác; không có Steam thì dùng hoàn toàn file local.
    /// Cách khác không cần code: bật Steam Auto-Cloud trên trang Steamworks, trỏ vào thư mục Saves (xem README).
    /// </summary>
    public sealed class SteamCloudSaveStorage : ISaveStorage
    {
        const string Extension = ".json";
        readonly ISaveStorage _local;

        public SteamCloudSaveStorage(ISaveStorage local)
        {
            _local = local;
        }

        public bool CloudReady
        {
            get
            {
#if IGNITE_STEAM_ACTIVE
                return SteamPlatform.IsInitialized
                    && SteamRemoteStorage.IsCloudEnabledForAccount()
                    && SteamRemoteStorage.IsCloudEnabledForApp();
#else
                return false;
#endif
            }
        }

        public bool Exists(string key)
        {
#if IGNITE_STEAM_ACTIVE
            if (CloudReady && SteamRemoteStorage.FileExists(key + Extension))
                return true;
#endif
            return _local.Exists(key);
        }

        public string Read(string key)
        {
#if IGNITE_STEAM_ACTIVE
            if (CloudReady && SteamRemoteStorage.FileExists(key + Extension))
            {
                int size = SteamRemoteStorage.GetFileSize(key + Extension);
                var buffer = new byte[size];
                int read = SteamRemoteStorage.FileRead(key + Extension, buffer, size);
                if (read == size)
                    return Encoding.UTF8.GetString(buffer);
                GameLog.Warning(SteamPlatform.Log, $"Đọc '{key}' từ Steam Cloud không đủ ({read}/{size} byte), dùng bản local.");
            }
#endif
            return _local.Read(key);
        }

        public void Write(string key, string content)
        {
            _local.Write(key, content); // bản local là bản dự phòng, ghi atomic
#if IGNITE_STEAM_ACTIVE
            if (CloudReady)
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                if (!SteamRemoteStorage.FileWrite(key + Extension, bytes, bytes.Length))
                    GameLog.Warning(SteamPlatform.Log, $"Ghi '{key}' lên Steam Cloud thất bại, vẫn còn bản local.");
            }
#endif
        }

        public bool Delete(string key)
        {
            bool deleted = _local.Delete(key);
#if IGNITE_STEAM_ACTIVE
            if (CloudReady && SteamRemoteStorage.FileExists(key + Extension))
                deleted |= SteamRemoteStorage.FileDelete(key + Extension);
#endif
            return deleted;
        }

        public IReadOnlyList<string> Keys()
        {
            var keys = new List<string>(_local.Keys());
#if IGNITE_STEAM_ACTIVE
            if (CloudReady)
            {
                int count = SteamRemoteStorage.GetFileCount();
                for (int i = 0; i < count; i++)
                {
                    string name = SteamRemoteStorage.GetFileNameAndSize(i, out _);
                    if (name.EndsWith(Extension))
                    {
                        string key = name.Substring(0, name.Length - Extension.Length);
                        if (!keys.Contains(key))
                            keys.Add(key);
                    }
                }
            }
#endif
            return keys;
        }
    }
}
