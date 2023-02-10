using BenchmarkDotNet.Attributes;
using StackExchange.Profiling.Internal;

namespace Benchmarks.Benchmarks
{
    [Config(typeof(Configs.Memory))]
    public class CreationBenchmarks
    {
        private static readonly MiniProfilerBaseOptions BaseOptions = new MiniProfilerBenchmarkOptions();

        [Benchmark(Description = "Start and Stop MiniProfiler")]
        public void StartStopProfiler()
        {
            var profiler = BaseOptions.StartProfiler("My Profiler");
            profiler?.Stop(true);
        }
    }
}
