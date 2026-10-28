# Upload lên nhánh beta (Buổi 39)

Phần này cần tài khoản Steamworks partner của game, nên phải làm tay. Repo chỉ giữ template, không giữ thông tin đăng nhập.

## Một lần cho mỗi game
1. Trên Steamworks: tạo nhánh `beta` (App Admin → SteamPipe → Builds → Branches), đặt mật khẩu nếu chỉ cho người thử.
2. Sửa `<APP_ID>` trong `app_build.vdf` và `<DEPOT_ID>` trong cả hai file vdf.
3. Tải Steamworks SDK, đặt biến môi trường `STEAMWORKS_SDK` và `STEAM_BUILD_USER`.

## Mỗi lần upload
1. Build Windows 64-bit **Release** (không Development) vào `Builds/Steam/`. Không để `steam_appid.txt` trong thư mục build.
2. Chạy `Tools/SteamPipe/upload.bat`, nhập mật khẩu và mã Steam Guard khi được hỏi.
3. Trên Steam: Library → game → Properties → Betas → chọn `beta`, tải về và chạy thử.

## Cổng Giai đoạn 4 (làm tay)
- [ ] Build lên được nhánh beta.
- [ ] Achievement mở được (xem trong Steam overlay hoặc trang cộng đồng).
- [ ] Cloud save đồng bộ giữa 2 máy cùng tài khoản.
- [ ] Mở overlay thì game Pause.
- [ ] Tắt Steam rồi chạy file exe: game vẫn chạy (rơi về NullPlatform).
