using System;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    [Collection("MiniProfiler")] // using that lib's storage provider
    public class MiniProfilerTest : BaseTest
    {
        public MiniProfilerTest()
        {
            MiniProfiler.Settings.ProfilerProvider = new WebRequestProfilerProvider();
            MiniProfiler.Settings.Storage = new Profiling.Storage.HttpRuntimeCacheStorage(TimeSpan.FromDays(1));
        }

        [Fact]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.NotNull(c);
                Assert.Equal(StepTimeMilliseconds, c.DurationMilliseconds);
                Assert.Equal("/Test.aspx", c.Name);

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

                IncrementStopwatch(); // 1 ms
                CustomTiming goodTiming;
                CustomTiming badTiming;

                using (goodTiming = mp1.CustomTimingIf("Cat1", "Yes", 1))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = mp1.CustomTimingIf("Cat1", "No", 5))
                {
                    IncrementStopwatch(); // 1 ms
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
