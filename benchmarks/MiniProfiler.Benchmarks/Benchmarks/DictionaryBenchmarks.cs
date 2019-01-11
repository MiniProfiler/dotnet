using BenchmarkDotNet.Attributes;
using StackExchange.Profiling;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Benchmarks.Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Memory))]
    public class DictionaryBenchmarks
    {
        [Benchmark(Description = "new Dictionary<string, CustomTiming>")]
        public Dictionary<string, CustomTiming> DictionaryCreate() =>
            new Dictionary<string, CustomTiming>();

        [Benchmark(Description = "new ConcurrentDictionary<string, CustomTiming>")]
        public ConcurrentDictionary<string, CustomTiming> ConcurrentDictionaryCreate() =>
            new ConcurrentDictionary<string, CustomTiming>();
    }
}
