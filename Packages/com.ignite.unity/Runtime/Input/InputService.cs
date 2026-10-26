using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Input
{
    /// <summary>
    /// Input lúc chạy. Mỗi action map là một layer: <c>using var layer = input.PushLayer("Player");</c>
    /// Ai bật layer thì giữ handle và tự Dispose để tắt; nhiều bên cùng bật thì map chỉ tắt khi bên cuối cùng tắt.
    /// Làm việc trên bản sao của InputActionAsset nên override phím không bao giờ ghi vào asset gốc.
    /// </summary>
    public sealed class InputService : IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Input");

        readonly Dictionary<InputActionMap, int> _layers = new Dictionary<InputActionMap, int>();
        readonly InputReader _reader;

        public InputService(InputReader reader, bool trackDevices = true)
        {
            _reader = reader;
            if (reader != null && reader.Actions != null)
                Actions = Object.Instantiate(reader.Actions);
            else
                GameLog.Warning(Log, "Chưa có InputReader/InputActionAsset: InputService chạy chế độ rỗng.");

            Devices = new DeviceTracker(trackDevices);
        }

        public InputActionAsset Actions { get; }
        public bool HasActions => Actions != null;
        public DeviceTracker Devices { get; }
        public InputReader Reader => _reader;

        public IDisposable PushLayer(string mapName)
        {
            if (!HasActions)
                return EmptyHandle.Instance;

            var map = Actions.FindActionMap(mapName, throwIfNotFound: true);
            _layers.TryGetValue(map, out int count);
            _layers[map] = count + 1;
            if (count == 0)
                map.Enable();
            return new LayerHandle(this, map);
        }

        public bool IsLayerEnabled(string mapName)
        {
            var map = Actions?.FindActionMap(mapName);
            return map != null && map.enabled;
        }

        public int LayerCount(string mapName)
        {
            var map = Actions?.FindActionMap(mapName);
            return map != null && _layers.TryGetValue(map, out int count) ? count : 0;
        }

        /// <summary>"Map/Action", ví dụ "UI/Cancel".</summary>
        public InputAction Find(string actionPath) => Actions?.FindAction(actionPath, throwIfNotFound: false);

        public string SaveOverrides() => HasActions ? Actions.SaveBindingOverridesAsJson() : string.Empty;

        public void LoadOverrides(string json)
        {
            if (!HasActions)
                return;
            Actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
                Actions.LoadBindingOverridesFromJson(json);
        }

        /// <summary>Chữ gợi ý phím theo thiết bị đang dùng, ví dụ "E" hay "Button South".</summary>
        public string DisplayFor(string actionPath, InputScheme? scheme = null)
        {
            var action = Find(actionPath);
            if (action == null || _reader == null)
                return string.Empty;
            string group = _reader.SchemeName(scheme ?? Devices.Current);
            return action.GetBindingDisplayString(InputBinding.MaskByGroup(group));
        }

        /// <summary>Icon gợi ý phím theo thiết bị đang dùng; null nếu bộ icon chưa có control đó.</summary>
        public Sprite IconFor(string actionPath, InputScheme? scheme = null)
        {
            var action = Find(actionPath);
            if (action == null || _reader == null)
                return null;

            var current = scheme ?? Devices.Current;
            string group = _reader.SchemeName(current);
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite || binding.groups == null || !binding.groups.Contains(group))
                    continue;
                foreach (var set in _reader.PromptIcons)
                {
                    if (set != null && set.Scheme == current)
                        return set.Find(binding.effectivePath);
                }
            }
            return null;
        }

        public void Dispose()
        {
            Devices.Dispose();
            if (HasActions)
            {
                Actions.Disable();
                if (Application.isPlaying)
                    Object.Destroy(Actions);
                else
                    Object.DestroyImmediate(Actions);
            }
            _layers.Clear();
        }

        void Release(InputActionMap map)
        {
            if (!_layers.TryGetValue(map, out int count))
                return;
            if (count <= 1)
            {
                _layers.Remove(map);
                map.Disable();
            }
            else
            {
                _layers[map] = count - 1;
            }
        }

        sealed class LayerHandle : IDisposable
        {
            InputService _service;
            readonly InputActionMap _map;

            public LayerHandle(InputService service, InputActionMap map)
            {
                _service = service;
                _map = map;
            }

            public void Dispose()
            {
                _service?.Release(_map);
                _service = null;
            }
        }

        sealed class EmptyHandle : IDisposable
        {
            public static readonly EmptyHandle Instance = new EmptyHandle();
            public void Dispose() { }
        }
    }
}
