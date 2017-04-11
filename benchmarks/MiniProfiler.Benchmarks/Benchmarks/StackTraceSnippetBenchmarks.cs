using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Full))]
    public class StackTraceSnippetBenchmarks
    {
        [Benchmark(Description = "System.Ben Baseline")]
        public void SystemDotBen() { }
        [Benchmark(Description = "StackTraceSnippet.Get()")]
        public string StackTraceSnippetGet() => StackTraceSnippet.Get();
    }
}
