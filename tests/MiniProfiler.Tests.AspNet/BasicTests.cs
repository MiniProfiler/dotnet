using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class BasicTests : AspNetTest
    {
        public BasicTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();
                mp.Increment(); // 1 ms
                mp.Stop();

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
                var mp = Options.StartProfiler();

                mp.Increment(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = mp.StepIf("Yes", 1))
                {
                    mp.Increment(2);
                }
                using (badTiming = mp.StepIf("No", 5))
                {
                    mp.Increment(); // 1 ms
                }
                mp.Stop();

                Assert.Contains(goodTiming, mp.Root.Children);
                Assert.DoesNotContain(badTiming, mp.Root.Children);
            }
        }

        [Fact]
        public void StepIf_IncludeChildren()
        {
            using (GetRequest())
            {
                var mp = Options.StartProfiler();

                mp.Increment(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = mp.StepIf("Yes", 5, true))
                {
                    mp.Increment(2);
                    using (mp.Step("#1"))
                    {
                        mp.Increment(2);
                    }
                    using (mp.Step("#2"))
                    {
                        mp.Increment(2);
                    }
                }
                using (badTiming = mp.StepIf("No", 5, false))
                {
                    mp.Increment(2);
                    using (mp.Step("#1"))
                    {
                        mp.Increment(2);
                    }
                    using (mp.Step("#2"))
                    {
                        mp.Increment(2);
                    }
                }
                mp.Stop();

                Assert.Contains(goodTiming, mp.Root.Children);
                Assert.DoesNotContain(badTiming, mp.Root.Children);
            }
        }

        [Fact]
        public void CustomTimingIf_Basic()
        {
            using (GetRequest())
            {
                var mp = Options.StartProfiler();

                mp.Increment(); // 1 ms
                CustomTiming goodTiming;
                CustomTiming badTiming;

                using (goodTiming = mp.CustomTimingIf("Cat1", "Yes", 1))
                {
                    mp.Increment(2);
                }
                using (badTiming = mp.CustomTimingIf("Cat1", "No", 5))
                {
                    mp.Increment(); // 1 ms
                }
                mp.Stop();

                Assert.Contains(goodTiming, mp.Root.CustomTimings["Cat1"]);
                Assert.DoesNotContain(badTiming, mp.Root.CustomTimings["Cat1"]);
            }
        }

        [Fact]
        public void DiscardResults()
        {
            using (GetRequest(startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();
                mp.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.Null(c);
            }
        }

        [Fact]
        public void GetProfiler_NoChildren()
        {
            // this won't create any child steps
            var mp = GetProfiler();

            // and shouldn't have any duration
            Assert.Equal(0, mp.DurationMilliseconds);
            Assert.False(mp.Root.HasChildren);
        }

        [Fact]
        public void GetProfiler_Children()
        {
            const int depth = 5;

            var mp = GetProfiler(childDepth: depth);

            Assert.Equal(depth, mp.DurationMilliseconds);
            Assert.True(mp.Root.HasChildren);

            var children = 0;
            foreach (var t in mp.GetTimingHierarchy())
            {
                if (t != mp.Root)
                    children++;
            }

            Assert.Equal(depth, children);
        }

        [Fact]
        public void GetRequest_StartAndStopProfiler()
        {
            MiniProfiler mp;
            using (GetRequest())
            {
                mp = MiniProfiler.Current;
                mp.Increment();
            }

            Assert.Equal(1, mp.DurationMilliseconds);
            Assert.False(mp.GetStopwatch().IsRunning);
        }
    }
}
