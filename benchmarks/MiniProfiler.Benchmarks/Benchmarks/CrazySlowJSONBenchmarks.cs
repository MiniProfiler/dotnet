#if NET472
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Benchmarks.Benchmarks
{
    [ClrJob]
    [Config(typeof(Configs.Memory))]
    public class CrazySlowJSONBenchmarks
    {
        private static readonly Dictionary<string, object> _complex;

        static CrazySlowJSONBenchmarks()
        {
            const int iterations = 25;
            var dict = new Dictionary<string, object>();
            for (var i = 0; i < iterations; i++)
            {
                var sub = new Dictionary<string, object>();
                dict[i.ToString()] = sub;
                for (var j = 0; j < iterations; j++)
                {
                    var sub2 = new Dictionary<string, object>();
                    sub[j.ToString()] = sub2;
                    for (var k = 0; k < iterations; k++)
                    {
                        sub2[k.ToString()] = new
                        {
                            a = new { test = 1, bleh = 2, thing = "hey" },
                            b = new { test = 1, bleh = 2, thing = "hey" },
                            c = new { test = 1, bleh = 2, thing = "hey" },
                            d = new { test = 1, bleh = 2, thing = "hey" }
                        };
                    }
                }
            }
            _complex = dict;
        }

        [Benchmark]
        public string Serialize() =>
            new JavaScriptSerializer() { MaxJsonLength = int.MaxValue }.Serialize(_complex);
    }
}
#endif
