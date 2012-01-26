using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace StackExchange.Profiling.Wcf
{
    [DataContract]
    public class MiniProfilerResultsHeader
    {
        public const string HeaderName = "MiniProfilerResults";
        public const string HeaderNamespace = "StackExchange.Profiling.Wcf";

        [DataMember]
        public MiniProfiler ProfilerResults { get; set; }
    }
}
