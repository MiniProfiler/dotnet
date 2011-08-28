using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MvcMiniProfiler.Wcf
{
    [DataContract]
    public class MiniProfilerResultsHeader
    {
        public const string HeaderName = "MiniProfilerResults";
        public const string HeaderNamespace = "MvcMiniProfiler.Wcf";

        [DataMember]
        public MiniProfiler ProfilerResults { get; set; }
    }
}
