using NUnit.Framework;

namespace MvcMiniProfiler.Tests
{
    [TestFixture]
    public class MiniProfilerTest : BaseTest
    {
        [Test]
        public void Simple()
        {
            using (SimulateRequest("http://localhost/Test.aspx"))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.EqualTo(StepTimeMilliseconds));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }
        }

        [Test]
        public void DiscardResults()
        {
            using (SimulateRequest("http://localhost/Test.aspx"))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }

    }
}
