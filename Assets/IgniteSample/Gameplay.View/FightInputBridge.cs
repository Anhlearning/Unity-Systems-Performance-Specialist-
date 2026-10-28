using Ignite.Ecs;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace IgniteSample.Gameplay.View
{
    /// <summary>
    /// Thay cho FightBridge của Project B, nhưng không tự tạo World nữa: chỉ đọc phím và gửi command.
    /// Trong game thật, RocksCrossed/RockHit/SpeedRatio do physics tính; ở sample dùng phím để giả lập.
    /// Space: móc quái · A/D: bẻ lái · chuột trái: ghì cần · F: thu dây · C: cắt dây · R: quái tông đá · T: bật/tắt dây cắt qua đá.
    /// </summary>
    public sealed class FightInputBridge : MonoBehaviour
    {
        CommandQueue<StartFightCommand> _start;
        CommandQueue<FightInputCommand> _input;
        CommandQueue<FightSenseCommand> _sense;
        bool _lineAcrossRock;

        [Inject]
        void Construct(CommandQueue<StartFightCommand> start, CommandQueue<FightInputCommand> input, CommandQueue<FightSenseCommand> sense)
        {
            _start = start;
            _input = input;
            _sense = sense;
        }

        /// <summary>Gọi từ test/tool để bắt đầu trận mà không cần bấm phím.</summary>
        public void StartFight(HookSpot spot = HookSpot.Body) => _start.Enqueue(new StartFightCommand(BeastPresets.Serpent, spot));

        void Update()
        {
            var keyboard = Keyboard.current;
            if (_input == null || keyboard == null)
                return;

            if (keyboard.spaceKey.wasPressedThisFrame)
                StartFight(keyboard.leftShiftKey.isPressed ? HookSpot.Head : HookSpot.Body);
            if (keyboard.tKey.wasPressedThisFrame)
                _lineAcrossRock = !_lineAcrossRock;

            float steer = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            bool pull = Mouse.current != null && Mouse.current.leftButton.isPressed;
            _input.Enqueue(new FightInputCommand(steer, pull, keyboard.fKey.isPressed, keyboard.cKey.isPressed));
            _sense.Enqueue(new FightSenseCommand(_lineAcrossRock ? 1 : 0, 1f, keyboard.rKey.wasPressedThisFrame));
        }
    }
}
