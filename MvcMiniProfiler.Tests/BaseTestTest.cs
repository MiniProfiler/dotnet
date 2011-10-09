using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MvcMiniProfiler.Tests
{
    [TestClass]
    public class BaseTestTest : BaseTest
    {
        [TestMethod]
        public void GetProfiler_NoChildren()
        {
            // this won't create any child steps
            var mp = GetProfiler();

            // and shouldn't have any duration
            Assert.That(mp.DurationMilliseconds, Is.EqualTo(0));
            Assert.That(mp.Root.HasChildren, Is.False);
        }

        [TestMethod]
        public void GetProfiler_Children()
        {
            var depth = 5;

            var mp = GetProfiler(childDepth: depth);

            Assert.That(mp.DurationMilliseconds, Is.EqualTo(depth));
            Assert.That(mp.Root.HasChildren, Is.True);

            var children = 0;
            foreach (var t in mp.GetTimingHierarchy())
            {
                if (t != mp.Root)
                    children++;
            }

            Assert.That(children, Is.EqualTo(depth));
        }

    }
}
