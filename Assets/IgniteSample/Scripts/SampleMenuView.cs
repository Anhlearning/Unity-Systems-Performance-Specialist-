using Cysharp.Threading.Tasks;
using Ignite.Unity.Audio;
using Ignite.Unity.Flow;
using Ignite.Unity.Localization;
using Ignite.Unity.UI;
using UnityEngine;
using VContainer;

namespace IgniteSample
{
    // Quy ước: MonoBehaviour trong scene nhận dependency qua method [Inject] và khởi tạo ở đó,
    // vì scope của nhóm scene chỉ được build sau khi cả nhóm load xong.

    /// <summary>Scene Menu: màn gốc của UI Stack với nút Play và Settings. Chọn sẵn nút Play cho gamepad.</summary>
    public sealed class SampleMenuView : MonoBehaviour
    {
        [Inject]
        void Construct(FlowController flow, SampleSession session, UIStackService ui, SampleScreens screens,
            AudioService audio, LocalizationService localization)
        {
            audio.PlayMusic("music.menu");
            var canvas = SampleUi.CreateCanvas(transform, "MenuCanvas", 0);
            var menu = SampleUi.CreateScreen<UIScreen>(canvas.transform, "MenuScreen", new Color(0.08f, 0.1f, 0.16f));
            SampleUi.Localize(SampleUi.CreateLabel(menu.transform, "", new Vector2(0.5f, 0.68f), 44), localization, "menu.title");
            SampleUi.Localize(SampleUi.CreateLabel(menu.transform, "", new Vector2(0.5f, 0.58f), 24), localization, "menu.runs", session.RunsStarted);

            var play = SampleUi.CreateButton(menu.transform, "", new Vector2(0.5f, 0.44f), () =>
            {
                audio.Play("ui.click");
                session.StartRun();
                flow.GoAsync("Gameplay").Forget();
            });
            SampleUi.Localize(play, localization, "menu.play");

            var settings = SampleUi.CreateButton(menu.transform, "", new Vector2(0.5f, 0.34f), () =>
            {
                audio.Play("ui.click");
                if (screens.Settings != null && !ui.Contains(screens.Settings))
                    ui.Push(screens.Settings);
            });
            SampleUi.Localize(settings, localization, "menu.settings");

            menu.Configure(popup: false, first: play);
            ui.Push(menu);
        }
    }
}
