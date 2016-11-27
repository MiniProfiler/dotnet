using NUnit.Framework;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class BaseTestTest : BaseTest
    {
        [Test]
        public void GetProfiler_NoChildren()
        {
            // this won't create any child steps
            var mp = GetProfiler();

            // and shouldn't have any duration
            Assert.That(mp.DurationMilliseconds, Is.EqualTo(0));
            Assert.That(mp.Root.HasChildren, Is.False);
        }

        [Test]
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

        [Test]
        public void GetRequest_StartAndStopProfiler()
        {
            MiniProfiler mp;
            using (GetRequest())
            {
                IncrementStopwatch();
                mp = MiniProfiler.Current;
            }

            Assert.That(mp.DurationMilliseconds == 1);
            Assert.That(mp.Stopwatch.IsRunning, Is.False);
        }
    }
}
