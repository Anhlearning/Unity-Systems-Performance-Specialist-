using System.Collections.Generic;
using Ignite.Ecs;
using NUnit.Framework;
using Scellecs.Morpeh;
using VContainer;

namespace IgniteSample.Gameplay.Tests
{
    /// <summary>
    /// Bốn kịch bản của trận giằng co (cổng Giai đoạn 2): bắt được, dây đứt, quái trốn sau hiệp cuồng nộ, cắt dây.
    /// Chạy World thật qua EcsWorldHost, chỉ giao tiếp bằng command/event như game thật.
    /// </summary>
    public class FightModuleTests
    {
        const float Dt = 0.1f;

        EcsWorldHost _host;
        CommandQueue<StartFightCommand> _start;
        CommandQueue<FightInputCommand> _input;
        CommandQueue<FightSenseCommand> _sense;
        BridgeQueue<FightEvent>.Reader _events;
        readonly List<FightEventKind> _seen = new List<FightEventKind>();

        // Quái nhỏ để test chạy nhanh: 10 thể lực, drain 1/s ở tốc độ 1 → (0.5 + 1²) = 1.5/s.
        static BeastStats SmallBeast => new BeastStats { MaxStamina = 10f, Drain = 1f, Mass = 1f, Phase = 1 };

        [SetUp]
        public void SetUp()
        {
            var module = new FightModule();
            var builder = new ContainerBuilder();
            module.Install(builder);
            var resolver = builder.Build();

            _host = new EcsWorldHost(new IGameModule[] { module }, resolver);
            _start = resolver.Resolve<CommandQueue<StartFightCommand>>();
            _input = resolver.Resolve<CommandQueue<FightInputCommand>>();
            _sense = resolver.Resolve<CommandQueue<FightSenseCommand>>();
            _events = resolver.Resolve<EventQueue<FightEvent>>().Claim("Test");
            _seen.Clear();
        }

        [TearDown]
        public void TearDown() => _host.Dispose();

        Entity StartFight(HookSpot spot = HookSpot.Body)
        {
            _start.Enqueue(new StartFightCommand(SmallBeast, spot));
            Step();
            Assert.That(_seen, Does.Contain(FightEventKind.Started));
            return LastFight;
        }

        Entity LastFight { get; set; }

        void Step(FightInputCommand input = default, int rocks = 0, bool rockHit = false, float speed = 1f)
        {
            _input.Enqueue(input);
            _sense.Enqueue(new FightSenseCommand(rocks, speed, rockHit));
            _host.Step(Dt);
            while (_events.TryRead(out var evt))
            {
                _seen.Add(evt.Kind);
                LastFight = evt.Fight;
            }
        }

        void StepFor(float seconds, FightInputCommand input = default, int rocks = 0)
        {
            for (float t = 0f; t < seconds; t += Dt)
                Step(input, rocks);
        }

        bool Has<T>(Entity fight) where T : struct, IComponent => _host.World.GetStash<T>().Has(fight);
        T Get<T>(Entity fight) where T : struct, IComponent => _host.World.GetStash<T>().Get(fight);

        FightOutcome Outcome(Entity fight)
        {
            Assert.That(Has<FightOver>(fight), Is.True, "Trận chưa kết thúc.");
            return Get<FightOver>(fight).Outcome;
        }

        // ---------- Bốn kịch bản ----------

        [Test]
        public void Scenario1_DrainToExhausted_ThenHaul_CatchesBeast()
        {
            var fight = StartFight();

            StepFor(7f); // 10 thể lực / 1.5 mỗi giây ≈ 6.7 s
            Assert.That(Has<Exhausted>(fight), Is.True);
            Assert.That(Has<Hooked>(fight), Is.False);

            StepFor(1.3f, new FightInputCommand(0f, false, haul: true, false)); // giữ F đủ 1.2 s

            Assert.That(Outcome(fight), Is.EqualTo(FightOutcome.Caught));
            Assert.That(_seen, Is.EqualTo(new[] { FightEventKind.Started, FightEventKind.Exhausted, FightEventKind.Caught }));
        }

        [Test]
        public void Scenario2_LineAcrossRocksTooLong_Snaps()
        {
            var fight = StartFight();

            // 1 tảng đá, tốc độ 1: tích luỹ 1.5 mỗi giây → ngưỡng 2.0 sau khoảng 1.4 s.
            StepFor(1.5f, rocks: 1);

            Assert.That(Outcome(fight), Is.EqualTo(FightOutcome.LineSnapped));
            Assert.That(Has<Hooked>(fight), Is.False);
        }

        [Test]
        public void Scenario3_MissBothExhaustWindows_EnragesThenEscapes()
        {
            var fight = StartFight();

            StepFor(7f);   // kiệt sức lần 1
            StepFor(7.2f); // không thu dây → hết cửa sổ 7 s → hồi sức
            Assert.That(Has<Recovering>(fight), Is.True);

            // Hồi sức tối đa 2.2 s; dừng đúng frame vừa sang hiệp 2 để đo thể lực.
            for (int i = 0; i < 30 && !Has<Hooked>(fight); i++)
                Step();
            Assert.That(Has<Hooked>(fight), Is.True);
            Assert.That(Get<BeastStats>(fight).Phase, Is.EqualTo(2));
            Assert.That(Get<BeastStamina>(fight).Current, Is.EqualTo(10f).Within(0.2f), "Hiệp cuồng nộ bắt đầu với thể lực đầy.");

            StepFor(7f);   // kiệt sức lần 2
            StepFor(7.2f); // lại không thu dây → trốn

            Assert.That(Outcome(fight), Is.EqualTo(FightOutcome.Escaped));
            Assert.That(_seen, Is.EqualTo(new[]
            {
                FightEventKind.Started, FightEventKind.Exhausted, FightEventKind.Recovering,
                FightEventKind.Enraged, FightEventKind.Exhausted, FightEventKind.Escaped,
            }));
        }

        [Test]
        public void Scenario4_HoldCutForHalfSecond_CutsLine()
        {
            var fight = StartFight();
            var cut = new FightInputCommand(0f, false, false, cut: true);

            StepFor(0.3f, cut);
            Assert.That(Has<FightOver>(fight), Is.False, "Chưa giữ đủ 0.5 s.");

            Step(); // nhả C → reset
            StepFor(0.6f, cut);

            Assert.That(Outcome(fight), Is.EqualTo(FightOutcome.LineCut));
        }

        // ---------- Luật phụ ----------

        [Test]
        public void RockHit_Takes8PercentStamina_ButNeverExhausts()
        {
            var fight = StartFight();
            float before = Get<BeastStamina>(fight).Current;

            Step(rockHit: true, speed: 0f);

            float lost = before - Get<BeastStamina>(fight).Current;
            Assert.That(lost, Is.EqualTo(0.8f + 0.05f).Within(0.01f)); // 8% của 10 + drain 0.5×0.1 ở tốc độ 0
            Assert.That(_seen, Does.Contain(FightEventKind.RockHit));
        }

        [Test]
        public void HeadHook_DrainsFasterThanBodyHook()
        {
            var body = StartFight(HookSpot.Body);
            StepFor(2f);
            float bodyLeft = Get<BeastStamina>(body).Current;

            var head = StartFight(HookSpot.Head);
            StepFor(2f);
            float headLeft = Get<BeastStamina>(head).Current;

            Assert.That(headLeft, Is.LessThan(bodyLeft - 0.1f));
        }

        [Test]
        public void SnagDecays_WhenLineClearsRocks()
        {
            var fight = StartFight();
            StepFor(0.8f, rocks: 1);
            float snagged = Get<FightLine>(fight).Snag;

            StepFor(0.5f);

            Assert.That(Get<FightLine>(fight).Snag, Is.LessThan(snagged));
            Assert.That(Has<FightOver>(fight), Is.False);
        }

        [Test]
        public void EachFightHasExactlyOneStateTag()
        {
            var fight = StartFight();
            AssertOneTag(fight);
            StepFor(7f);
            AssertOneTag(fight);
            StepFor(7.2f);
            AssertOneTag(fight);
        }

        void AssertOneTag(Entity fight)
        {
            int tags = (Has<Hooked>(fight) ? 1 : 0) + (Has<Exhausted>(fight) ? 1 : 0)
                     + (Has<Recovering>(fight) ? 1 : 0) + (Has<FightOver>(fight) ? 1 : 0);
            Assert.That(tags, Is.EqualTo(1));
        }
    }

    public class HitDemoModuleTests
    {
        [Test]
        public void HitCommand_ReducesHealth_AndPublishesDamaged()
        {
            var module = new HitDemoModule();
            var builder = new ContainerBuilder();
            module.Install(builder);
            var resolver = builder.Build();
            var host = new EcsWorldHost(new IGameModule[] { module }, resolver);
            try
            {
                var target = host.World.CreateEntity();
                host.World.GetStash<Health>().Set(target, new Health { Current = 10f, Max = 10f });
                host.World.Commit();
                var events = resolver.Resolve<EventQueue<DamagedEvent>>().Claim("Test");

                resolver.Resolve<CommandQueue<HitCommand>>().Enqueue(new HitCommand(target, 3f));
                host.Step(0.016f);

                Assert.That(host.World.GetStash<Health>().Get(target).Current, Is.EqualTo(7f));
                Assert.That(events.TryRead(out var damaged), Is.True);
                Assert.That(damaged.Remaining, Is.EqualTo(7f));
                Assert.That(damaged.Target, Is.EqualTo(target));
            }
            finally
            {
                host.Dispose();
            }
        }
    }
}
