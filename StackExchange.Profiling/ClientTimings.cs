using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Times collected from the client
    /// </summary>
    [DataContract]
    public class ClientTimings
    {
        /// <summary>
        /// A client timing probe
        /// </summary>
        [DataContract]
        public class ClientTiming
        {
            /// <summary>
            /// 
            /// </summary>
            [DataMember(Order = 1)]
            public string Name { get; set; }

            /// <summary>
            /// 
            /// </summary>
            [DataMember(Order = 2)]
            public Decimal Start { get; set; }

            /// <summary>
            /// 
            /// </summary>
            [DataMember(Order = 3)]
            public Decimal Duration { get; set; }
        }

        const string clientTimingPrefix = "clientPerformance[timing][";
        const string clientProbesPrefix = "clientProbes[";

        /// <summary>
        /// Returns null if there is not client timing stuff
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static ClientTimings FromRequest(HttpRequest request)
        {

            ClientTimings timing = null;
            long navigationStart = 0;
            long.TryParse(request[clientTimingPrefix + "navigationStart]"], out navigationStart);
            if (navigationStart > 0)
            {
                List<ClientTiming> timings = new List<ClientTiming>();

                timing = new ClientTimings();

                int redirectCount = 0;
                int.TryParse(request["clientPerformance[navigation][redirectCount]"], out redirectCount);
                timing.RedirectCount = redirectCount;

                Dictionary<string, ClientTiming> clientPerf = new Dictionary<string, ClientTiming>();
                Dictionary<int, ClientTiming> clientProbes = new Dictionary<int, ClientTiming>(); 

                foreach (string key in request.Form.Keys.Cast<string>().OrderBy(i => i.IndexOf("Start]") > 0 ? "_" + i : i ))
                {
                    if (key.StartsWith(clientTimingPrefix))
                    {
                        long val = 0;
                        long.TryParse(request[key], out val);
                        val -= navigationStart;

                        string parsedName = key.Substring(clientTimingPrefix.Length, (key.Length-1) - clientTimingPrefix.Length);
                        // just ignore stuff that is negative ... not relevant
                        if (val > 0)
                        {
                            if (parsedName.EndsWith("Start"))
                            {
                                var shortName = parsedName.Substring(0, parsedName.Length - 5);
                                clientPerf[shortName] = new ClientTiming {Duration = -1, Name = parsedName, Start = val};
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

                    if (key.StartsWith(clientProbesPrefix))
                    { 
                        int probeId;
                        if (key.IndexOf("]") > 0 && int.TryParse(key.Substring(clientProbesPrefix.Length, key.IndexOf("]") - clientProbesPrefix.Length), out probeId))
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

        private static string SentenceCase(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i == 0)
                {
                    sb.Append(Char.ToUpper(s[0]));
                    continue;
                }
                else if (s[i] == char.ToUpper(s[i])) 
                {
                    sb.Append(' ');
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Stores information about client perf
        /// </summary>
        public ClientTimings()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Order = 1)]
        public int RedirectCount { get; set; }

        /// <summary>
        /// List of client side timings
        /// </summary>
        [DataMember(Order = 2)]
        public List<ClientTiming> Timings { get; set; }

    }
}
