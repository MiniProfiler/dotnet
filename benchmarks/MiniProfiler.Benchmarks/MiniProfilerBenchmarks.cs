using BenchmarkDotNet.Attributes;
using StackExchange.Profiling;
namespace Benchmarks
{
    [Config(typeof(Configs.Full))]
    public class MiniProfilerBenchmarks
    {
        [Benchmark(Description = "new MiniProfiler")]
        public MiniProfiler Creation() => new MiniProfiler("Test");
    }
}
