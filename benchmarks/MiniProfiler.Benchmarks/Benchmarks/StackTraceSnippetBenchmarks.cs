using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Full))]
    public class StackTraceSnippetBenchmarks
    {
        private MiniProfilerBaseOptions Options { get; } = new MiniProfilerBaseOptions();

        [Benchmark(Description = "System.Ben Baseline")]
        public void SystemDotBen() { }
        [Benchmark(Description = "StackTraceSnippet.Get()")]
        public string StackTraceSnippetGet() => StackTraceSnippet.Get(Options);
    }
}
