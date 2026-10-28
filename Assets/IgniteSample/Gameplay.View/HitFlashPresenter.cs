using Ignite.Ecs;
using Ignite.Unity.Audio;
using VContainer;

namespace IgniteSample.Gameplay.View
{
    /// <summary>Consumer duy nhất của DamagedEvent: tìm view của entity bị đánh và cho nó đổi màu.</summary>
    public sealed class HitFlashPresenter : PresenterBase<DamagedEvent>
    {
        AudioService _audio;

        public int EventsReceived { get; private set; }

        [Inject]
        void InjectAudio(AudioService audio) => _audio = audio;

        protected override void OnEvent(in DamagedEvent evt)
        {
            EventsReceived++;
            _audio?.Play("sfx.hit");
            if (Views.TryGetView(evt.Target, out var link) && link.TryGetComponent<HitTargetView>(out var view))
                view.ShowDamage(evt.Max > 0f ? evt.Remaining / evt.Max : 0f);
        }
    }
}
