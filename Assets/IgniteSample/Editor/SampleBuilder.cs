using System.Collections.Generic;
using System.Linq;
using Ignite.Unity.Flow;
using Ignite.Unity.Scenes;
using Ignite.Unity.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IgniteSample.Editor
{
    /// <summary>
    /// Dựng lại toàn bộ sample: 3 scene, 2 Scene Group dùng chung SharedUI, Game Flow theo template,
    /// RootLifetimeScope trong Boot, và Build Settings (Boot ở index 0).
    /// </summary>
    public static class SampleBuilder
    {
        const string Root = "Assets/IgniteSample";
        const string BootPath = "Assets/Scene/Boot.unity";

        [MenuItem("Tools/Ignite/Rebuild Sample")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder("Scenes");
            EnsureFolder("Data");

            string menu = MakeScene("Menu", typeof(SampleMenuView), new Color(0.08f, 0.1f, 0.16f));
            string gameplay = MakeScene("Gameplay", typeof(SampleGameplayView), new Color(0.12f, 0.2f, 0.12f));
            string shared = MakeScene("SharedUI", typeof(SampleSharedHud), null);

            var menuGroup = LoadOrCreate<SceneGroupAsset>("SG_Menu");
            menuGroup.EditorSetScenes(new[]
            {
                new SceneEntry { path = menu, role = SceneRole.Active },
                new SceneEntry { path = shared, role = SceneRole.UI },
            });
            var gameplayGroup = LoadOrCreate<SceneGroupAsset>("SG_Gameplay");
            gameplayGroup.EditorSetScenes(new[]
            {
                new SceneEntry { path = gameplay, role = SceneRole.Active },
                new SceneEntry { path = shared, role = SceneRole.UI },
            });

            var flow = LoadOrCreate<GameFlowAsset>("SampleGameFlow");
            flow.ApplyTemplate(menuGroup, gameplayGroup);
            var installer = LoadOrCreate<SampleGameInstaller>("SampleGameInstaller");

            foreach (var asset in new Object[] { menuGroup, gameplayGroup, flow, installer })
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            BuildBoot(AssetDatabase.GetAssetPath(installer), AssetDatabase.GetAssetPath(flow));
            SetBuildScenes(menu, gameplay, shared);

            Debug.Log("[Ignite Sample] Đã dựng lại sample. Mở Boot (hoặc bất kỳ scene sample nào) rồi bấm Play.");
        }

        static void BuildBoot(string installerPath, string flowPath)
        {
            var boot = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);

            // OpenScene (Single) giải phóng asset không còn được tham chiếu, nên phải load lại sau khi mở scene.
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(installerPath);
            var flow = AssetDatabase.LoadAssetAtPath<GameFlowAsset>(flowPath);
            foreach (var go in boot.GetRootGameObjects())
                Object.DestroyImmediate(go);

            var ignite = new GameObject("Ignite");
            var loadingGo = new GameObject("LoadingScreen");
            loadingGo.transform.SetParent(ignite.transform, false);
            var loading = loadingGo.AddComponent<DefaultLoadingScreen>();

            var scope = ignite.AddComponent<RootLifetimeScope>();
            var so = new SerializedObject(scope);
            so.FindProperty("gameInstaller").objectReferenceValue = installer;
            so.FindProperty("gameFlow").objectReferenceValue = flow;
            so.FindProperty("loadingScreen").objectReferenceValue = loading;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(boot);
        }

        static void SetBuildScenes(params string[] samplePaths)
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(BootPath, true) };
            scenes.AddRange(samplePaths.Select(p => new EditorBuildSettingsScene(p, true)));
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (scenes.All(s => s.path != existing.path))
                    scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static string MakeScene(string name, System.Type view, Color? cameraBackground)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (cameraBackground.HasValue)
            {
                var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = cameraBackground.Value;
                cameraGo.AddComponent<AudioListener>();
            }
            new GameObject(view.Name).AddComponent(view);

            string path = $"{Root}/Scenes/Sample_{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static T LoadOrCreate<T>(string name) where T : ScriptableObject
        {
            string path = $"{Root}/Data/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder($"{Root}/{name}"))
                AssetDatabase.CreateFolder(Root, name);
        }
    }
}
