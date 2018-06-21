using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using Jil;
using Newtonsoft.Json;
using ServiceStack.Text;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;

namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Memory))]
    public class JSONLibBenchmarks
    {
        private static readonly Options JilOptions = Options.ISO8601ExcludeNulls;

        private static readonly MiniProfiler _simpleProfiler = new MiniProfiler("Simple", new MiniProfilerBenchmarkOptions());
        private static readonly string _simpleProfilerJson = _simpleProfiler.ToJson();
        private static readonly MiniProfiler _complexProfiler = Utils.GetComplexProfiler(new MiniProfilerBenchmarkOptions());
        private static readonly string _complexProfilerJson = _complexProfiler.ToJson();

        static JSONLibBenchmarks()
        {
            // Avoid the UTC and back roundtrip
            JsConfig.SkipDateTimeConversion = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
        }

        [Benchmark(Description = "Serialize: Simple MiniProfiler (Newtonsoft)")]
        public string SimpleSerializeNewtonsoft() => JsonConvert.SerializeObject(_simpleProfiler);

        [Benchmark(Description = "Serialize: Simple MiniProfiler (Jil)")]
        public string SimpleSerializeJil() => JSON.Serialize(_simpleProfiler, JilOptions);

        [Benchmark(Description = "Serialize: Simple MiniProfiler (ServiceStack)")]
        public string SimpleSerializeServiceStack() => ServiceStack.Text.JsonSerializer.SerializeToString(_simpleProfiler);

        [Benchmark(Description = "Serialize: Complex MiniProfiler (Newtonsoft)")]
        public string ComplexSerializeNewtonsoft() => JsonConvert.SerializeObject(_complexProfiler);

        [Benchmark(Description = "Serialize: Complex MiniProfiler (Jil)")]
        public string ComplexSerializeJil() => JSON.Serialize(_complexProfiler, JilOptions);

        [Benchmark(Description = "Serialize: Complex MiniProfiler (ServiceStack)")]
        public string ComplexSerializeServiceStack() => ServiceStack.Text.JsonSerializer.SerializeToString(_complexProfiler);

        [Benchmark(Description = "Deserialize: Simple MiniProfiler (Newtonsoft)")]
        public MiniProfiler SimpleDeserializeNewtonsoft() => JsonConvert.DeserializeObject<MiniProfiler>(_simpleProfilerJson);

        [Benchmark(Description = "Deserialize: Simple MiniProfiler (Jil)")]
        public MiniProfiler SimpleDeserializeBuiltIn() => JSON.Deserialize<MiniProfiler>(_simpleProfilerJson, JilOptions);

        [Benchmark(Description = "Deserialize: Simple MiniProfiler (ServiceStack)")]
        public MiniProfiler SimpleDeserializeServiceStack() => ServiceStack.Text.JsonSerializer.DeserializeFromString<MiniProfiler>(_simpleProfilerJson);

        [Benchmark(Description = "Deserialize: Complex MiniProfiler (Newtonsoft)")]
        public MiniProfiler ComplexDeserializeNewtonsoft() => JsonConvert.DeserializeObject<MiniProfiler>(_complexProfilerJson);

        [Benchmark(Description = "Deserialize: Complex MiniProfiler (Jil)")]
        public MiniProfiler ComplexDeserializeBuiltIn() => JSON.Deserialize<MiniProfiler>(_complexProfilerJson, JilOptions);

        [Benchmark(Description = "Deserialize: Complex MiniProfiler (ServiceStack)")]
        public MiniProfiler ComplexDeserializeServiceStack() => ServiceStack.Text.JsonSerializer.DeserializeFromString<MiniProfiler>(_complexProfilerJson);
    }
}
