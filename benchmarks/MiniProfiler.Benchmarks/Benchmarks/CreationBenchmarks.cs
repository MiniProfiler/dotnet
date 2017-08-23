using BenchmarkDotNet.Attributes;
using StackExchange.Profiling;

namespace Benchmarks.Benchmarks
{
    [Config(typeof(Configs.MemoryFast))]
    public class CreationBenchmarks
    {
        private static readonly MiniProfilerBaseOptions BaseOptions = new MiniProfilerBaseOptions()
        {
            Storage = null
        };

        [Benchmark(Description = "BaseOptions.StartProfiler")]
        public MiniProfiler Profiler() => BaseOptions.StartProfiler("My Pofiler");
    }
}
