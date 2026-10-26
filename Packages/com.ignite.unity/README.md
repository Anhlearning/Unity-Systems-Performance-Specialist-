# com.ignite.unity

Tầng Unity: Boot, Services (VContainer), Game Flow + Scene Group, Pool GameObject, màn loading,
Input, UI Stack, Assets (Addressables), Audio, Settings, Save, Localization, Debug, nền tảng (Null mặc định).

Game phải tự cài trong `Packages/manifest.json` (package git không tự kéo dependency git):

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
"jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.17.0"
```

## Dựng một game mới

1. Tạo scene `Boot.unity`, đặt ở **build index 0**. Thêm GameObject có `RootLifetimeScope`.
2. Viết `GameInstaller : InstallerAsset`, liệt kê service của game theo thứ tự. Tạo asset rồi kéo vào `RootLifetimeScope`.
3. Tạo `Scene Group` (Create → Ignite → Scene Group) cho từng màn: danh sách scene kèm vai trò (Active, UI, HUD…).
4. Tạo `Game Flow` (Create → Ignite → Game Flow), khai báo state, nhóm scene của state, state được chuyển tới. Chuột phải asset → *Áp template Menu/Gameplay/Pause/Results* để có sẵn 4 state.
5. Kéo Game Flow vào `RootLifetimeScope`. Bấm Play ở scene nào cũng được: Boot chạy trước, rồi vào state chứa scene đó.

Mẫu đầy đủ: `Assets/IgniteSample` (dựng lại bằng **Tools/Ignite/Rebuild Sample**).

## Thứ tự khi chuyển nhóm scene A → B

1. Hiện màn loading (canvas + camera riêng trong Boot).
2. So A với B: scene chung **giữ nguyên**, chỉ có ở A thì unload, chỉ có ở B thì load.
3. Huỷ scope con của A → unload → `Resources.UnloadUnusedAssets` → load additive song song.
4. Đặt Active scene theo vai trò → build scope con của B (installer của nhóm, nếu có) → inject mọi scene của B.
5. Ẩn loading khi thanh tiến trình chạy tới 100%.

Log kênh `Scene` in số scene giữ/unload/load và thời gian mỗi lần chuyển.

## Quy ước cho MonoBehaviour trong scene

Nhận dependency qua method `[Inject]` và khởi tạo trong đó, **không dựa vào `Start`**: scope của nhóm chỉ được build sau khi cả nhóm load xong. Scene được giữ lại giữa hai nhóm sẽ được inject lại từ scope mới (đăng ký event cũ đã tự huỷ cùng scope cũ).

## Service framework đăng ký sẵn

| Service | Lifetime | Ghi chú |
|---|---|---|
| `EventBus` / `IEventPublisher` | Singleton | |
| `IEventSubscriber` | Scoped | Mỗi nhóm scene một bản, tự huỷ đăng ký khi rời nhóm |
| `PoolService` | Scoped | Pool thuộc nhóm scene, rời nhóm thì object bị huỷ |
| `SceneGroupLoader` | Singleton | |
| `FlowController` | Singleton | `GoAsync("Gameplay")`, `CanGo`, `Current` |
| `ILoadingScreen` | Instance | Mặc định `DefaultLoadingScreen`; thay bằng MonoBehaviour của game |

Xem `Runtime/Services/FrameworkInstaller.cs`.

## Pool

```csharp
var bullet = pools.Spawn(bulletPrefab, pos, rot);
pools.Despawn(bullet);      // hoặc bullet.SetActive(false): tự trả về pool
```

Component trên prefab cài `IPoolable` nhận `OnSpawn/OnDespawn`. Sau `Prewarm`, Spawn/Despawn không sinh rác (có test 1000 lần).

## Người chơi (Giai đoạn 3)

| Module | Dùng | Ghi chú |
|---|---|---|
| Input | `InputService.PushLayer("Player")` → Dispose để tắt | Mỗi action map là một layer; nhiều bên cùng bật thì map tắt khi bên cuối tắt. Làm trên bản sao asset |
| Đổi phím | `InputRebinder.Start("Player/Jump", index)` | Lưu vào Settings, khởi động lại vẫn còn |
| Icon gợi ý | `InputService.DisplayFor/IconFor(action)` | Theo `DeviceTracker.Current` (bàn phím, gamepad, Steam Deck) |
| UI Stack | `UIStackService.Push(screen)`, `Back()` | Màn `UIScreen`: chọn sẵn `firstSelected`, nhớ focus khi quay lại. Back = `UI/Cancel`; ở màn gốc phát `BackAtRoot` |
| Assets | `await assets.LoadAsync<T>(key)` / `Release<T>(key)` | Addressables, đếm tham chiếu; thay cho `Resources.Load` |
| Audio | `audio.Play("sfx.hit")`, `PlayMusic`, `SetBusVolume` | `AudioLibrary` theo id, pool AudioSource, 4 bus; `AudioMixer` tuỳ chọn |
| Settings | `settings.Change(s => s.MasterVolume = 0.5f)` | File riêng, áp dụng âm lượng / phím / màn hình / ngôn ngữ |
| Save | `ISaveParticipant` + `save.Register(this)`, `Save/Load(slot)` | JSON có version, migration, ghi atomic; module ECS lưu qua `ISaveableModule` |
| Localization | `localization.Get("menu.play")`, `LocalizedText` | Bọc Unity Localization (tuỳ chọn); chỉ chạy khi Play, không chặn main thread |
| Debug | Phím `` ` `` mở console, F3 overlay FPS/GC | Chỉ Editor và Development Build. Thêm lệnh: `DebugCommands.Register` |
| Nền tảng | `IAchievements`, `IStats`, `IPlatformOverlay`, `IPlatformInfo` | Mặc định `NullPlatform`; Steam qua `com.ignite.steam`. Mở overlay → Pause |

GameInstaller override thêm `ConfigureSave(options)` (version, migration, nơi lưu) và `ConfigurePlatform(builder)`.

## Menu

- **Tools/Ignite/Play From Boot**: bật/tắt việc Play luôn đi qua Boot.
- **Tools/GameLog**: bật/tắt kênh log.
- **Tools/Ignite/Rebuild Sample**, **Tools/Ignite/Rebuild Sample Localization**: dựng lại sample.
