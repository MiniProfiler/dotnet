using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace StackExchange.Profiling.Wcf
{
    [DataContract]
    public class MiniProfilerRequestHeader
    {
        public const string HeaderName = "MiniProfilerRequestHeader";
        public const string HeaderNamespace = "StackExchange.Profiling.Wcf";


        [DataMember]
        public Guid ParentProfilerId { get; set; }

        [DataMember]
        // The name of the user as provided 
        public string User { get; set; }

        [DataMember]
        public bool ExcludeTrivialMethods { get; set; }

        [DataMember]
        public decimal? TrivialDurationThresholdMilliseconds { get; set; }
    }
}
