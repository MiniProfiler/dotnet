using Xunit;

namespace StackExchange.Profiling.Tests
{
    public class BaseTestTest : BaseTest
    {
        [Fact]
        public void GetProfiler_NoChildren()
        {
            // this won't create any child steps
            var mp = GetProfiler();

            // and shouldn't have any duration
            Assert.Equal(mp.DurationMilliseconds, 0);
            Assert.False(mp.Root.HasChildren);
        }

        [Fact]
        public void GetProfiler_Children()
        {
            var depth = 5;

            var mp = GetProfiler(childDepth: depth);

            Assert.Equal(mp.DurationMilliseconds, depth);
            Assert.True(mp.Root.HasChildren);

            var children = 0;
            foreach (var t in mp.GetTimingHierarchy())
            {
                if (t != mp.Root)
                    children++;
            }

            Assert.Equal(children, depth);
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

            Assert.Equal(mp.DurationMilliseconds, 1);
            Assert.False(mp.Stopwatch.IsRunning);
        }
    }
}
