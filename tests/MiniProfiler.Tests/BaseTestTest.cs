using StackExchange.Profiling;
using Xunit;

namespace Tests
{
    public class BaseTestTest : BaseTest
    {
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
                IncrementStopwatch();
                mp = MiniProfiler.Current;
            }

            Assert.Equal(1, mp.DurationMilliseconds);
            Assert.False(mp.Stopwatch.IsRunning);
        }
    }
}
