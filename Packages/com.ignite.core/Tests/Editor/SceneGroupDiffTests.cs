using System.Collections.Generic;
using Ignite.Core.Scenes;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class SceneGroupDiffTests
    {
        readonly List<string> _keep = new List<string>();
        readonly List<string> _unload = new List<string>();
        readonly List<string> _load = new List<string>();

        void Diff(string[] current, string[] target) => SceneGroupDiff.Compute(current, target, _keep, _unload, _load);

        [Test]
        public void MenuToGameplay_KeepsSharedUi()
        {
            Diff(new[] { "Menu", "SharedUI" }, new[] { "Gameplay", "SharedUI", "HUD" });

            Assert.That(_keep, Is.EqualTo(new[] { "SharedUI" }));
            Assert.That(_unload, Is.EqualTo(new[] { "Menu" }));
            Assert.That(_load, Is.EqualTo(new[] { "Gameplay", "HUD" }));
        }

        [Test]
        public void FromNothing_LoadsEverything()
        {
            Diff(new string[0], new[] { "Menu", "SharedUI" });

            Assert.That(_keep, Is.Empty);
            Assert.That(_unload, Is.Empty);
            Assert.That(_load, Is.EqualTo(new[] { "Menu", "SharedUI" }));
        }

        [Test]
        public void SameGroup_KeepsAll_LoadsNothing()
        {
            Diff(new[] { "A", "B" }, new[] { "B", "A" });

            Assert.That(_keep, Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(_unload, Is.Empty);
            Assert.That(_load, Is.Empty);
        }

        [Test]
        public void DisjointGroups_UnloadAll_LoadAll()
        {
            Diff(new[] { "A", "B" }, new[] { "C" });

            Assert.That(_keep, Is.Empty);
            Assert.That(_unload, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(_load, Is.EqualTo(new[] { "C" }));
        }

        [Test]
        public void DuplicateEntries_AreReportedOnce()
        {
            Diff(new[] { "A", "A" }, new[] { "B", "B" });

            Assert.That(_unload, Is.EqualTo(new[] { "A" }));
            Assert.That(_load, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public void Compute_ClearsResultListsBetweenCalls()
        {
            Diff(new[] { "A" }, new[] { "B" });
            Diff(new[] { "B" }, new[] { "B" });

            Assert.That(_keep, Is.EqualTo(new[] { "B" }));
            Assert.That(_unload, Is.Empty);
            Assert.That(_load, Is.Empty);
        }
    }
}
