using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using Jil;
using Newtonsoft.Json;
using ServiceStack.Text;
using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;

namespace Benchmarks
{
    [ClrJob, CoreJob]
    [Config(typeof(Configs.Memory))]
    public class JSONBenchmarks
    {
        private static readonly Options JilOptions = Options.ISO8601ExcludeNulls;
        private static readonly MiniProfiler _simpleProfiler = new MiniProfiler("Simple");
        private static readonly string _simpleProfilerJson = _simpleProfiler.ToJson();

        private static readonly MiniProfiler _complexProfiler = GetComplexProfiler();
        private static readonly string _complexProfilerJson = _complexProfiler.ToJson();

        static JSONBenchmarks()
        {
            // Avoid the UTC and back roundtrip
            JsConfig.SkipDateTimeConversion = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
        }

        [Benchmark(Description = "Serialize: Simple MiniProfiler (.ToJson())")]
        public string SimpleSerialize() => _simpleProfiler.ToJson();
        [Benchmark(Description = "Serialize: Simple MiniProfiler (Newtonsoft)")]
        public string SimpleSerializeNewtonsoft() => JsonConvert.SerializeObject(_simpleProfiler);
        [Benchmark(Description = "Serialize: Simple MiniProfiler (Jil)")]
        public string SimpleSerializeJil() => JSON.Serialize(_simpleProfiler, JilOptions);
        [Benchmark(Description = "Serialize: Simple MiniProfiler (ServiceStack)")]
        public string SimpleSerializeServiceStack() => ServiceStack.Text.JsonSerializer.SerializeToString(_simpleProfiler);

        [Benchmark(Description = "Serialize: Complex MiniProfiler (.ToJson())")]
        public string ComplexSerialize() => _complexProfiler.ToJson();
        [Benchmark(Description = "Serialize: Complex MiniProfiler (Newtonsoft)")]
        public string ComplexSerializeNewtonsoft() => JsonConvert.SerializeObject(_complexProfiler);
        [Benchmark(Description = "Serialize: Complex MiniProfiler (Jil)")]
        public string ComplexSerializeJil() => JSON.Serialize(_complexProfiler, JilOptions);
        [Benchmark(Description = "Serialize: Complex MiniProfiler (ServiceStack)")]
        public string ComplexSerializeServiceStack() => ServiceStack.Text.JsonSerializer.SerializeToString(_complexProfiler);

        [Benchmark(Description = "Deserialize: Simple MiniProfiler (.FromJson())")]
        public MiniProfiler SimpleDeserialize() => MiniProfiler.FromJson(_simpleProfilerJson);
        [Benchmark(Description = "Deserialize: Simple MiniProfiler (Newtonsoft)")]
        public MiniProfiler SimpleDeserializeNewtonsoft() => JsonConvert.DeserializeObject<MiniProfiler>(_simpleProfilerJson);
        [Benchmark(Description = "Deserialize: Simple MiniProfiler (Jil)")]
        public MiniProfiler SimpleDeserializeBuiltIn() => JSON.Deserialize<MiniProfiler>(_simpleProfilerJson, JilOptions);
        [Benchmark(Description = "Deserialize: Simple MiniProfiler (ServiceStack)")]
        public MiniProfiler SimpleDeserializeServiceStack() => ServiceStack.Text.JsonSerializer.DeserializeFromString<MiniProfiler>(_simpleProfilerJson);

        [Benchmark(Description = "Deserialize: Complex MiniProfiler (.FromJson())")]
        public MiniProfiler ComplexDeserialize() => MiniProfiler.FromJson(_complexProfilerJson);
        [Benchmark(Description = "Deserialize: Complex MiniProfiler (Newtonsoft)")]
        public MiniProfiler ComplexDeserializeNewtonsoft() => JsonConvert.DeserializeObject<MiniProfiler>(_complexProfilerJson);
        [Benchmark(Description = "Deserialize: Complex MiniProfiler (Jil)")]
        public MiniProfiler ComplexDeserializeBuiltIn() => JSON.Deserialize<MiniProfiler>(_complexProfilerJson, JilOptions);
        [Benchmark(Description = "Deserialize: Complex MiniProfiler (ServiceStack)")]
        public MiniProfiler ComplexDeserializeServiceStack() => ServiceStack.Text.JsonSerializer.DeserializeFromString<MiniProfiler>(_complexProfilerJson);

        private static MiniProfiler GetComplexProfiler()
        {
            var mp = new MiniProfiler("Complex");
            for (var i = 0; i < 50; i++)
            {
                using (mp.Step("Step " + i))
                {
                    for (var j = 0; j < 50; j++)
                    {
                        using (mp.Step("SubStep " + j))
                        {
                            for (var k = 0; k < 50; k++)
                            {
                                using (mp.CustomTiming("Custom " + k, "YOLO!"))
                                {
                                }
                            }
                        }
                    }
                }
            }
            return mp;
        }
    }
}
