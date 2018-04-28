using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Times collected from the client
    /// </summary>
    [DataContract]
    public class ClientTimings
    {
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

        /// <summary>
        /// Gets a ClientTimings object from a <see cref="ResultRequest"/>.
        /// </summary>
        /// <param name="request">The request to convert.</param>
        /// <returns>A <see cref="ClientTimings"/> object.</returns>
        public static ClientTimings FromRequest(ResultRequest request)
        {
            var result = new ClientTimings()
            {
                RedirectCount = request.RedirectCount ?? 0,
                Timings = new List<ClientTiming>(request.Performance?.Count + request.Probes?.Count ?? 0)
            };
            if (request.Performance?.Count > 0)
            {
                foreach (var t in request.Performance)
                {
                    if (t.Name?.EndsWith("End") == true)
                    {
                        continue;
                    }
                    result.Timings.Add(t);
                }
            }
            if (request.Probes?.Count > 0)
            {
                result.Timings.AddRange(request.Probes);
            }
            // Noise
            result.Timings.RemoveAll(t => t.Start < 0 || t.Duration < 0);
            // Sort for storage later
            result.Timings.Sort((a, b) => a.Start.CompareTo(b.Start));

            // TODO: Collapse client start/end timings? Probably...
            return result;
        }
    }
}
