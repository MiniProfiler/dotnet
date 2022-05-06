using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472, invocationCount: 50_000)]
    [SimpleJob(RuntimeMoniker.Net50, invocationCount: 50_000)]
    [Config(typeof(Configs.Memory))]
    public class CustomTimingBenchmarks
    {
        private MiniProfiler Profiler;

        [IterationSetup]
        public void SetupData()
        {
            Profiler = new MiniProfiler("Test", new MiniProfilerBenchmarkOptions());
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
