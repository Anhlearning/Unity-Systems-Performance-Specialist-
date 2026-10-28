using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Ignite.Core.Pooling;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Ignite.Unity.Audio
{
    /// <summary>
    /// Phát âm thanh theo id qua pool AudioSource, chia 4 bus (Master/Music/SFX/UI).
    /// AudioSource phát xong được trả về pool trong Tick (không sinh rác mỗi frame).
    /// </summary>
    public sealed class AudioService : ITickable, IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Audio");

        readonly AudioConfig _config;
        readonly AudioLibrary _library;
        readonly ObjectPool<AudioSource> _pool;
        readonly List<(AudioSource source, AudioLibrary.Entry entry)> _playing = new List<(AudioSource, AudioLibrary.Entry)>();
        readonly float[] _busVolume = { 1f, 1f, 1f, 1f };
        readonly GameObject _root;
        AudioSource _music;
        AudioLibrary.Entry _musicEntry;

        public AudioService(AudioConfig config = null, AudioLibrary library = null)
        {
            _config = config;
            _library = library != null ? library : config != null ? config.library : null;

            _root = new GameObject("[Audio]");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root); // sống suốt game, kể cả khi scene active đổi
            _pool = new ObjectPool<AudioSource>(CreateSource,
                onGet: s => s.gameObject.SetActive(true),
                onRelease: s => { s.Stop(); s.clip = null; s.gameObject.SetActive(false); },
                onDestroy: s => DestroyObject(s.gameObject),
                initialCapacity: PoolSize);
            _pool.Prewarm(PoolSize);
        }

        int PoolSize => _config != null ? _config.poolSize : 16;
        public int PlayingCount => _playing.Count;
        public int PooledCount => _pool.CountInactive;

        public float GetBusVolume(AudioBus bus) => _busVolume[(int)bus];

        public void SetBusVolume(AudioBus bus, float linear)
        {
            _busVolume[(int)bus] = Mathf.Clamp01(linear);
            if (_config != null && _config.mixer != null)
                _config.mixer.SetFloat($"{bus}Volume", LinearToDecibels(_busVolume[(int)bus]));
            RefreshVolumes();
        }

        /// <summary>0..1 sang dB cho AudioMixer: 1 → 0 dB, 0.5 → khoảng −6 dB, 0 → −80 dB (tắt tiếng).</summary>
        public static float LinearToDecibels(float linear) => linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);

        /// <summary>Âm lượng cuối của một âm thanh: khi không có mixer thì framework tự nhân bus × master.</summary>
        public float EffectiveVolume(AudioLibrary.Entry entry)
        {
            if (_config != null && _config.mixer != null)
                return entry.volume;
            float bus = entry.bus == AudioBus.Master ? 1f : _busVolume[(int)entry.bus];
            return entry.volume * bus * _busVolume[(int)AudioBus.Master];
        }

        /// <summary>Phát một lần. Có vị trí thì âm thanh 3D tại đó, không thì 2D. Trả về false nếu id không có.</summary>
        public bool Play(string id, Vector3? position = null)
        {
            if (!TryGetEntry(id, out var entry) || entry.clips.Length == 0)
                return false;

            var source = _pool.Get();
            Configure(source, entry);
            source.spatialBlend = position.HasValue ? 1f : 0f;
            source.transform.position = position ?? Vector3.zero;
            source.Play();
            _playing.Add((source, entry));
            return true;
        }

        public bool PlayMusic(string id)
        {
            if (!TryGetEntry(id, out var entry) || entry.clips.Length == 0)
                return false;

            if (_music == null)
            {
                _music = CreateSource();
                _music.gameObject.name = "Music";
                _music.gameObject.SetActive(true);
            }
            _musicEntry = entry;
            Configure(_music, entry);
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.Play();
            return true;
        }

        public void StopMusic()
        {
            if (_music != null)
                _music.Stop();
            _musicEntry = null;
        }

        public void Tick()
        {
            for (int i = _playing.Count - 1; i >= 0; i--)
            {
                var source = _playing[i].source;
                if (source != null && (source.isPlaying || source.loop))
                    continue;
                _playing.RemoveAt(i);
                if (source != null)
                    _pool.Release(source);
            }
        }

        public void StopAll()
        {
            foreach (var (source, _) in _playing)
            {
                if (source != null)
                    _pool.Release(source);
            }
            _playing.Clear();
            StopMusic();
        }

        bool TryGetEntry(string id, out AudioLibrary.Entry entry)
        {
            entry = null;
            if (_library != null && _library.TryGet(id, out entry))
                return true;
            GameLog.Warning(Log, $"Không có âm thanh '{id}' trong AudioLibrary.");
            return false;
        }

        void Configure(AudioSource source, AudioLibrary.Entry entry)
        {
            source.clip = entry.clips.Length == 1 ? entry.clips[0] : entry.clips[Random.Range(0, entry.clips.Length)];
            source.loop = entry.loop;
            source.pitch = 1f + (entry.pitchJitter > 0f ? Random.Range(-entry.pitchJitter, entry.pitchJitter) : 0f);
            source.volume = EffectiveVolume(entry);
            if (_config != null)
                source.outputAudioMixerGroup = _config.GroupOf(entry.bus);
        }

        void RefreshVolumes()
        {
            foreach (var (source, entry) in _playing)
            {
                if (source != null)
                    source.volume = EffectiveVolume(entry);
            }
            if (_music != null && _musicEntry != null)
                _music.volume = EffectiveVolume(_musicEntry);
        }

        AudioSource CreateSource()
        {
            var go = new GameObject("AudioSource");
            go.transform.SetParent(_root.transform, false);
            go.SetActive(false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        public void Dispose()
        {
            StopAll();
            _pool.Clear();
            DestroyObject(_root);
        }

        static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }
    }
}
