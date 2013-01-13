using NUnit.Framework;

namespace StackExchange.Profiling.Tests
{
    /// <summary>
    /// The mini profiler test.
    /// </summary>
    [TestFixture]
    public class MiniProfilerTest : BaseTest
    {
        /// <summary>
        /// simple test.
        /// </summary>
        [Test]
        public void Simple()
        {
            using (BaseTest.GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                BaseTest.IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.EqualTo(BaseTest.StepTimeMilliseconds));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }
        }

        /// <summary>
        /// discard the results.
        /// </summary>
        [Test]
        public void DiscardResults()
        {
            using (BaseTest.GetRequest(startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }
    }
}
