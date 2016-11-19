using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Times collected from the client
    /// </summary>
    [DataContract]
    public class ClientTimings
    {
        private const string ClientTimingPrefix = "clientPerformance[timing][";
        private const string ClientProbesPrefix = "clientProbes[";

        /// <summary>
        /// Gets or sets the list of client side timings
        /// </summary>
        [DataMember(Order = 2)]
        public List<ClientTiming> Timings { get; set; }

        /// <summary>
        /// Gets or sets the redirect count.
        /// </summary>
        [DataMember(Order = 1)]
        public int RedirectCount { get; set; }
        
        private static string SentenceCase(string value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (i == 0)
                {
                    sb.Append(char.ToUpper(value[0]));
                    continue;
                }
                
                if (value[i] == char.ToUpper(value[i])) 
                {
                    sb.Append(' ');
                }

                sb.Append(value[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// A client timing probe
        /// </summary>
        [DataContract]
        public class ClientTiming
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            [DataMember(Order = 1)]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the start.
            /// </summary>
            [DataMember(Order = 2)]
            public decimal Start { get; set; }

            /// <summary>
            /// Gets or sets the duration.
            /// </summary>
            [DataMember(Order = 3)]
            public decimal Duration { get; set; }

            /// <summary>
            /// Unique Identifier used for sql storage. 
            /// </summary>
            /// <remarks>Not set unless storing in Sql</remarks>
            public Guid Id { get; set; }

            /// <summary>
            /// Used for sql storage
            /// </summary>
            /// <remarks>Not set unless storing in Sql</remarks>
            public Guid MiniProfilerId { get; set; }
        }
    }
}
