namespace StackExchange.Profiling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web;

    /// <summary>
    /// Times collected from the client
    /// </summary>
    [DataContract]
    public class ClientTimings
    {
        /// <summary>
        /// The client timing prefix.
        /// </summary>
        private const string ClientTimingPrefix = "clientPerformance[timing][";

        /// <summary>
        /// The client probes prefix.
        /// </summary>
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

        /// <summary>
        /// Returns null if there is not client timing stuff
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>the client timings.</returns>
        public static ClientTimings FromRequest(HttpRequest request)
        {
            ClientTimings timing = null;
            long navigationStart = 0;
            long.TryParse(request[ClientTimingPrefix + "navigationStart]"], out navigationStart);
            if (navigationStart > 0)
            {
                var timings = new List<ClientTiming>();

                timing = new ClientTimings();

                int redirectCount = 0;
                int.TryParse(request["clientPerformance[navigation][redirectCount]"], out redirectCount);
                timing.RedirectCount = redirectCount;

                var clientPerf = new Dictionary<string, ClientTiming>();
                var clientProbes = new Dictionary<int, ClientTiming>();

                foreach (
                    string key in
                        request.Form.Keys.Cast<string>()
                               .OrderBy(i => i.IndexOf("Start]", StringComparison.Ordinal) > 0 ? "_" + i : i))
                {
                    if (key.StartsWith(ClientTimingPrefix))
                    {
                        long val = 0;
                        long.TryParse(request[key], out val);
                        val -= navigationStart;

                        string parsedName = key.Substring(
                            ClientTimingPrefix.Length, (key.Length - 1) - ClientTimingPrefix.Length);

                        // just ignore stuff that is negative ... not relevant
                        if (val > 0)
                        {
                            if (parsedName.EndsWith("Start"))
                            {
                                var shortName = parsedName.Substring(0, parsedName.Length - 5);
                                clientPerf[shortName] = new ClientTiming
                                                            {
                                                                Duration = -1,
                                                                Name = parsedName,
                                                                Start = val
                                                            };
                            }
                            else if (parsedName.EndsWith("End"))
                            {
                                var shortName = parsedName.Substring(0, parsedName.Length - 3);
                                ClientTiming t;
                                if (clientPerf.TryGetValue(shortName, out t))
                                {
                                    t.Duration = val - t.Start;
                                    t.Name = shortName;
                                }
                            }
                            else
                            {
                                clientPerf[parsedName] = new ClientTiming { Name = parsedName, Start = val, Duration = -1 };
                            }
                        }
                    }

                    if (key.StartsWith(ClientProbesPrefix))
                    { 
                        int probeId;
                        if (key.IndexOf("]", StringComparison.Ordinal) > 0 && int.TryParse(key.Substring(ClientProbesPrefix.Length, key.IndexOf("]", StringComparison.Ordinal) - ClientProbesPrefix.Length), out probeId))
                        {
                            ClientTiming t;
                            if (!clientProbes.TryGetValue(probeId, out t))
                            {
                                t = new ClientTiming();
                                clientProbes.Add(probeId, t);
                            }

                            if (key.EndsWith("[n]"))
                            {
                                t.Name = request[key];
                            }

                            if (key.EndsWith("[d]")) 
                            {
                                long val = 0;
                                long.TryParse(request[key], out val);
                                if (val > 0)
                                {
                                    t.Start = val - navigationStart;
                                }
                            }
                        }
                    }
                }

                foreach (var group in clientProbes
                          .Values.OrderBy(p => p.Name)
                          .GroupBy(p => p.Name))
                {
                    ClientTiming current = null;
                    foreach (var item in group)
                    {
                        if (current == null)
                        {
                            current = item;
                        }
                        else
                        {
                            current.Duration = item.Start - current.Start;
                            timings.Add(current);
                            current = null;
                        }
                    }
                }

                foreach (var item in clientPerf.Values)
                {
                    item.Name = SentenceCase(item.Name);
                }

                timings.AddRange(clientPerf.Values);
                timing.Timings = timings.OrderBy(t => t.Start).ToList();
            }

            return timing;
        }

        /// <summary>
        /// convert to sentence case.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>the converted string.</returns>
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
        }
    }
}
