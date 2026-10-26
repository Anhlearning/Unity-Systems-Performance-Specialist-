using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ignite.Core.Save
{
    /// <summary>Nơi lưu chuỗi theo key. Tách ra để test dùng bộ nhớ, bản thật dùng file, Steam Cloud dùng API của Steam.</summary>
    public interface ISaveStorage
    {
        bool Exists(string key);
        string Read(string key);
        void Write(string key, string content);
        bool Delete(string key);
        IReadOnlyList<string> Keys();
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public bool Exists(string key) => _files.ContainsKey(key);
        public string Read(string key) => _files.TryGetValue(key, out var content) ? content : null;
        public void Write(string key, string content) => _files[key] = content;
        public bool Delete(string key) => _files.Remove(key);
        public IReadOnlyList<string> Keys() => new List<string>(_files.Keys);
    }

    /// <summary>
    /// Lưu ra file .json trong một thư mục. Ghi atomic: ghi ra file .tmp rồi thay file cũ trong một bước,
    /// nên tắt game giữa chừng chỉ làm mất lần ghi đó, không bao giờ làm hỏng file đang có.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        static readonly Regex ValidKey = new Regex("^[A-Za-z0-9_\\-]+$");
        const string Extension = ".json";

        readonly string _directory;

        public FileSaveStorage(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(_directory);
        }

        public string Folder => _directory;

        public bool Exists(string key) => File.Exists(PathOf(key));

        public string Read(string key)
        {
            string path = PathOf(key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void Write(string key, string content)
        {
            string path = PathOf(key);
            string temp = path + ".tmp";

            File.WriteAllText(temp, content);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temp, path, null);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                    File.Delete(path);
                }
            }
            File.Move(temp, path);
        }

        public bool Delete(string key)
        {
            string path = PathOf(key);
            if (!File.Exists(path))
                return false;
            File.Delete(path);
            return true;
        }

        public IReadOnlyList<string> Keys()
        {
            var keys = new List<string>();
            foreach (string file in Directory.GetFiles(_directory, "*" + Extension))
                keys.Add(Path.GetFileNameWithoutExtension(file));
            return keys;
        }

        string PathOf(string key)
        {
            if (string.IsNullOrEmpty(key) || !ValidKey.IsMatch(key))
                throw new ArgumentException($"Key '{key}' chỉ được chứa chữ, số, '_' và '-'.", nameof(key));
            return Path.Combine(_directory, key + Extension);
        }
    }
}
