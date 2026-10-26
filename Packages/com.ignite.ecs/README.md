# com.ignite.ecs

Chạy gameplay bằng Morpeh (khoá tag 2024.1.1) theo cùng một cách cho mọi game. Game phải tự cài Morpeh trong manifest:

```json
"com.scellecs.morpeh": "https://github.com/scellecs/morpeh.git?path=Scellecs.Morpeh#2024.1.1"
```

## Cắm gameplay vào

```csharp
// Installer của nhóm scene Gameplay (kéo vào SceneGroupAsset.installer)
public sealed class GameplayInstaller : InstallerAsset
{
    public override void Install(IContainerBuilder builder)
        => EcsInstaller.Install(builder, new InputModule(), new FightModule(config)); // thứ tự = thứ tự chạy
}
```

- `EcsWorldHost` tạo World khi scope của nhóm được build, huỷ khi rời nhóm. Tick → `Update`, LateTick → `LateUpdate` + `CleanupUpdate`, FixedTick → `FixedUpdate`. `World.UpdateByUnity = false`.
- Mỗi `IGameModule` có một `SystemsGroup`. `AddSystems` thêm system theo đúng thứ tự viết ra, không tự quét.

```csharp
public sealed class FightModule : IGameModule
{
    public string Name => "Fight";
    public void Install(IContainerBuilder b) => b.Register<CommandQueue<FightInputCommand>>(Lifetime.Singleton);
    public void AddSystems(SystemsBuilder s) => s.Add(new FightCommandSystem(s.Resolve<CommandQueue<FightInputCommand>>()));
}
```

## Quy ước Bridge

| Chiều | Dùng | Ghi chú |
|---|---|---|
| Mono → ECS | `CommandQueue<T>.Enqueue` | Chỉ chở ý định, không ghi thẳng component |
| ECS → Mono | `EventQueue<T>.Publish` | Event tự đủ data; presenter đọc trong LateUpdate |
| State liên tục | Đọc component qua `Host.World` | Thanh máu, độ vướng dây… chỉ đọc |
| Entity ↔ View | `EntityViewRegistry.Link(entity, link)` | `EntityLink` bị Destroy thì tự gỡ |

Mỗi queue **chỉ 1 consumer** (`Claim(tên)`); consumer thứ hai bị báo lỗi ngay.

## Presenter

```csharp
public sealed class HitFlashPresenter : PresenterBase<DamagedEvent>
{
    protected override void OnEvent(in DamagedEvent e) { /* Views.TryGetView(e.Target, …) */ }
}
```

## Tách asmdef

Gameplay (component + system) để ở asmdef bật `noEngineReferences`, view ở asmdef `.View` riêng: phần sim không thể vô tình `using UnityEngine`. Xem `Assets/IgniteSample/Gameplay` và `Gameplay.View`.
