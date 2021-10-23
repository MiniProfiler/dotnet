using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [Config(typeof(Configs.Memory))]
    public class SerializationBenchmarks
    {
        private static readonly MiniProfiler _simpleProfiler = new MiniProfiler("Simple", new MiniProfilerBenchmarkOptions());
        private static readonly string _simpleProfilerJson = _simpleProfiler.ToJson();
        private static readonly MiniProfiler _complexProfiler = Utils.GetComplexProfiler(new MiniProfilerBenchmarkOptions());
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
