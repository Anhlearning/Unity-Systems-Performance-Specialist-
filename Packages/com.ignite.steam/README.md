# com.ignite.steam

Steamworks.NET cho framework Ignite: khởi động an toàn, achievement, stats, overlay → Pause, cloud save.
Không có Steam (chưa cài, chưa chạy, thiếu DLL) thì tự rơi về `NullPlatform`, game vẫn chạy.

Game phải tự cài Steamworks.NET trong `Packages/manifest.json` (khoá tag):

```json
"com.rlabrecque.steamworks.net": "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1"
```

## Bật Steam cho game

```csharp
public sealed class GameInstaller : InstallerAsset
{
    public override void ConfigurePlatform(IContainerBuilder builder) => SteamInstaller.Install(builder, appId: 480);

    public override void ConfigureSave(SaveOptions options)
    {
        options.WrapStorage = local => new SteamCloudSaveStorage(local); // tuỳ chọn, xem "Cloud save"
    }
}
```

- **App ID 480** là Spacewar, app thử của Valve. Đổi sang App ID thật trước khi lên Steam.
- Đặt `steam_appid.txt` (chỉ chứa App ID) ở thư mục gốc project để chạy trong Editor và bản build chạy ngoài Steam. **Không** đưa file này vào bản phát hành.
- Bản phát hành mở từ ngoài Steam sẽ được Steam mở lại (`RestartAppIfNecessary`); Editor và Development Build thì không.

## Dùng trong game

Code game chỉ thấy interface của Core, không gọi thẳng Steamworks:

| Interface | Dùng để |
|---|---|
| `IAchievements` | `Unlock("ACH_WIN_ONE_GAME")`, `IsUnlocked` |
| `IStats` | `SetInt("NumWins", n)`, `GetInt`, `Store()` |
| `IPlatformOverlay` | `OverlayChanged`. Framework đã tự Pause qua `OverlayPauseBridge` (state lấy từ `GameFlowAsset.PauseState`) |
| `IPlatformInfo` | `IsAvailable`, `IsSteamDeck`, `UserName` |

Achievement và stat phải được khai báo trước trên trang Steamworks (App Admin → Stats & Achievements) rồi bấm Publish.

## Cloud save

Hai cách, chọn một:

1. **Steam Auto-Cloud (không cần code):** Steamworks → App Admin → Cloud → bật Auto-Cloud, thêm Root Path
   `WinAppDataLocalLow`, Subdirectory `<CompanyName>/<ProductName>/Saves`, Pattern `*.json`.
2. **`SteamCloudSaveStorage`:** ghi song song file local + Steam Remote Storage, đọc ưu tiên bản trên Cloud.
   Cần bật Steam Cloud (Byte quota, Number of files) trên trang Steamworks.

Kiểm chứng "đồng bộ giữa 2 máy" (cổng Giai đoạn 4) phải làm tay: lưu ở máy A, mở game ở máy B cùng tài khoản.

## Test

`Tests/Editor/PlatformTests.cs`: NullPlatform, overlay → Pause, SteamPlatform khi không có Steam, cloud storage khi không có Steam.
Đường "có Steam" chỉ kiểm được trên máy có Steam đang chạy (test sẽ tự bỏ qua nếu Steam đang chạy).

Steam Deck: xem `Documentation~/SteamDeck.md`. Upload lên nhánh beta: xem `Tools/SteamPipe/README.md` ở gốc repo.
