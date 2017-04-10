using StackExchange.Profiling;
using System;
using Xunit;

namespace Tests
{
    [Collection("MiniProfiler")] // using that lib's storage provider
    public class MiniProfilerTests : BaseTest
    {
        [Fact]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                Increment(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.NotNull(c);
                Assert.Equal(StepTimeMilliseconds, c.DurationMilliseconds);

                Assert.NotNull(c.Root);
                Assert.False(c.Root.HasChildren);
            }
        }

        [Fact]
        public void StepIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                Increment(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 1)))
                {
                    Increment(2);
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5)))
                {
                    Increment(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.True(mp1.Root.Children.Contains(goodTiming));
                Assert.True(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Fact]
        public void StepIf_IncludeChildren()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                Increment(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 5, true)))
                {
                    Increment(2);
                    using (mp1.Step("#1"))
                    {
                        Increment(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        Increment(2);
                    }
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5, false)))
                {
                    Increment(2);
                    using (mp1.Step("#1"))
                    {
                        Increment(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        Increment(2);
                    }
                }
                MiniProfiler.Stop();

                Assert.True(mp1.Root.Children.Contains(goodTiming));
                Assert.True(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Fact]
        public void CustomTimingIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                Increment(); // 1 ms
                CustomTiming goodTiming;
                CustomTiming badTiming;

                using (goodTiming = mp1.CustomTimingIf("Cat1", "Yes", 1))
                {
                    Increment(2);
                }
                using (badTiming = mp1.CustomTimingIf("Cat1", "No", 5))
                {
                    Increment(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.True(mp1.Root.CustomTimings["Cat1"].Contains(goodTiming));
                Assert.True(!mp1.Root.CustomTimings["Cat1"].Contains(badTiming));
            }
        }

        [Fact]
        public void DiscardResults()
        {
            using (GetRequest(startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.Null(c);
            }
        }
    }
}
