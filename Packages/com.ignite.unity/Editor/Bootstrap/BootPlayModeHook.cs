using System.IO;
using System.Linq;
using Ignite.Unity.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Ignite.Unity.Editor.Bootstrap
{
    /// <summary>Bấm Play ở scene nào cũng vào Boot trước, rồi Boot load lại các scene đang mở.</summary>
    [InitializeOnLoad]
    static class BootPlayModeHook
    {
        const string EnabledPref = "Ignite.Boot.PlayFromBoot";
        const string MenuPath = "Tools/Ignite/Play From Boot";

        static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPref, true);
            set => EditorPrefs.SetBool(EnabledPref, value);
        }

        static BootPlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
                return;

            EditorSceneManager.playModeStartScene = null;
            if (!Enabled)
                return;

            // Test Runner vào Play mode bằng scene riêng (InitTestScene...): không được ép nó qua Boot.
            if (SceneManager.GetActiveScene().name.StartsWith("InitTestScene"))
                return;

            var boot = FindBootScene();
            if (boot == null)
            {
                UnityEngine.Debug.LogWarning($"[Boot] Không tìm thấy scene '{BootLoader.BootSceneName}.unity', Play bình thường.");
                return;
            }

            // Scene được Boot load lại từ file, nên thay đổi chưa lưu phải được lưu trước.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            string bootPath = AssetDatabase.GetAssetPath(boot);
            var active = SceneManager.GetActiveScene();
            var openScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(s => s.isLoaded && !string.IsNullOrEmpty(s.path) && s.path != bootPath)
                .OrderByDescending(s => s == active) // scene active load trước để thành active scene
                .Select(s => s.path);

            SessionState.SetString(BootLoader.PendingScenesKey, string.Join(";", openScenes));
            EditorSceneManager.playModeStartScene = boot;
        }

        static SceneAsset FindBootScene()
        {
            return AssetDatabase.FindAssets($"t:SceneAsset {BootLoader.BootSceneName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == BootLoader.BootSceneName)
                .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                .FirstOrDefault();
        }
    }
}
