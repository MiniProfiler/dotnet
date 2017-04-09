using BenchmarkDotNet.Attributes;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [Config(typeof(Configs.Full))]
    public class StackTraceSnippetBenchmarks
    {
        [Benchmark(Description = "StackTraceSnippet.Get()")]
        public string StackTraceSnippetGet() => StackTraceSnippet.Get();
    }
}
