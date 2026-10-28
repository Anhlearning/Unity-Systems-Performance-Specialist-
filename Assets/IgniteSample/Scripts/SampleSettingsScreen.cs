using Ignite.Core.Settings;
using Ignite.Unity.Input;
using Ignite.Unity.Localization;
using Ignite.Unity.Settings;
using Ignite.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace IgniteSample
{
    /// <summary>
    /// Màn Settings dùng UI Stack: âm lượng 4 bus, chế độ màn hình, vsync, chất lượng, độ phân giải, ngôn ngữ, đổi phím Nhảy.
    /// Thanh trượt áp dụng ngay (Preview), ghi file khi đóng màn. Điều khiển được hoàn toàn bằng gamepad.
    /// </summary>
    public sealed class SampleSettingsScreen : UIScreen
    {
        const string JumpAction = "Player/Jump";

        SettingsService _settings;
        InputService _input;
        InputRebinder _rebinder;
        UIStackService _ui;
        LocalizationService _loc;
        Text _display, _vsync, _quality, _resolution, _language, _jump;
        bool _waitingForKey;

        public void Build(SettingsService settings, InputService input, InputRebinder rebinder, UIStackService ui, LocalizationService localization)
        {
            _settings = settings;
            _input = input;
            _rebinder = rebinder;
            _ui = ui;
            _loc = localization;
            var s = settings.Current;

            SampleUi.Localize(SampleUi.CreateLabel(transform, "", new Vector2(0.5f, 0.92f), 40), _loc, "settings.title");
            var master = Slider("settings.master", 0.82f, s.MasterVolume, v => Preview(x => x.MasterVolume = v));
            Slider("settings.music", 0.75f, s.MusicVolume, v => Preview(x => x.MusicVolume = v));
            Slider("settings.sfx", 0.68f, s.SfxVolume, v => Preview(x => x.SfxVolume = v));
            Slider("settings.ui", 0.61f, s.UiVolume, v => Preview(x => x.UiVolume = v));

            _display = Row(0.52f, CycleDisplay);
            _resolution = Row(0.45f, CycleResolution);
            _vsync = Row(0.38f, () => Preview(x => x.VSync = !x.VSync));
            _quality = Row(0.31f, CycleQuality);
            _language = Row(0.24f, () => Preview(x => x.Language = x.Language == "vi" ? "en" : "vi"));
            _jump = Row(0.17f, Rebind);

            SampleUi.Localize(SampleUi.CreateButton(transform, "", new Vector2(0.5f, 0.07f), () => _ui.Back()), _loc, "settings.back");

            Configure(popup: false, first: master);
            _settings.Store.Changed += _ => Refresh();
            _input.Devices.Changed += _ => Refresh();
            _loc.LanguageApplied += Refresh;
            Refresh();
        }

        public override void OnHide()
        {
            base.OnHide();
            _settings?.Store.Save();
        }

        Slider Slider(string key, float y, float value, System.Action<float> onChange)
        {
            var slider = SampleUi.CreateSlider(transform, "", new Vector2(0.5f, y), value, onChange);
            SampleUi.Localize(slider.transform.parent.GetComponentInChildren<Text>(), _loc, key);
            return slider;
        }

        Text Row(float y, UnityEngine.Events.UnityAction onClick)
        {
            var button = SampleUi.CreateButton(transform, "", new Vector2(0.5f, y), onClick, 700f);
            return button.GetComponentInChildren<Text>();
        }

        void Preview(System.Action<GameSettings> change) => _settings.Store.Preview(change);

        void CycleDisplay() => Preview(x => x.DisplayMode = x.DisplayMode == DisplayMode.Windowed ? DisplayMode.ExclusiveFullScreen : x.DisplayMode + 1);

        void CycleQuality() => Preview(x => x.QualityLevel = (x.QualityLevel + 1) % QualitySettings.names.Length);

        void CycleResolution()
        {
            var all = SettingsService.AvailableResolutions();
            if (all.Count == 0)
                return;
            var current = _settings.Current;
            int index = -1;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].x == current.ResolutionWidth && all[i].y == current.ResolutionHeight)
                    index = i;
            }
            var next = all[(index + 1) % all.Count];
            Preview(x => { x.ResolutionWidth = next.x; x.ResolutionHeight = next.y; });
        }

        void Rebind()
        {
            if (_rebinder.IsRebinding)
                return;
            int binding = _rebinder.FindBindingIndex(JumpAction, _input.Devices.Current);
            if (binding < 0)
                return;
            _waitingForKey = true;
            Refresh();
            _rebinder.Start(JumpAction, binding, _ => { _waitingForKey = false; Refresh(); });
        }

        void Refresh()
        {
            if (_display == null)
                return;
            var s = _settings.Current;
            string quality = QualitySettings.names[s.QualityLevel >= 0 ? s.QualityLevel : QualitySettings.GetQualityLevel()];
            string resolution = s.ResolutionWidth > 0 ? $"{s.ResolutionWidth}×{s.ResolutionHeight}" : _loc.Get("settings.native");

            _display.text = $"{_loc.Get("settings.display")}: {s.DisplayMode}";
            _resolution.text = $"{_loc.Get("settings.resolution")}: {resolution}";
            _vsync.text = $"VSync: {_loc.Get(s.VSync ? "settings.on" : "settings.off")}";
            _quality.text = $"{_loc.Get("settings.quality")}: {quality}";
            _language.text = $"{_loc.Get("settings.language")}: {(s.Language == "vi" ? "Tiếng Việt" : "English")}";
            _jump.text = _waitingForKey ? _loc.Get("settings.press") : $"{_loc.Get("settings.rebind")}: {_input.DisplayFor(JumpAction)}";
        }
    }
}
