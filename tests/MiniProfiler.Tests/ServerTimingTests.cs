using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class ServerTimingTests : BaseTest
    {
        public ServerTimingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        [Obsolete("Still awaiting browser support")]
        public void ServerTimingFormat()
        {
            var mp = new MiniProfiler("Test", Options);
            using (mp.Step("Main"))
            {
                using (mp.Step("Sub Step 1"))
                {
                    mp.Head.AddCustomTiming("A", new CustomTiming()
                    {
                        DurationMilliseconds = 5
                    });
                }
                using (mp.Step("Sub Step 2"))
                {
                    mp.Head.AddCustomTiming("A", new CustomTiming()
                    {
                        DurationMilliseconds = 10.1m
                    });
                    mp.Head.AddCustomTiming("B", new CustomTiming()
                    {
                        DurationMilliseconds = 8.2345m
                    });
                }
            }
            mp.Stop();
            mp.DurationMilliseconds = 5m + 10.1m + 8.2345m; // Since we're synthetic here, need to set it
            var st = mp.GetServerTimingHeader();
            Assert.Equal(@"A;desc=""A"";dur=15.1,B;desc=""B"";dur=8.23,total;desc=""Total"";dur=23.33", st);
        }
    }
}
