using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class TimingInstrumentationTest : BaseTest
    {
        public TimingInstrumentationTest(ITestOutputHelper output) : base(output) { }

        private class TimingInstrumentation : IDisposable
        {
            public Timing Timing { get; set; }
            public bool Disposed { get; set; }
            public void Dispose() => Disposed = true;

            public TimingInstrumentation(Timing timing) => Timing = timing;
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
