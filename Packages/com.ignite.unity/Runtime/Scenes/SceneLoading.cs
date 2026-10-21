using Ignite.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ignite.Unity.Scenes
{
    /// <summary>Load scene additive theo đường dẫn. Trong Editor, scene chưa có trong Build Settings vẫn load được (kèm cảnh báo).</summary>
    public static class SceneLoading
    {
        public static AsyncOperation LoadAdditive(string scenePath)
        {
            if (SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0)
                return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);

#if UNITY_EDITOR
            GameLog.Warning(LogChannels.Scene, $"'{scenePath}' chưa có trong Build Settings: Editor vẫn load được nhưng bản build sẽ thiếu scene này.");
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            GameLog.Error(LogChannels.Scene, $"'{scenePath}' không có trong Build Settings.");
            return null;
#endif
        }
    }
}
