using System.Linq;
using NUnit.Framework;

namespace Ignite.Core.Tests
{
    public class CoreBoundaryTests
    {
        // Cổng Giai đoạn 0: Core không được biết Unity. asmdef đã chặn lúc compile, test này chặn thêm lúc chạy.
        [Test]
        public void Core_DoesNotReferenceUnityAssemblies()
        {
            var unityRefs = typeof(FrameworkInfo).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

            Assert.That(unityRefs, Is.Empty);
        }

        [Test]
        public void Core_DoesNotReferenceOtherFrameworkPackages()
        {
            var frameworkRefs = typeof(FrameworkInfo).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("Ignite."))
                .ToArray();

            Assert.That(frameworkRefs, Is.Empty);
        }
    }
}
