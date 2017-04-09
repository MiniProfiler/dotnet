using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [Config(typeof(Configs.Memory))]
    public class JSONBenchmarks
    {
        private static readonly MiniProfiler _simpleProfiler = new MiniProfiler("Simple");
        private static readonly string _simpleProfilerJson = _simpleProfiler.ToJson();

        [Benchmark(Description = ".ToJson(): Minimal MiniProfiler")]
        public string SimpleSerialize() => _simpleProfiler.ToJson();

        [Benchmark(Description = ".FromJson(): Minimal MiniProfiler")]
        public MiniProfiler SimpleDeserialize() => MiniProfiler.FromJson(_simpleProfilerJson);

        [Benchmark(Description = ".ToJson(): Minimal MiniProfiler (Newtonsoft)")]
        public string SimpleSerializeNewtonsoft() => JsonConvert.SerializeObject(_simpleProfiler);

        [Benchmark(Description = ".FromJson(): Minimal MiniProfiler (Newtonsoft)")]
        public MiniProfiler SimpleDeserializeNewtonsoft() => JsonConvert.DeserializeObject<MiniProfiler>(_simpleProfilerJson);

        [Benchmark(Description = ".ToJson(): Minimal MiniProfiler (Jil)")]
        public string SimpleSerializeBuiltIn() => JsonConvert.SerializeObject(_simpleProfiler);

        [Benchmark(Description = ".FromJson(): Minimal MiniProfiler (Jil)")]
        public MiniProfiler SimpleDeserializeBuiltIn() => JsonConvert.DeserializeObject<MiniProfiler>(_simpleProfilerJson);
    }
}
