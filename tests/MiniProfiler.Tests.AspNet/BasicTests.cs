using Xunit;

namespace StackExchange.Profiling.Tests
{
    [Collection(NonParallel)]
    public class BasicTests(ITestOutputHelper output) : AspNetTest(output)
    {
        [Fact]
        public void Simple()
        {
            Skip.IfNotWindows();
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();
                Assert.NotNull(mp);
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
            Skip.IfNotWindows();
            using (GetRequest())
            {
                var mp = Options.StartProfiler();
                Assert.NotNull(mp);

                mp.Increment(); // 1 ms
                Timing? goodTiming;
                Timing? badTiming;

                using (goodTiming = mp.StepIf("Yes", 1))
                {
                    mp.Increment(2);
                }
                using (badTiming = mp.StepIf("No", 5))
                {
                    mp.Increment(); // 1 ms
                }
                mp.Stop();

                Assert.NotNull(mp.Root.Children);
                Assert.Contains(goodTiming, mp.Root.Children);
                Assert.DoesNotContain(badTiming, mp.Root.Children);
            }
        }

        [Fact]
        public void StepIf_IncludeChildren()
        {
            Skip.IfNotWindows();
            using (GetRequest())
            {
                var mp = Options.StartProfiler();
                Assert.NotNull(mp);

                mp.Increment(); // 1 ms
                Timing? goodTiming;
                Timing? badTiming;

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

                Assert.NotNull(mp.Root.Children);
                Assert.Contains(goodTiming, mp.Root.Children);
                Assert.DoesNotContain(badTiming, mp.Root.Children);
            }
        }

        [Fact]
        public void CustomTimingIf_Basic()
        {
            Skip.IfNotWindows();
            using (GetRequest())
            {
                var mp = Options.StartProfiler();
                Assert.NotNull(mp);

                mp.Increment(); // 1 ms
                CustomTiming? goodTiming;
                CustomTiming? badTiming;

                using (goodTiming = mp.CustomTimingIf("Cat1", "Yes", 1))
                {
                    mp.Increment(2);
                }
                using (badTiming = mp.CustomTimingIf("Cat1", "No", 5))
                {
                    mp.Increment(); // 1 ms
                }
                mp.Stop();

                Assert.NotNull(mp.Root.CustomTimings);
                Assert.Contains(goodTiming, mp.Root.CustomTimings["Cat1"]);
                Assert.DoesNotContain(badTiming, mp.Root.CustomTimings["Cat1"]);
            }
        }

        [Fact]
        public void DiscardResults()
        {
            Skip.IfNotWindows();
            using (GetRequest(startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();
                Assert.NotNull(mp);
                mp.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.Null(c);
            }
        }

        [Fact]
        public void GetProfiler_NoChildren()
        {
            Skip.IfNotWindows();
            // this won't create any child steps
            var mp = GetProfiler();

            // and shouldn't have any duration
            Assert.Equal(0, mp.DurationMilliseconds);
            Assert.False(mp.Root.HasChildren);
        }

        [Fact]
        public void GetProfiler_Children()
        {
            Skip.IfNotWindows();
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
            Skip.IfNotWindows();
            MiniProfiler? mp;
            using (GetRequest())
            {
                mp = MiniProfiler.Current;
                Assert.NotNull(mp);
                mp.Increment();
            }

            Assert.Equal(1, mp.DurationMilliseconds);
            Assert.False(mp.GetStopwatch().IsRunning);
        }
    }
}
