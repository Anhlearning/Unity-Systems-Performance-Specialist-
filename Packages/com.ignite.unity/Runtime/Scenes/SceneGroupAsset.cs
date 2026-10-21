using System;
using System.Collections.Generic;
using Ignite.Unity.Services;
using UnityEngine;

namespace Ignite.Unity.Scenes
{
    public enum SceneRole
    {
        Active,
        UI,
        HUD,
        Environment,
        Other,
    }

    /// <summary>Cho Inspector biết field string là đường dẫn scene, để hiện ô kéo thả SceneAsset.</summary>
    public sealed class ScenePathAttribute : PropertyAttribute { }

    [Serializable]
    public struct SceneEntry
    {
        [ScenePath] public string path;
        public SceneRole role;
    }

    /// <summary>
    /// Một nhóm scene additive load cùng nhau, mỗi scene có vai trò.
    /// Scene có ở cả nhóm cũ và nhóm mới được giữ lại khi chuyển nhóm.
    /// </summary>
    [CreateAssetMenu(menuName = "Ignite/Scene Group", fileName = "SceneGroup")]
    public sealed class SceneGroupAsset : ScriptableObject
    {
        [SerializeField] List<SceneEntry> scenes = new List<SceneEntry>();
        [Tooltip("Service riêng của nhóm này, sống trong scope con và bị huỷ khi rời nhóm. Có thể để trống.")]
        [SerializeField] InstallerAsset installer;

        public IReadOnlyList<SceneEntry> Scenes => scenes;
        public InstallerAsset Installer => installer;

        /// <summary>Scene có vai trò Active; không có thì lấy scene đầu tiên.</summary>
        public string ActiveScenePath
        {
            get
            {
                foreach (var entry in scenes)
                {
                    if (entry.role == SceneRole.Active)
                        return entry.path;
                }
                return scenes.Count > 0 ? scenes[0].path : null;
            }
        }

        public bool Contains(string scenePath)
        {
            foreach (var entry in scenes)
            {
                if (entry.path == scenePath)
                    return true;
            }
            return false;
        }

        public void GetPaths(List<string> result)
        {
            result.Clear();
            foreach (var entry in scenes)
            {
                if (!string.IsNullOrEmpty(entry.path))
                    result.Add(entry.path);
            }
        }

#if UNITY_EDITOR
        /// <summary>Dùng cho tool editor dựng sẵn nhóm scene.</summary>
        public void EditorSetScenes(IEnumerable<SceneEntry> entries, InstallerAsset groupInstaller = null)
        {
            scenes = new List<SceneEntry>(entries);
            installer = groupInstaller;
        }
#endif
    }
}
