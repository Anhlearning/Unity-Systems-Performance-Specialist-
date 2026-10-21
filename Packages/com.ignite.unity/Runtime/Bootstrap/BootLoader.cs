using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using Ignite.Unity.Scenes;
using Ignite.Unity.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ignite.Unity.Bootstrap
{
    /// <summary>
    /// Scene Boot luôn chạy trước và không bao giờ bị unload.
    /// Build: Boot là build index 0. Editor: BootPlayModeHook đặt Boot làm playModeStartScene
    /// và ghi lại các scene đang mở; GameBootstrapper (hoặc BootLoader nếu Boot chưa có RootLifetimeScope) load lại chúng.
    /// </summary>
    public static class BootLoader
    {
        public const string BootSceneName = "Boot";
        public const string PendingScenesKey = "Ignite.Boot.PendingScenes";

        public static Scene BootScene => SceneManager.GetSceneByName(BootSceneName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnFirstSceneLoaded()
        {
            if (SceneManager.GetActiveScene().name != BootSceneName)
            {
                // Boot bị tắt (menu Tools/Ignite), đang chạy test, hoặc build chưa đặt Boot ở index 0.
                GameLog.Warning(LogChannels.Scene, $"Scene đầu tiên không phải '{BootSceneName}', bỏ qua bootstrap.");
                return;
            }

            // Có RootLifetimeScope thì GameBootstrapper lo phần còn lại.
            if (UnityEngine.Object.FindAnyObjectByType<RootLifetimeScope>() != null)
                return;

            LoadStartScenesAsync(TakePendingScenes()).Forget();
        }

        /// <summary>Lấy (và xoá) danh sách scene đang mở lúc bấm Play. Build luôn trả về rỗng.</summary>
        public static string[] TakePendingScenes()
        {
#if UNITY_EDITOR
            string pending = UnityEditor.SessionState.GetString(PendingScenesKey, string.Empty);
            UnityEditor.SessionState.EraseString(PendingScenesKey);
            return pending.Split(';', StringSplitOptions.RemoveEmptyEntries);
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>Load scene không qua Game Flow: các scene được truyền vào, hoặc build index 1 nếu không có.</summary>
        public static async UniTask LoadStartScenesAsync(string[] scenePaths)
        {
            var loaded = new List<Scene>();

            if (scenePaths.Length > 0)
            {
                foreach (string path in scenePaths)
                {
                    await SceneLoading.LoadAdditive(path);
                    loaded.Add(SceneManager.GetSceneByPath(path));
                }
            }
            else if (SceneManager.sceneCountInBuildSettings > 1)
            {
                await SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
                loaded.Add(SceneManager.GetSceneByBuildIndex(1));
            }

            if (loaded.Count > 0 && loaded[0].IsValid())
                SceneManager.SetActiveScene(loaded[0]);
        }
    }
}
