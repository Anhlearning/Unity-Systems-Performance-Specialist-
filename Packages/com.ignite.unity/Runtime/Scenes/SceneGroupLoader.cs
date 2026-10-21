using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using Ignite.Core.Scenes;
using Ignite.Unity.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Ignite.Unity.Scenes
{
    /// <summary>
    /// Adaptive scene load theo Scene Group. Chuyển từ nhóm A sang nhóm B:
    /// 1. Hiện màn loading.
    /// 2. So sánh A với B: giữ scene chung, unload scene chỉ có ở A, load scene chỉ có ở B.
    /// 3. Huỷ scope con của A, unload, UnloadUnusedAssets, load additive song song.
    /// 4. Đặt Active scene theo vai trò, build scope con của B, inject vào mọi scene của B.
    /// 5. Ẩn loading.
    /// MonoBehaviour trong scene nhận dependency qua method [Inject] và khởi tạo ở đó, không dựa vào Start,
    /// vì scope con chỉ được build sau khi cả nhóm load xong. Scene được giữ lại sẽ được inject lại từ scope mới.
    /// </summary>
    public sealed class SceneGroupLoader
    {
        readonly LifetimeScope _root;
        readonly ILoadingScreen _loadingScreen;

        readonly List<string> _loadedPaths = new List<string>();
        readonly List<string> _targetPaths = new List<string>();
        readonly List<string> _keep = new List<string>();
        readonly List<string> _unload = new List<string>();
        readonly List<string> _load = new List<string>();
        readonly List<AsyncOperation> _operations = new List<AsyncOperation>();
        readonly List<GameObject> _roots = new List<GameObject>();

        LifetimeScope _groupScope;

        public SceneGroupLoader(LifetimeScope root, ILoadingScreen loadingScreen)
        {
            _root = root;
            _loadingScreen = loadingScreen;
        }

        public SceneGroupAsset Current { get; private set; }
        public bool IsLoading { get; private set; }
        public LifetimeScope GroupScope => _groupScope;

        /// <summary>Kết quả của lần chuyển nhóm gần nhất, để log và test kiểm tra scene nào được giữ.</summary>
        public IReadOnlyList<string> LastKept => _keep;
        public IReadOnlyList<string> LastUnloaded => _unload;
        public IReadOnlyList<string> LastLoaded => _load;
        public long LastDurationMs { get; private set; }

        public async UniTask LoadAsync(SceneGroupAsset target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (IsLoading)
                throw new InvalidOperationException($"Đang load nhóm scene, không thể load '{target.name}' cùng lúc.");

            IsLoading = true;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _loadingScreen.ShowAsync();

                target.GetPaths(_targetPaths);
                SceneGroupDiff.Compute(_loadedPaths, _targetPaths, _keep, _unload, _load);

                DisposeGroupScope();

                // Active scene không được là scene sắp unload.
                var boot = BootLoader.BootScene;
                if (boot.isLoaded)
                    SceneManager.SetActiveScene(boot);

                foreach (string path in _unload)
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    if (scene.isLoaded)
                        await SceneManager.UnloadSceneAsync(scene);
                }
                if (_unload.Count > 0)
                    await Resources.UnloadUnusedAssets();

                await LoadInParallel();

                _loadedPaths.Clear();
                _loadedPaths.AddRange(_targetPaths);

                SetActiveScene(target);
                BuildGroupScope(target);
                Current = target;

                LastDurationMs = stopwatch.ElapsedMilliseconds;
                GameLog.Info(LogChannels.Scene,
                    $"Nhóm '{target.name}': giữ {_keep.Count}, unload {_unload.Count}, load {_load.Count}, {LastDurationMs} ms.");

                _loadingScreen.SetProgress(1f);
                await _loadingScreen.HideAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        async UniTask LoadInParallel()
        {
            _operations.Clear();
            foreach (string path in _load)
            {
                var operation = SceneLoading.LoadAdditive(path);
                if (operation != null)
                    _operations.Add(operation);
            }

            while (true)
            {
                bool done = true;
                float sum = 0f;
                foreach (var operation in _operations)
                {
                    done &= operation.isDone;
                    sum += operation.isDone ? 1f : operation.progress;
                }

                _loadingScreen.SetProgress(_operations.Count == 0 ? 1f : sum / _operations.Count);
                if (done)
                    break;
                await UniTask.Yield();
            }
        }

        void SetActiveScene(SceneGroupAsset group)
        {
            string path = group.ActiveScenePath;
            if (path == null)
                return;

            var scene = SceneManager.GetSceneByPath(path);
            if (scene.isLoaded)
                SceneManager.SetActiveScene(scene);
        }

        void BuildGroupScope(SceneGroupAsset group)
        {
            var installer = group.Installer;
            _groupScope = _root.CreateChild(builder =>
            {
                if (installer != null)
                    installer.Install(builder);
            }, $"Scope ({group.name})");

            foreach (string path in _loadedPaths)
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.isLoaded)
                    continue;

                scene.GetRootGameObjects(_roots);
                foreach (var go in _roots)
                    _groupScope.Container.InjectGameObject(go);
            }
            _roots.Clear();
        }

        void DisposeGroupScope()
        {
            if (_groupScope == null)
                return;
            _groupScope.Dispose();
            _groupScope = null;
        }
    }
}
