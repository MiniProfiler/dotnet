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

        public string ToHeaderText()
        {
            var text = ParentProfilerId.ToString() + "&" + User + "&" + (ExcludeTrivialMethods ? "y" : "n") + (TrivialDurationThresholdMilliseconds.HasValue ? "&" + TrivialDurationThresholdMilliseconds.Value.ToString() : string.Empty);

            return text;
        }

        public static MiniProfilerRequestHeader FromHeaderText(string text)
        {
            var parts = text.Split('&');
            var header = new MiniProfilerRequestHeader
            {
                ParentProfilerId = Guid.Parse(parts[0]),
                User = parts[1],
                ExcludeTrivialMethods = parts[2] == "y"
            };

            if (parts.Length > 3)
                header.TrivialDurationThresholdMilliseconds = decimal.Parse(parts[3]);

            return header;
        }
    }
}
