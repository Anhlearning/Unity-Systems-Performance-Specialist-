using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Scellecs.Morpeh;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Ignite.Ecs
{
    /// <summary>
    /// Sở hữu World Morpeh của nhóm scene gameplay: tạo khi scope của nhóm được build, huỷ khi rời nhóm.
    /// Mỗi IGameModule có một SystemsGroup, chạy theo thứ tự module được liệt kê.
    /// Tick → Update; LateTick → LateUpdate rồi CleanupUpdate; FixedTick → FixedUpdate.
    /// </summary>
    public sealed class EcsWorldHost : ITickable, ILateTickable, IFixedTickable, IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Ecs");

        readonly List<string> _systemOrder = new List<string>();
        readonly List<IDisposable> _saveRegistrations = new List<IDisposable>();
        readonly EntityViewRegistry _views;

        public EcsWorldHost(IReadOnlyList<IGameModule> modules, IObjectResolver resolver)
        {
            resolver.TryResolve(out _views);

            // Tạo ngay trong constructor (lúc scope build), để presenter được inject ngay sau đó đã thấy World.
            World = World.Create();
            World.UpdateByUnity = false; // framework tự gọi update, Morpeh không được gọi thêm lần nữa
            _views?.Attach(World);

            for (int i = 0; i < modules.Count; i++)
            {
                var group = World.CreateSystemsGroup();
                modules[i].AddSystems(new SystemsBuilder(World, group, resolver, _systemOrder));
                World.AddSystemsGroup(i, group);
            }
            World.Commit();

            // Module có tiến trình cần lưu: đăng ký với SaveService (nếu game có), nhận lại dữ liệu đã load ngay lúc này.
            if (resolver.TryResolve(out SaveService save))
            {
                foreach (var module in modules)
                {
                    if (module is ISaveableModule saveable)
                        _saveRegistrations.Add(save.Register(new ModuleSaveParticipant(saveable, World)));
                }
                World.Commit();
            }

            GameLog.Info(Log, $"Tạo World với {modules.Count} module: {string.Join(" → ", _systemOrder)}");
        }

        public World World { get; private set; }
        public bool IsAlive => World != null && !World.IsDisposed;

        /// <summary>Tên system theo đúng thứ tự chạy, để test và debug kiểm tra.</summary>
        public IReadOnlyList<string> SystemOrder => _systemOrder;

        public void Tick() => Step(Time.deltaTime);
        public void FixedTick() { if (IsAlive) World.FixedUpdate(Time.fixedDeltaTime); }

        public void LateTick()
        {
            if (!IsAlive)
                return;
            World.LateUpdate(Time.deltaTime);
            World.CleanupUpdate(Time.deltaTime);
        }

        /// <summary>Chạy một frame logic với dt cho trước. Test dùng hàm này để không phụ thuộc Time.deltaTime.</summary>
        public void Step(float deltaTime)
        {
            if (IsAlive)
                World.Update(deltaTime);
        }

        public void Dispose()
        {
            if (!IsAlive)
                return;

            // Chụp dữ liệu module lần cuối trong lúc World còn sống.
            foreach (var registration in _saveRegistrations)
                registration.Dispose();
            _saveRegistrations.Clear();

            _views?.UnlinkAll();
            World.Dispose();
            World = null;
            GameLog.Info(Log, "Huỷ World");
        }
    }
}
