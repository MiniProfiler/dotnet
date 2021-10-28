using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StackExchange.Profiling;
using StackExchange.Profiling.SqlFormatters;

namespace Benchmarks.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [Config(typeof(Configs.Memory))]
    public class InlineFormatterBenchmarks
    {
        [Params(1, 100, 1_000)]
        public int NumParams { get; set; }

        [Params(false, true)]
        public bool ParamNamePrefixed { get; set; }

        private string _queryString;
        private List<SqlTimingParameter> _params;

        private InlineFormatter _formatter;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var queryBuilder = new StringBuilder("SELECT * FROM Table WHERE Id IN (");
            _params = new(NumParams);
            for(var i = 0; i < NumParams; i++)
            {
                queryBuilder.Append("@param");
                queryBuilder.Append(i + 1);
                if(i != NumParams-1)
                {
                    queryBuilder.Append(',');
                }

                _params.Add(new SqlTimingParameter
                {
                    Name = $"{(ParamNamePrefixed ? '@' : string.Empty)}param{i + 1}",
                    DbType = "Int32",
                    Value = i.ToString(),
                });
            }
            queryBuilder.Append(')');

            _queryString = queryBuilder.ToString();

            _formatter = new();
        }

        [Benchmark]
        public void FormatSql() => _formatter.FormatSql(_queryString, _params);
    }
}
