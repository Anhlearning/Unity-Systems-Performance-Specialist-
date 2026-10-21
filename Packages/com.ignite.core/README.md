# com.ignite.core

C# thuần, không tham chiếu UnityEngine (`noEngineReferences: true`). Thử `using UnityEngine` ở đây sẽ bị lỗi compile, đó là cố ý.

| Module | Namespace | Dùng để |
|---|---|---|
| Log | `Ignite.Core.Logging` | `GameLog.Info/Warning/Error(kênh, chuỗi)`. Bật tắt theo kênh. `Info` bị compiler xoá khỏi bản release (trừ khi define `IGNITE_LOG_INFO`). `Error` luôn in. |
| Event bus | `Ignite.Core.Events` | Event cấp app, kiểu struct, `Publish` không sinh rác. Đăng ký qua `IEventSubscriber` của scope thì tự huỷ khi scope bị huỷ. |
| Game Flow | `Ignite.Core.Flow` | State machine do game khai báo (`FlowDefinition`). Chuyển state hai pha: `BeginTransition` → (load scene) → `CompleteTransition`. |
| Scene diff | `Ignite.Core.Scenes` | `SceneGroupDiff.Compute`: so hai nhóm scene ra danh sách giữ / unload / load. |
| Pool | `Ignite.Core.Pooling` | `ObjectPool<T>` và `IPoolable` (OnSpawn/OnDespawn). |

## Khai báo kênh log của game

```csharp
public static class FightLog
{
    public static readonly LogChannel Fight = LogChannels.Register("Fight");
}

GameLog.Info(FightLog.Fight, "Bắt đầu trận");
```

Tối đa 64 kênh (framework dùng 6). Bật tắt trong **Tools/GameLog**.

## Event

```csharp
public readonly struct EnemyDied { public readonly int Id; public EnemyDied(int id) => Id = id; }

publisher.Publish(new EnemyDied(7));
subscriber.Subscribe<EnemyDied>(e => ...); // IEventSubscriber của scope: tự huỷ khi rời nhóm scene
```

Event gameplay trong ECS không đi qua bus này (xem com.ignite.ecs, Giai đoạn 2).

## Test

`Tests/Editor`: Log, Event bus, Game Flow, Scene diff, Pool, ranh giới phụ thuộc. Chạy trong Test Runner → EditMode.
