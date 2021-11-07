using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [Config(typeof(Configs.Full))]
    public class StackTraceSnippetBenchmarks
    {
        private MiniProfilerBenchmarkOptions Options { get; } = new MiniProfilerBenchmarkOptions();

        [Benchmark(Description = "System.Ben Baseline")]
        public void SystemDotBen() { }

        [Benchmark(Description = "StackTraceSnippet.Get()")]
        public string StackTraceSnippetGet() => StackTraceSnippet.Get(Options);
    }
}
