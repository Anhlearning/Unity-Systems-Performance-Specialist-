using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Ecs;
using NUnit.Framework;
using Scellecs.Morpeh;
using VContainer;

namespace IgniteSample.Gameplay.Tests
{
    /// <summary>Buổi 32: module ECS lưu tiến trình, World bị huỷ rồi tạo lại (rời rồi vào lại Gameplay) vẫn còn.</summary>
    public class FightSaveTests
    {
        ILogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
        }

        [TearDown]
        public void TearDown() => GameLog.Sink = _sink;

        static (EcsWorldHost host, IObjectResolver resolver) CreateHost(SaveService save)
        {
            var module = new FightModule();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(save);
            module.Install(builder);
            var resolver = builder.Build();
            return (new EcsWorldHost(new IGameModule[] { module }, resolver), resolver);
        }

        static void CatchOneBeast(EcsWorldHost host, IObjectResolver resolver)
        {
            resolver.Resolve<CommandQueue<StartFightCommand>>().Enqueue(
                new StartFightCommand(new BeastStats { MaxStamina = 1f, Drain = 1f, Mass = 1f, Phase = 1 }, HookSpot.Body));
            var input = resolver.Resolve<CommandQueue<FightInputCommand>>();
            for (int i = 0; i < 40; i++)
            {
                input.Enqueue(new FightInputCommand(0f, false, haul: true, false));
                host.Step(0.1f);
            }
        }

        static int Caught(EcsWorldHost host) => FightProgressAccess.GetOrCreate(host.World).Caught;

        [Test]
        public void CaughtCount_SurvivesWorldRecreation_AndRestart()
        {
            var storage = new MemorySaveStorage();
            var save = new SaveService(storage);

            var (host, resolver) = CreateHost(save);
            CatchOneBeast(host, resolver);
            Assert.That(Caught(host), Is.EqualTo(1));
            host.Dispose(); // về Menu: World bị huỷ, module chụp dữ liệu lần cuối

            save.Save("auto"); // tự lưu ở Menu, lúc World không còn

            // Khởi động lại game: SaveService mới tải file, rồi vào Gameplay tạo World mới.
            var restartedSave = new SaveService(storage);
            restartedSave.Load("auto");
            var (again, againResolver) = CreateHost(restartedSave);
            try
            {
                Assert.That(Caught(again), Is.EqualTo(1));
                CatchOneBeast(again, againResolver);
                Assert.That(Caught(again), Is.EqualTo(2));
            }
            finally
            {
                again.Dispose();
            }
        }

        [Test]
        public void FinishedFight_IsCountedOnlyOnce()
        {
            var (host, resolver) = CreateHost(new SaveService(new MemorySaveStorage()));
            try
            {
                CatchOneBeast(host, resolver);
                for (int i = 0; i < 20; i++)
                    host.Step(0.1f);

                Assert.That(Caught(host), Is.EqualTo(1));
            }
            finally
            {
                host.Dispose();
            }
        }
    }
}
