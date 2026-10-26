@echo off
REM Upload bản build trong Builds\Steam lên nhánh beta bằng SteamPipe.
REM Cần:
REM   - Steamworks SDK, đặt biến môi trường STEAMWORKS_SDK trỏ tới thư mục sdk (có tools\ContentBuilder).
REM   - Biến STEAM_BUILD_USER: tài khoản Steam có quyền upload build cho app. Mật khẩu và Steam Guard nhập khi chạy,
REM     không bao giờ lưu trong file này hay trong repo.
REM   - Đã sửa <APP_ID> và <DEPOT_ID> trong app_build.vdf và depot_build.vdf.

if "%STEAMWORKS_SDK%"=="" (
    echo Thiếu biến STEAMWORKS_SDK.
    exit /b 1
)
if "%STEAM_BUILD_USER%"=="" (
    echo Thiếu biến STEAM_BUILD_USER.
    exit /b 1
)
if not exist "%~dp0..\..\Builds\Steam\" (
    echo Chưa có bản build trong Builds\Steam. Build Windows 64-bit Release vào đó trước.
    exit /b 1
)

"%STEAMWORKS_SDK%\tools\ContentBuilder\builder\steamcmd.exe" +login %STEAM_BUILD_USER% +run_app_build "%~dp0app_build.vdf" +quit
