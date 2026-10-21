using System;
using System.Collections.Generic;

namespace Ignite.Core.Scenes
{
    /// <summary>
    /// So sánh nhóm scene đang mở với nhóm cần chuyển tới:
    /// có ở cả hai thì giữ, chỉ có ở nhóm cũ thì unload, chỉ có ở nhóm mới thì load.
    /// Ghi vào list do bên gọi cấp sẵn để không sinh rác mỗi lần chuyển.
    /// </summary>
    public static class SceneGroupDiff
    {
        public static void Compute(
            IReadOnlyList<string> current,
            IReadOnlyList<string> target,
            List<string> keep,
            List<string> unload,
            List<string> load)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (target == null) throw new ArgumentNullException(nameof(target));

            keep.Clear();
            unload.Clear();
            load.Clear();

            for (int i = 0; i < current.Count; i++)
            {
                string scene = current[i];
                if (Contains(target, scene))
                {
                    if (!keep.Contains(scene))
                        keep.Add(scene);
                }
                else if (!unload.Contains(scene))
                {
                    unload.Add(scene);
                }
            }

            for (int i = 0; i < target.Count; i++)
            {
                string scene = target[i];
                if (!Contains(current, scene) && !load.Contains(scene))
                    load.Add(scene);
            }
        }

        static bool Contains(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
