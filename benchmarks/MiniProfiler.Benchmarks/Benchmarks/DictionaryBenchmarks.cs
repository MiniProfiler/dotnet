using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Benchmarks.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [Config(typeof(Configs.Memory))]
    public class DictionaryBenchmarks
    {
        [Benchmark(Description = "new Dictionary<string, CustomTiming>")]
        public Dictionary<string, CustomTiming> DictionaryCreate() => new();

        [Benchmark(Description = "new ConcurrentDictionary<string, CustomTiming>")]
        public ConcurrentDictionary<string, CustomTiming> ConcurrentDictionaryCreate() => new();
    }
}
