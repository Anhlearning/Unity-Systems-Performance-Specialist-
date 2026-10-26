using System.Collections;
using Ignite.Unity.Localization;
using NUnit.Framework;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

namespace IgniteSample.Tests
{
    /// <summary>
    /// Buổi 37: UI của sample có bản Việt và Anh, đổi ngôn ngữ thì chuỗi đổi theo.
    /// PlayMode vì Localization khởi tạo bất đồng bộ và cần vòng update của game để chạy xong.
    /// </summary>
    public class LocalizationPlayModeTests
    {
        static readonly string[] Keys =
        {
            "menu.title", "menu.play", "menu.settings", "gameplay.hint", "pause.title", "pause.resume", "pause.menu",
            "settings.title", "settings.master", "settings.music", "settings.sfx", "settings.ui", "settings.display",
            "settings.resolution", "settings.quality", "settings.language", "settings.rebind", "settings.press", "settings.back",
        };

        static IEnumerator ApplyAndWait(LocalizationService localization, string language)
        {
            bool applied = false;
            void OnApplied() => applied = true;
            localization.LanguageApplied += OnApplied;
            localization.Apply(language);
            for (int frame = 0; frame < 300 && !applied; frame++)
                yield return null;
            localization.LanguageApplied -= OnApplied;
            Assert.That(applied, Is.True, $"Không đổi được sang '{language}' sau 300 frame.");
        }

        [UnityTest]
        public IEnumerator SwitchingLanguage_ChangesStrings_AndEveryKeyIsTranslated()
        {
            var localization = new LocalizationService(null);
            if (!localization.IsAvailable)
                Assert.Ignore("Chưa có Localization Settings: chạy Tools/Ignite/Rebuild Sample Localization.");

            yield return ApplyAndWait(localization, "vi");
            Assert.That(localization.Get("menu.play"), Is.EqualTo("Chơi"));
            Assert.That(localization.Get("menu.runs", LocalizationService.DefaultTable, 3), Is.EqualTo("Số trận đã chơi: 3"));
            foreach (string key in Keys)
                Assert.That(localization.Get(key), Is.Not.EqualTo(key), $"Thiếu bản dịch tiếng Việt của '{key}'.");

            yield return ApplyAndWait(localization, "en");
            Assert.That(localization.Get("menu.play"), Is.EqualTo("Play"));
            Assert.That(localization.Get("pause.title"), Is.EqualTo("PAUSED"));
            foreach (string key in Keys)
                Assert.That(localization.Get(key), Is.Not.EqualTo(key), $"Thiếu bản dịch tiếng Anh của '{key}'.");

            Assert.That(LocalizationSettings.SelectedLocale.Identifier.Code, Is.EqualTo("en"));
        }

        [UnityTest]
        public IEnumerator MissingKey_ReturnsKeyItself()
        {
            var localization = new LocalizationService(null);
            yield return ApplyAndWait(localization, "vi");
            Assert.That(localization.Get("no.such.key"), Is.EqualTo("no.such.key"));
        }
    }
}
