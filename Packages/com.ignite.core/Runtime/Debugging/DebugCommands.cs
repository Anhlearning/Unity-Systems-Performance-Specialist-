using System;
using System.Collections.Generic;
using System.Text;

namespace Ignite.Core.Debugging
{
    /// <summary>
    /// Bảng lệnh của console debug. Lệnh nhận mảng tham số, trả về chuỗi in ra console.
    /// Đăng ký trả về IDisposable để lệnh của một nhóm scene tự gỡ khi rời nhóm.
    /// </summary>
    public sealed class DebugCommands
    {
        readonly Dictionary<string, (string description, Func<string[], string> run)> _commands =
            new Dictionary<string, (string, Func<string[], string>)>(StringComparer.OrdinalIgnoreCase);

        public DebugCommands()
        {
            Register("help", "Liệt kê lệnh", _ => Help());
        }

        public IReadOnlyCollection<string> Names => _commands.Keys;

        public IDisposable Register(string name, string description, Func<string[], string> run)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains(" "))
                throw new ArgumentException("Tên lệnh không được rỗng hay có dấu cách.", nameof(name));
            _commands[name] = (description, run ?? throw new ArgumentNullException(nameof(run)));
            return new Unregister(this, name, run);
        }

        public string Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!_commands.TryGetValue(parts[0], out var command))
                return $"Không có lệnh '{parts[0]}'. Gõ help để xem danh sách.";

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            try
            {
                return command.run(args) ?? string.Empty;
            }
            catch (Exception e)
            {
                return $"Lệnh '{parts[0]}' lỗi: {e.Message}";
            }
        }

        string Help()
        {
            var names = new List<string>(_commands.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            var text = new StringBuilder();
            foreach (string name in names)
                text.Append(name).Append(" — ").AppendLine(_commands[name].description);
            return text.ToString().TrimEnd();
        }

        sealed class Unregister : IDisposable
        {
            DebugCommands _owner;
            readonly string _name;
            readonly Func<string[], string> _run;

            public Unregister(DebugCommands owner, string name, Func<string[], string> run)
            {
                _owner = owner;
                _name = name;
                _run = run;
            }

            public void Dispose()
            {
                // Chỉ gỡ nếu lệnh chưa bị đăng ký đè bởi bên khác.
                if (_owner != null && _owner._commands.TryGetValue(_name, out var current) && current.run == _run)
                    _owner._commands.Remove(_name);
                _owner = null;
            }
        }
    }
}
