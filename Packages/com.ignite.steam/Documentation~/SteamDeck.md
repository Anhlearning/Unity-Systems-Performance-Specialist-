# Checklist Steam Deck (Buổi 38)

Framework đã lo phần tự động kiểm được; phần còn lại phải thử trên máy thật (hoặc Steam Deck Dev Kit).

## Framework đã có

| Hạng mục | Ở đâu | Đã kiểm |
|---|---|---|
| Điều khiển hoàn toàn bằng gamepad | `UIScreen` chọn sẵn phần tử đầu tiên, nhớ focus khi quay lại | PlayMode test `UIScreenFocusPlayModeTests` |
| Nút Back thống nhất (B) | `UIStackService` + action `UI/Cancel` | EditMode test `UiCancel_IsTheSharedBackButton` |
| Icon gợi ý phím theo thiết bị | `DeviceTracker`, `InputService.DisplayFor/IconFor` | EditMode test `PromptText_ChangesWithDevice` |
| Nhận biết Steam Deck | `IPlatformInfo.IsSteamDeck` (Steam API), `DeviceTracker.IsSteamDeck` | Chỉ kiểm được trên Deck |
| Mở overlay (nút Steam) thì Pause | `OverlayPauseBridge` | EditMode test `OverlayOpened_DuringGameplay_PausesGame` |
| UI theo 1280×800 | `CanvasScaler` reference 1280×800 trong sample | Xem bên dưới |

## Phải thử trên máy thật

- [ ] Chạy bản build qua Steam (nhánh beta) trên Deck, không cắm bàn phím/chuột.
- [ ] Đi hết luồng Menu → Gameplay → Pause → Settings → đổi phím → Menu chỉ bằng gamepad.
- [ ] Chữ nhỏ nhất đọc được ở 1280×800 (sample dùng cỡ tối thiểu 18 ở độ phân giải tham chiếu 1280×800).
- [ ] Bàn phím ảo hiện khi cần nhập chữ (nếu game có ô nhập).
- [ ] Nút Steam / "…" mở overlay → game Pause; đóng overlay rồi Resume được.
- [ ] Sleep/Resume máy giữa trận: game không crash, âm thanh trở lại.
- [ ] FPS ổn định ở 60/40/30 Hz (chỉnh trong Quick Access); không vượt TDP.
- [ ] Lần đầu mở game: ngôn ngữ, độ phân giải mặc định hợp lý (Deck mặc định 1280×800, Fullscreen).
- [ ] Gửi Steam Deck Compatibility Review trên trang Steamworks khi đã qua hết mục trên.
