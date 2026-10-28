using System;
using System.Collections.Generic;
using Ignite.Core.Logging;
using Ignite.Core.Settings;
using Ignite.Unity.Audio;
using Ignite.Unity.Input;
using UnityEngine;

namespace Ignite.Unity.Settings
{
    /// <summary>
    /// Nạp settings lúc khởi động và áp dụng mỗi khi settings đổi: âm lượng 4 bus, override phím,
    /// độ phân giải / chế độ màn hình / vsync / chất lượng. Ngôn ngữ được phát qua LanguageChanged cho Localization.
    /// </summary>
    public sealed class SettingsService : IDisposable
    {
        readonly SettingsStore _store;
        readonly AudioService _audio;
        readonly InputService _input;
        string _language;

        public SettingsService(SettingsStore store, AudioService audio, InputService input)
        {
            _store = store;
            _audio = audio;
            _input = input;
            _store.Changed += Apply;
            _store.Load();
        }

        public GameSettings Current => _store.Current;
        public SettingsStore Store => _store;

        public event Action<string> LanguageChanged;

        public void Change(Action<GameSettings> change) => _store.Apply(change);

        /// <summary>Danh sách độ phân giải màn hình hỗ trợ, không trùng lặp, từ nhỏ tới lớn.</summary>
        public static IReadOnlyList<Vector2Int> AvailableResolutions()
        {
            var result = new List<Vector2Int>();
            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!result.Contains(size))
                    result.Add(size);
            }
            return result;
        }

        public static FullScreenMode ToUnity(DisplayMode mode) => (FullScreenMode)(int)mode;

        void Apply(GameSettings settings)
        {
            if (_audio != null)
            {
                _audio.SetBusVolume(AudioBus.Master, settings.MasterVolume);
                _audio.SetBusVolume(AudioBus.Music, settings.MusicVolume);
                _audio.SetBusVolume(AudioBus.Sfx, settings.SfxVolume);
                _audio.SetBusVolume(AudioBus.UI, settings.UiVolume);
            }

            _input?.LoadOverrides(settings.BindingOverrides);

            // Ở Edit mode (test, tool) mà đổi QualitySettings thì Unity ghi thẳng vào ProjectSettings của project.
            if (Application.isPlaying)
                ApplyDisplay(settings);

            if (_language != settings.Language)
            {
                _language = settings.Language;
                LanguageChanged?.Invoke(_language);
            }

            GameLog.Info(SettingsStore.Log, $"Áp dụng settings: âm lượng {settings.MasterVolume:0.##}, {settings.DisplayMode}, vsync {settings.VSync}, ngôn ngữ {settings.Language}");
        }

        static void ApplyDisplay(GameSettings settings)
        {
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
            if (settings.QualityLevel >= 0 && settings.QualityLevel < QualitySettings.names.Length && settings.QualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(settings.QualityLevel, true);

            // Trong Editor, Game view tự quản kích thước: chỉ đổi độ phân giải thật ở bản build.
            if (Application.isEditor)
                return;
            if (settings.ResolutionWidth > 0 && settings.ResolutionHeight > 0)
                Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, ToUnity(settings.DisplayMode));
            else
                Screen.fullScreenMode = ToUnity(settings.DisplayMode);
        }

        public void Dispose() => _store.Changed -= Apply;
    }
}
