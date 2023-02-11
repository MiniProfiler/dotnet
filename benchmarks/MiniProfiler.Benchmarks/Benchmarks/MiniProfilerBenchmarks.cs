using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [Config(typeof(Configs.Memory))]
    public class MiniProfilerBenchmarks
    {
        private static MiniProfilerBenchmarkOptions Options { get; } = new MiniProfilerBenchmarkOptions();

        [Benchmark(Description = "new MiniProfiler")]
        public MiniProfiler Creation() => new MiniProfiler("Test", Options);
    }
}
