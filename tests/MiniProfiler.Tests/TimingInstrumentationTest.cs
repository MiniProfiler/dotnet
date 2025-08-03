using System;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    public class TimingInstrumentationTest(ITestOutputHelper output) : BaseTest(output)
    {
        private class TimingInstrumentation(Timing timing) : IDisposable
        {
            public Timing Timing { get; set; } = timing;
            public bool Disposed { get; set; }
            public void Dispose() => Disposed = true;
        }

        [Fact]
        public void IsInstrumented()
        {
            TimingInstrumentation? instrumentation = null;
            Timing? timing = null;
            Options.TimingInstrumentationProvider = t => instrumentation = new TimingInstrumentation(t);
            var mp = Options.StartProfiler();

            using (timing = mp.Step("Test timing"))
            {
                Assert.NotNull(instrumentation);
                Assert.False(instrumentation.Disposed);
                mp.Increment();
            }

            Assert.NotNull(instrumentation);
            Assert.Equal(timing, instrumentation.Timing);
            Assert.True(instrumentation.Disposed);
        }
    }
}
