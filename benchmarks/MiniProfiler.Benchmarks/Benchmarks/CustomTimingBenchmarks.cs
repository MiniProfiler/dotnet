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

        [Params(true, false)]
        public bool IncludeStackTrace { get; set; }

        [IterationSetup]
        public void SetupData()
        {
            Profiler = new MiniProfiler("Test", new MiniProfilerBenchmarkOptions());
        }

        [Benchmark(Description = "Creation of a standalone CustomTiming")]
        public CustomTiming Creation() => new CustomTiming(Profiler, "Test", includeStackTrace: IncludeStackTrace);

        [Benchmark(Description = "Creation a CustomTiming via MiniProfiler")]
        public void AddingToMiniProfiler()
        {
            Profiler.CustomTiming("Test", "MyCategory", includeStackTrace: IncludeStackTrace);
        }

        [Benchmark(Description = "Using a CustomTiming with MiniProfiler")]
        public void UsingWithMiniProfiler()
        {
            using (Profiler.CustomTiming("Test", "MyCategory", includeStackTrace: IncludeStackTrace))
            {
                // Trigger the .Dispose()
            }
        }
    }
}
