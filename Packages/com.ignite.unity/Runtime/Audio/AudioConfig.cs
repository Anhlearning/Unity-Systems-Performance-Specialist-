using UnityEngine;
using UnityEngine.Audio;

namespace Ignite.Unity.Audio
{
    /// <summary>
    /// Cấu hình audio của game. Mixer là tuỳ chọn: có thì mỗi bus đi qua một AudioMixerGroup và âm lượng đặt bằng
    /// tham số exposed "MasterVolume", "MusicVolume", "SfxVolume", "UIVolume"; không có thì framework tự nhân âm lượng theo bus.
    /// </summary>
    [CreateAssetMenu(menuName = "Ignite/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        public AudioLibrary library;
        public AudioMixer mixer;
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup uiGroup;
        [Min(1)] public int poolSize = 16;
        [Min(0f)] public float musicFadeSeconds = 0.75f;

        public AudioMixerGroup GroupOf(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Music: return musicGroup != null ? musicGroup : masterGroup;
                case AudioBus.Sfx: return sfxGroup != null ? sfxGroup : masterGroup;
                case AudioBus.UI: return uiGroup != null ? uiGroup : masterGroup;
                default: return masterGroup;
            }
        }
    }
}
