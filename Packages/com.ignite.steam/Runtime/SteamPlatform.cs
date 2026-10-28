#if IGNITE_STEAMWORKS && (UNITY_EDITOR || UNITY_STANDALONE)
#define IGNITE_STEAM_ACTIVE
#endif
using System;
using Ignite.Core.Logging;
using Ignite.Core.Platform;
using Ignite.Unity.Platform;
using UnityEngine;
using VContainer;
using VContainer.Unity;
#if IGNITE_STEAM_ACTIVE
using Steamworks;
#endif

namespace Ignite.Steam
{
    public sealed class SteamOptions
    {
        /// <summary>480 = Spacewar, App ID thử của Valve. Đổi thành App ID thật trước khi lên Steam.</summary>
        public uint AppId = 480;

        /// <summary>Bản build phát hành mở từ ngoài Steam thì nhờ Steam mở lại game. Tắt trong Editor và Development Build.</summary>
        public bool RestartIfNecessary = !Application.isEditor && !Debug.isDebugBuild;
    }

    /// <summary>
    /// Steamworks.NET: khởi động an toàn. Không có Steam (chưa cài, chưa chạy, thiếu DLL…) thì IsAvailable = false
    /// và mọi lệnh rơi về NullPlatform, game vẫn chạy bình thường.
    /// </summary>
    public sealed class SteamPlatform : IAchievements, IStats, IPlatformOverlay, IPlatformInfo, ITickable, IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Steam");

        /// <summary>Cho các phần khác của package (cloud save) biết Steam đã khởi động chưa.</summary>
        public static bool IsInitialized { get; private set; }

        readonly NullPlatform _fallback = new NullPlatform();
#if IGNITE_STEAM_ACTIVE
        Callback<GameOverlayActivated_t> _overlayCallback;
#endif

        public SteamPlatform(SteamOptions options)
        {
            options ??= new SteamOptions();
            _fallback.OverlayChanged += active => OverlayChanged?.Invoke(active);
#if IGNITE_STEAM_ACTIVE
            try
            {
                if (!Packsize.Test())
                {
                    InitError = "Packsize.Test thất bại: sai bản Steamworks.NET cho nền tảng này.";
                }
                else if (options.RestartIfNecessary && SteamAPI.RestartAppIfNecessary(new AppId_t(options.AppId)))
                {
                    InitError = "Game được mở lại qua Steam.";
                    Application.Quit();
                }
                else
                {
                    IsAvailable = SteamAPI.Init();
                    if (!IsAvailable)
                        InitError = "SteamAPI.Init() thất bại: Steam chưa chạy, hoặc thiếu steam_appid.txt khi chạy ngoài Steam.";
                }
            }
            catch (DllNotFoundException e)
            {
                InitError = $"Không tìm thấy steam_api: {e.Message}";
            }
            catch (Exception e)
            {
                InitError = e.Message;
            }

            if (IsAvailable)
            {
                IsInitialized = true;
                _overlayCallback = Callback<GameOverlayActivated_t>.Create(cb => OverlayChanged?.Invoke(cb.m_bActive != 0));
                GameLog.Info(Log, $"Steam sẵn sàng: {UserName}, App ID {options.AppId}, Steam Deck: {IsSteamDeck}");
                return;
            }
#else
            InitError = "Không có Steamworks.NET cho nền tảng này.";
#endif
            GameLog.Warning(Log, $"Chạy không có Steam: {InitError}");
        }

        public string Name => IsAvailable ? "Steam" : "Steam (offline)";
        public bool IsAvailable { get; }
        public string InitError { get; }

        public event Action<bool> OverlayChanged;

        public bool IsSteamDeck
        {
            get
            {
#if IGNITE_STEAM_ACTIVE
                if (IsAvailable)
                    return SteamUtils.IsSteamRunningOnSteamDeck();
#endif
                return false;
            }
        }

        public string UserName
        {
            get
            {
#if IGNITE_STEAM_ACTIVE
                if (IsAvailable)
                    return SteamFriends.GetPersonaName();
#endif
                return _fallback.UserName;
            }
        }

        public bool Unlock(string id)
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
            {
                bool ok = SteamUserStats.SetAchievement(id) && SteamUserStats.StoreStats();
                GameLog.Info(Log, $"Achievement {id}: {(ok ? "mở" : "lỗi")}");
                return ok;
            }
#endif
            return _fallback.Unlock(id);
        }

        public bool IsUnlocked(string id)
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
                return SteamUserStats.GetAchievement(id, out bool achieved) && achieved;
#endif
            return _fallback.IsUnlocked(id);
        }

        public bool SetInt(string id, int value)
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
                return SteamUserStats.SetStat(id, value);
#endif
            return _fallback.SetInt(id, value);
        }

        public int GetInt(string id)
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
                return SteamUserStats.GetStat(id, out int value) ? value : 0;
#endif
            return _fallback.GetInt(id);
        }

        public bool Store()
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
                return SteamUserStats.StoreStats();
#endif
            return _fallback.Store();
        }

        /// <summary>Giả lập overlay khi không có Steam (test, console debug).</summary>
        public void SimulateOverlay(bool active) => _fallback.SimulateOverlay(active);

        public void Tick()
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
                SteamAPI.RunCallbacks();
#endif
        }

        public void Dispose()
        {
#if IGNITE_STEAM_ACTIVE
            if (IsAvailable)
            {
                _overlayCallback?.Dispose();
                SteamAPI.Shutdown();
                IsInitialized = false;
            }
#endif
        }
    }

    public static class SteamInstaller
    {
        /// <summary>Gọi trong GameInstaller.ConfigurePlatform. Thay NullPlatform bằng Steam (tự rơi về Null nếu Steam không chạy).</summary>
        public static void Install(IContainerBuilder builder, uint appId = 480)
        {
            builder.RegisterEntryPoint<SteamPlatform>().WithParameter(new SteamOptions { AppId = appId }).AsSelf();
            PlatformInstaller.RegisterCommon(builder);
        }
    }
}
