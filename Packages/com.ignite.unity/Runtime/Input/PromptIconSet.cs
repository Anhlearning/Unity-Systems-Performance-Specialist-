using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ignite.Unity.Input
{
    /// <summary>Icon gợi ý phím cho một scheme, tra theo đường dẫn control (ví dụ "&lt;Gamepad&gt;/buttonSouth").</summary>
    [CreateAssetMenu(menuName = "Ignite/Prompt Icon Set", fileName = "PromptIcons")]
    public sealed class PromptIconSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string controlPath;
            public Sprite icon;
        }

        [SerializeField] InputScheme scheme;
        [SerializeField] List<Entry> icons = new List<Entry>();

        public InputScheme Scheme => scheme;

        public Sprite Find(string controlPath)
        {
            foreach (var entry in icons)
            {
                if (string.Equals(entry.controlPath, controlPath, StringComparison.OrdinalIgnoreCase))
                    return entry.icon;
            }
            return null;
        }
    }
}
