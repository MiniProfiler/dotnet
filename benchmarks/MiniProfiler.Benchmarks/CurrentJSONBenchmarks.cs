using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Memory))]
    public class CurrentJSONBenchmarks
    {
        private static readonly MiniProfiler _simpleProfiler = new MiniProfiler("Simple");
        private static readonly string _simpleProfilerJson = _simpleProfiler.ToJson();
        private static readonly MiniProfiler _complexProfiler = Utils.GetComplexProfiler();
        private static readonly string _complexProfilerJson = _complexProfiler.ToJson();

        [Benchmark(Description = "System.Ben Baseline")]
        public void SystemDotBen() { }

        [Benchmark(Description = "Serialize: Simple MiniProfiler (.ToJson())")]
        public string SimpleSerialize() => _simpleProfiler.ToJson();

        [Benchmark(Description = "Serialize: Complex MiniProfiler (.ToJson())")]
        public string ComplexSerialize() => _complexProfiler.ToJson();

        [Benchmark(Description = "Deserialize: Simple MiniProfiler (.FromJson())")]
        public MiniProfiler SimpleDeserialize() => MiniProfiler.FromJson(_simpleProfilerJson);

        [Benchmark(Description = "Deserialize: Complex MiniProfiler (.FromJson())")]
        public MiniProfiler ComplexDeserialize() => MiniProfiler.FromJson(_complexProfilerJson);
    }
}
