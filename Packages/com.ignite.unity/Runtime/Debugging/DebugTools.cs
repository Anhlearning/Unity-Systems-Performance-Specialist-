using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Ignite.Core.Debugging;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Unity.Flow;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Debugging
{
    /// <summary>
    /// Console lệnh (phím ` để mở), cheat và overlay FPS/GC. Chỉ được cài trong Editor và Development Build.
    /// Lệnh có sẵn: help, flow, timescale, log, save, load, gc, overlay. Game thêm lệnh qua DebugCommands.Register.
    /// </summary>
    public sealed class DebugTools : IStartable, IDisposable
    {
        readonly DebugCommands _commands;
        readonly FlowController _flow;
        readonly SaveService _save;
        readonly List<IDisposable> _registrations = new List<IDisposable>();
        GameObject _root;

        public DebugTools(DebugCommands commands, FlowController flow, SaveService save)
        {
            _commands = commands;
            _flow = flow;
            _save = save;
        }

        public DebugConsoleGui Console { get; private set; }
        public StatsOverlay Overlay { get; private set; }

        public void Start()
        {
            _registrations.Add(_commands.Register("flow", "flow <state>: chuyển state", Flow));
            _registrations.Add(_commands.Register("timescale", "timescale <x>: đặt Time.timeScale", TimeScale));
            _registrations.Add(_commands.Register("log", "log <kênh> <on|off>: bật tắt kênh log", Log));
            _registrations.Add(_commands.Register("save", "save <slot>: lưu game", a => { _save.Save(Arg(a, 0, "debug")); return "Đã lưu."; }));
            _registrations.Add(_commands.Register("load", "load <slot>: tải game", a => _save.Load(Arg(a, 0, "debug")) ? "Đã tải." : "Không có slot đó."));
            _registrations.Add(_commands.Register("gc", "gc: chạy GC và báo bộ nhớ", _ => { GC.Collect(); return $"Heap: {GC.GetTotalMemory(false) / 1024} KB"; }));
            _registrations.Add(_commands.Register("overlay", "overlay: bật/tắt FPS/GC", _ => { Overlay.Visible = !Overlay.Visible; return Overlay.Visible ? "Overlay bật." : "Overlay tắt."; }));

            _root = new GameObject("[Debug]");
            Object.DontDestroyOnLoad(_root);
            Console = _root.AddComponent<DebugConsoleGui>();
            Console.Bind(_commands);
            Console.enabled = false;
            Overlay = _root.AddComponent<StatsOverlay>();
            Overlay.Visible = false; // IMGUI sinh rác mỗi frame: chỉ bật khi cần xem (F3)
            _root.AddComponent<DebugHotkeys>().Bind(Console, Overlay);
        }

        string Flow(string[] args)
        {
            if (args.Length == 0)
                return $"State hiện tại: {_flow.Current}";
            if (!_flow.CanGo(args[0]))
                return $"Không chuyển được từ {_flow.Current} sang {args[0]}.";
            _flow.GoAsync(args[0]).Forget();
            return $"→ {args[0]}";
        }

        static string TimeScale(string[] args)
        {
            if (args.Length == 0 || !float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                return $"timeScale = {Time.timeScale}";
            Time.timeScale = Mathf.Max(0f, scale);
            return $"timeScale = {Time.timeScale}";
        }

        static string Log(string[] args)
        {
            if (args.Length < 2)
                return "Cách dùng: log <kênh> <on|off>";
            for (int i = 0; i < LogChannels.Count; i++)
            {
                var channel = LogChannels.At(i);
                if (string.Equals(channel.Name, args[0], StringComparison.OrdinalIgnoreCase))
                {
                    GameLog.SetEnabled(channel, args[1] == "on");
                    return $"Kênh {channel.Name}: {args[1]}";
                }
            }
            return $"Không có kênh '{args[0]}'.";
        }

        static string Arg(string[] args, int index, string fallback) => args.Length > index ? args[index] : fallback;

        public void Dispose()
        {
            foreach (var registration in _registrations)
                registration.Dispose();
            _registrations.Clear();
            if (_root != null)
                Object.Destroy(_root);
        }
    }

}
