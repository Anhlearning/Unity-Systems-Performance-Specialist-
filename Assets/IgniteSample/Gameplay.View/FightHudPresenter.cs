using Ignite.Core.Platform;
using Ignite.Ecs;
using VContainer;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.UI;

namespace IgniteSample.Gameplay.View
{
    /// <summary>
    /// HUD của trận giằng co. Event (kiệt sức, đứt dây…) đọc từ EventQueue; thanh thể lực và độ vướng dây là state liên tục
    /// nên đọc thẳng component (đúng quy ước Bridge). Chỉ dựng lại chuỗi khi số hiển thị thay đổi.
    /// </summary>
    public sealed class FightHudPresenter : PresenterBase<FightEvent>
    {
        Text _status;
        Text _message;
        Entity _fight;
        bool _hasFight;
        int _shownStamina = -1;
        int _shownSnag = -1;
        string _shownState;

        IAchievements _achievements;
        IStats _stats;

        public string LastMessage => _message != null ? _message.text : null;

        /// <summary>Achievement và stat của app thử Spacewar (480). Game thật dùng id của mình trên trang Steamworks.</summary>
        public const string FirstCatchAchievement = "ACH_WIN_ONE_GAME";
        public const string CatchStat = "NumWins";

        [Inject]
        void InjectPlatform(IAchievements achievements, IStats stats)
        {
            _achievements = achievements;
            _stats = stats;
        }
        public FightEventKind? LastEvent { get; private set; }

        protected override void OnInjected()
        {
            var canvas = SampleUi.CreateCanvas(transform, "FightHud", 20);
            _status = SampleUi.CreateLabel(canvas.transform, "Space: móc quái · H: đánh cube", new Vector2(0.5f, 0.78f), 24);
            _message = SampleUi.CreateLabel(canvas.transform, string.Empty, new Vector2(0.5f, 0.7f), 28);
        }

        protected override void OnEvent(in FightEvent evt)
        {
            _fight = evt.Fight;
            _hasFight = true;
            LastEvent = evt.Kind;
            _message.text = Describe(evt.Kind);

            if (evt.Kind == FightEventKind.Caught && _achievements != null)
            {
                _achievements.Unlock(FirstCatchAchievement);
                _stats.SetInt(CatchStat, _stats.GetInt(CatchStat) + 1);
                _stats.Store();
            }
        }

        protected override void OnLateFrame()
        {
            if (!_hasFight || World.IsDisposed(_fight))
                return;

            var stats = World.GetStash<BeastStats>().Get(_fight);
            int stamina = Mathf.CeilToInt(World.GetStash<BeastStamina>().Get(_fight).Current);
            int snag = Mathf.RoundToInt(World.GetStash<FightLine>().Get(_fight).Snag / 2f * 100f);
            string state = StateOf(_fight);

            if (stamina == _shownStamina && snag == _shownSnag && state == _shownState)
                return;

            _shownStamina = stamina;
            _shownSnag = snag;
            _shownState = state;
            _status.text = $"{state} · hiệp {stats.Phase} · thể lực {stamina}/{Mathf.RoundToInt(stats.MaxStamina)} · dây vướng đá {snag}%";
        }

        string StateOf(Entity fight)
        {
            if (World.GetStash<FightOver>().Has(fight)) return "KẾT THÚC";
            if (World.GetStash<Exhausted>().Has(fight)) return "KIỆT SỨC (giữ F)";
            if (World.GetStash<Recovering>().Has(fight)) return "HỒI SỨC";
            return "GIẰNG CO";
        }

        static string Describe(FightEventKind kind)
        {
            switch (kind)
            {
                case FightEventKind.Started: return "MÓC TRÚNG!";
                case FightEventKind.RockHit: return "Quái tông vỡ đá · −8% thể lực";
                case FightEventKind.Exhausted: return "QUÁI KIỆT SỨC! Giữ F để thu dây";
                case FightEventKind.Recovering: return "NÓ HỒI SỨC… chuẩn bị hiệp cuồng nộ";
                case FightEventKind.Enraged: return "CUỒNG NỘ!";
                case FightEventKind.Caught: return "BẮT ĐƯỢC!";
                case FightEventKind.Escaped: return "QUÁI ĐÃ TRỐN";
                case FightEventKind.LineSnapped: return "DÂY ĐỨT";
                default: return "ĐÃ CẮT DÂY";
            }
        }
    }
}
