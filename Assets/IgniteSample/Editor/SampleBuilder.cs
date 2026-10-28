using System.Collections.Generic;
using System.Linq;
using Ignite.Ecs;
using Ignite.Unity.Audio;
using Ignite.Unity.Input;
using Ignite.Unity.Flow;
using Ignite.Unity.Scenes;
using Ignite.Unity.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using IgniteSample.Gameplay.View;
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
            SampleLocalizationBuilder.Build();

            string menu = MakeScene("Menu", typeof(SampleMenuView), new Color(0.08f, 0.1f, 0.16f));
            string gameplay = MakeScene("Gameplay", typeof(SampleGameplayView), new Color(0.12f, 0.2f, 0.12f), AddGameplayDemo);
            string shared = MakeScene("SharedUI", typeof(SampleSharedHud), null);

            var menuGroup = LoadOrCreate<SceneGroupAsset>("SG_Menu");
            menuGroup.EditorSetScenes(new[]
            {
                new SceneEntry { path = menu, role = SceneRole.Active },
                new SceneEntry { path = shared, role = SceneRole.UI },
            });
            // Nhóm Gameplay có installer riêng: World ECS sống cùng nhóm này.
            var gameplayInstaller = LoadOrCreate<SampleGameplayInstaller>("SampleGameplayInstaller");
            var gameplayGroup = LoadOrCreate<SceneGroupAsset>("SG_Gameplay");
            gameplayGroup.EditorSetScenes(new[]
            {
                new SceneEntry { path = gameplay, role = SceneRole.Active },
                new SceneEntry { path = shared, role = SceneRole.UI },
            }, gameplayInstaller);

            var flow = LoadOrCreate<GameFlowAsset>("SampleGameFlow");
            flow.ApplyTemplate(menuGroup, gameplayGroup);
            var installer = LoadOrCreate<SampleGameInstaller>("SampleGameInstaller");
            var inputReader = BuildInputReader();
            var audioConfig = BuildAudio();

            foreach (var asset in new Object[] { menuGroup, gameplayGroup, gameplayInstaller, flow, installer })
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            BuildBoot(AssetDatabase.GetAssetPath(installer), AssetDatabase.GetAssetPath(flow),
                AssetDatabase.GetAssetPath(inputReader), AssetDatabase.GetAssetPath(audioConfig));
            SetBuildScenes(menu, gameplay, shared);

            Debug.Log("[Ignite Sample] Đã dựng lại sample. Mở Boot (hoặc bất kỳ scene sample nào) rồi bấm Play.");
        }

        const string InputActionsPath = "Assets/FPS/InputSystem_Actions.inputactions";

        static InputReader BuildInputReader()
        {
            var reader = LoadOrCreate<InputReader>("SampleInputReader");
            var so = new SerializedObject(reader);
            so.FindProperty("actions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reader);
            return reader;
        }

        /// <summary>Dùng âm thanh có sẵn của FPS sample: nhạc nền Menu và tiếng va chạm khi đánh cube.</summary>
        static AudioConfig BuildAudio()
        {
            var library = LoadOrCreate<AudioLibrary>("SampleAudioLibrary");
            library.EditorSetEntries(new[]
            {
                Entry("music.menu", "Assets/FPS/Audio/Music/Wind Ambience.mp3", AudioBus.Music, 0.6f, loop: true),
                Entry("sfx.hit", "Assets/FPS/Audio/SFX/Enemies/Turret_Attack.wav", AudioBus.Sfx, 0.8f),
                Entry("ui.click", "Assets/FPS/Audio/SFX/Pickups/Pickup_Weapon_Small.wav", AudioBus.UI, 0.5f),
            });
            EditorUtility.SetDirty(library);

            var config = LoadOrCreate<AudioConfig>("SampleAudioConfig");
            config.library = library;
            EditorUtility.SetDirty(config);
            return config;
        }

        static AudioLibrary.Entry Entry(string id, string clipPath, AudioBus bus, float volume, bool loop = false)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            return new AudioLibrary.Entry
            {
                id = id,
                clips = clip != null ? new[] { clip } : new AudioClip[0],
                bus = bus,
                volume = volume,
                loop = loop,
            };
        }

        static void BuildBoot(string installerPath, string flowPath, string inputPath, string audioPath)
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
            so.FindProperty("inputReader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputReader>(inputPath);
            so.FindProperty("audioConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioConfig>(audioPath);
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

        /// <summary>Cube bị đánh đổi màu (Buổi 19) và demo trận giằng co (Buổi 20).</summary>
        static void AddGameplayDemo()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "HitTarget";
            cube.transform.position = new Vector3(0f, 0f, 5f);
            cube.AddComponent<EntityLink>();
            cube.AddComponent<HitTargetView>();

            new GameObject("HitFlashPresenter").AddComponent<HitFlashPresenter>();

            var fight = new GameObject("FightDemo");
            fight.AddComponent<FightInputBridge>();
            fight.AddComponent<FightHudPresenter>();
        }

        static string MakeScene(string name, System.Type view, Color? cameraBackground, System.Action addExtras = null)
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
            addExtras?.Invoke();

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
