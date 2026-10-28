using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Ignite.Unity.Audio
{
    public enum AudioBus
    {
        Master,
        Music,
        Sfx,
        UI,
    }

    /// <summary>Bảng âm thanh theo id: code gọi audio.Play("ui.click"), không cầm thẳng AudioClip.</summary>
    [CreateAssetMenu(menuName = "Ignite/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            [Tooltip("Nhiều clip thì mỗi lần phát chọn ngẫu nhiên một clip.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
            public AudioBus bus = AudioBus.Sfx;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.5f)] public float pitchJitter;
            public bool loop;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        Dictionary<string, Entry> _lookup;

        public bool TryGet(string id, out Entry entry)
        {
            if (_lookup == null || _lookup.Count != entries.Count)
            {
                _lookup = new Dictionary<string, Entry>(entries.Count);
                foreach (var e in entries)
                {
                    if (!string.IsNullOrEmpty(e.id))
                        _lookup[e.id] = e;
                }
            }
            return _lookup.TryGetValue(id, out entry);
        }

        /// <summary>Dùng cho test và tool.</summary>
        public void EditorSetEntries(IEnumerable<Entry> newEntries)
        {
            entries = new List<Entry>(newEntries);
            _lookup = null;
        }
    }

}
