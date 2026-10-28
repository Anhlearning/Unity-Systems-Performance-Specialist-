using System;
using Cysharp.Threading.Tasks;
using Ignite.Core.Logging;
using Ignite.Core.Save;
using Ignite.Core.Settings;
using Ignite.Unity.Assets;
using Ignite.Unity.Audio;
using Ignite.Unity.Settings;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ignite.Unity.Tests
{
    public class AssetServiceTests
    {
        sealed class FakeBackend : IAssetBackend
        {
            public int Loads;
            public int Releases;
            public UniTaskCompletionSource<Object> Pending;
            public Object Asset;
            public bool Fail;

            public AssetRequest Load<T>(string key) where T : Object
            {
                Loads++;
                if (Fail)
                    return new AssetRequest(UniTask.FromException<Object>(new InvalidOperationException("không có")), key);
                if (Pending != null)
                    return new AssetRequest(Pending.Task, key);
                return new AssetRequest(UniTask.FromResult(Asset), key);
            }

            public void Release(AssetRequest request) => Releases++;
        }

        Texture2D _texture;
        ILogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(2, 2);
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_texture);
            GameLog.Sink = _sink;
        }

        [Test]
        public void SameKeyTwice_LoadsOnce_ReleasesAfterLastOwner()
        {
            var backend = new FakeBackend { Asset = _texture };
            var assets = new AssetService(backend);

            var a = assets.LoadAsync<Texture2D>("icon").GetAwaiter().GetResult();
            var b = assets.LoadAsync<Texture2D>("icon").GetAwaiter().GetResult();

            Assert.That(a, Is.SameAs(b));
            Assert.That(backend.Loads, Is.EqualTo(1));
            Assert.That(assets.RefCount<Texture2D>("icon"), Is.EqualTo(2));

            assets.Release<Texture2D>("icon");
            Assert.That(backend.Releases, Is.EqualTo(0), "Còn một bên đang dùng.");
            assets.Release<Texture2D>("icon");
            Assert.That(backend.Releases, Is.EqualTo(1));
            Assert.That(assets.LoadedCount, Is.EqualTo(0));
        }

        [Test]
        public void ConcurrentLoads_ShareOneRequest()
        {
            var backend = new FakeBackend { Pending = new UniTaskCompletionSource<Object>() };
            var assets = new AssetService(backend);

            var first = assets.LoadAsync<Texture2D>("icon");
            var second = assets.LoadAsync<Texture2D>("icon");
            backend.Pending.TrySetResult(_texture);

            Assert.That(backend.Loads, Is.EqualTo(1));
            Assert.That(first.GetAwaiter().GetResult(), Is.SameAs(_texture));
            Assert.That(second.GetAwaiter().GetResult(), Is.SameAs(_texture));
        }

        [Test]
        public void FailedLoad_DoesNotLeakReference()
        {
            var backend = new FakeBackend { Fail = true };
            var assets = new AssetService(backend);

            Assert.Throws<InvalidOperationException>(() => assets.LoadAsync<Texture2D>("missing").GetAwaiter().GetResult());
            Assert.That(assets.LoadedCount, Is.EqualTo(0));
            Assert.That(backend.Releases, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ReleasesLeftovers()
        {
            var backend = new FakeBackend { Asset = _texture };
            var assets = new AssetService(backend);
            assets.LoadAsync<Texture2D>("icon").GetAwaiter().GetResult();

            assets.Dispose();

            Assert.That(backend.Releases, Is.EqualTo(1));
        }
    }

    public class AudioServiceTests
    {
        AudioLibrary _library;
        AudioClip _clip;
        AudioService _audio;
        ILogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _sink = GameLog.Sink;
            GameLog.Sink = NullLogSink.Instance;
            _clip = AudioClip.Create("beep", 4410, 1, 44100, false);
            _library = ScriptableObject.CreateInstance<AudioLibrary>();
            _library.EditorSetEntries(new[]
            {
                new AudioLibrary.Entry { id = "sfx.hit", clips = new[] { _clip }, bus = AudioBus.Sfx, volume = 0.8f },
                new AudioLibrary.Entry { id = "music", clips = new[] { _clip }, bus = AudioBus.Music, volume = 1f, loop = true },
            });
            _audio = new AudioService(null, _library);
        }

        [TearDown]
        public void TearDown()
        {
            _audio.Dispose();
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_clip);
            GameLog.Sink = _sink;
        }

        [Test]
        public void Play_KnownId_UsesPooledSource_StopReturnsIt()
        {
            int pooled = _audio.PooledCount;

            Assert.That(_audio.Play("sfx.hit"), Is.True);
            Assert.That(_audio.Play("sfx.hit"), Is.True);
            Assert.That(_audio.PlayingCount, Is.EqualTo(2));
            Assert.That(_audio.PooledCount, Is.EqualTo(pooled - 2));

            _audio.StopAll();
            Assert.That(_audio.PlayingCount, Is.EqualTo(0));
            Assert.That(_audio.PooledCount, Is.EqualTo(pooled), "Source được trả về pool, không tạo mới.");
        }

        [Test]
        public void Play_UnknownId_ReturnsFalse()
        {
            Assert.That(_audio.Play("nope"), Is.False);
        }

        [Test]
        public void BusVolume_MultipliesWithMaster_WhenNoMixer()
        {
            _library.TryGet("sfx.hit", out var hit);
            _audio.SetBusVolume(AudioBus.Master, 0.5f);
            _audio.SetBusVolume(AudioBus.Sfx, 0.5f);

            Assert.That(_audio.EffectiveVolume(hit), Is.EqualTo(0.8f * 0.5f * 0.5f).Within(1e-5f));
        }

        [Test]
        public void LinearToDecibels_MatchesMixerScale()
        {
            Assert.That(AudioService.LinearToDecibels(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(AudioService.LinearToDecibels(0.5f), Is.EqualTo(-6.02f).Within(0.01f));
            Assert.That(AudioService.LinearToDecibels(0f), Is.EqualTo(-80f));
        }
    }

    public class SettingsServiceTests
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

        [Test]
        public void SavedVolumes_AreAppliedToAudioBusesOnStartup()
        {
            var storage = new MemorySaveStorage();
            new SettingsStore(storage).Apply(s => { s.MasterVolume = 0.7f; s.MusicVolume = 0.2f; s.UiVolume = 0.4f; });

            var audio = new AudioService();
            try
            {
                using var service = new SettingsService(new SettingsStore(storage), audio, null);

                Assert.That(audio.GetBusVolume(AudioBus.Master), Is.EqualTo(0.7f));
                Assert.That(audio.GetBusVolume(AudioBus.Music), Is.EqualTo(0.2f));
                Assert.That(audio.GetBusVolume(AudioBus.UI), Is.EqualTo(0.4f));
            }
            finally
            {
                audio.Dispose();
            }
        }

        [Test]
        public void ChangingLanguage_RaisesEventOnce()
        {
            var audio = new AudioService();
            try
            {
                using var service = new SettingsService(new SettingsStore(new MemorySaveStorage()), audio, null);
                string language = null;
                int raised = 0;
                service.LanguageChanged += l => { language = l; raised++; };

                service.Change(s => s.Language = "en");
                service.Change(s => s.MasterVolume = 0.5f);

                Assert.That(language, Is.EqualTo("en"));
                Assert.That(raised, Is.EqualTo(1));
            }
            finally
            {
                audio.Dispose();
            }
        }
    }
}
