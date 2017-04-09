using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using StackExchange.Profiling;
namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Memory))]
    public class MiniProfilerBenchmarks
    {
        [Benchmark(Description = "new MiniProfiler")]
        public MiniProfiler Creation() => new MiniProfiler("Test");
    }
}
