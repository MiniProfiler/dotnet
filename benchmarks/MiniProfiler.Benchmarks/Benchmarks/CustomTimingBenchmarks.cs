using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [Config(typeof(Configs.Memory))]
    public class CustomTimingBenchmarks
    {
        private MiniProfiler Profiler;

        [GlobalSetup]
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
