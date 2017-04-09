using BenchmarkDotNet.Attributes;
using StackExchange.Profiling;

namespace Benchmarks
{
    [Config(typeof(Configs.Full))]
    public class CustomTimingBenchmarks
    {
        private MiniProfiler Profiler;

        [Setup]
        public void SetupData()
        {
            Profiler = new MiniProfiler("Test");
        }

        [Benchmark(Description = "Creation of a standalone CustomTiming")]
        public CustomTiming Creation() => new CustomTiming(Profiler, "Test");

        [Benchmark(Description = "Creation a CustomTiming via MiniProfiler")]
        public void AddingToMiniProfiler()
        {
            Profiler.CustomTiming("Test", "MyCategory");
        }

        [Benchmark(Description = "Using a CustomTiming with MiniProfiler")]
        public void UsingWithMiniProfiler()
        {
            using (Profiler.CustomTiming("Test", "MyCategory"))
            {
                // Trigger the .Dispose()
            }
        }
    }
}
