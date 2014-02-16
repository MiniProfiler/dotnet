using NUnit.Framework;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class MiniProfilerTest : BaseTest
    {
        [Test]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
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
        public void StepIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 1)))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5)))
                {
                    IncrementStopwatch(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Test]
        public void StepIf_IncludeChildren()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 5, true)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5, false)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }
        [Test]
        public void DiscardResults()
        {
            using (GetRequest(startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }
    }
}
