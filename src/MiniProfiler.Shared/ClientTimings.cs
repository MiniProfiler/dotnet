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
        private const string TimingPrefix = "clientPerformance[timing][";
        private const string ProbesPrefix = "clientProbes[";

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
        public static ClientTimings FromForm(IDictionary<string, string> form)
        {
            ClientTimings timing = null;
            long navigationStart;
            // AJAX requests won't have client timings
            if (!form.ContainsKey(TimingPrefix + "navigationStart]")) return timing;
            long.TryParse(form[TimingPrefix + "navigationStart]"], out navigationStart);
            if (navigationStart > 0)
            {
                var timings = new List<ClientTiming>();
                timing = new ClientTimings();

                int redirectCount;
                int.TryParse(form["clientPerformance[navigation][redirectCount]"], out redirectCount);
                timing.RedirectCount = redirectCount;

                var clientPerf = new Dictionary<string, ClientTiming>();
                var clientProbes = new Dictionary<int, ClientTiming>();

                foreach (string key in
                        form.Keys.Cast<string>()
                               .OrderBy(i => i.IndexOf("Start]", StringComparison.Ordinal) > 0 ? "_" + i : i))
                {
                    if (key.StartsWith(TimingPrefix))
                    {
                        long val;
                        long.TryParse(form[key], out val);
                        val -= navigationStart;

                        string parsedName = key.Substring(
                            TimingPrefix.Length, (key.Length - 1) - TimingPrefix.Length);

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

                    if (key.StartsWith(ProbesPrefix))
                    {
                        int probeId;
                        if (key.IndexOf("]", StringComparison.Ordinal) > 0 && int.TryParse(key.Substring(ProbesPrefix.Length, key.IndexOf("]", StringComparison.Ordinal) - ProbesPrefix.Length), out probeId))
                        {
                            ClientTiming t;
                            if (!clientProbes.TryGetValue(probeId, out t))
                            {
                                t = new ClientTiming();
                                clientProbes.Add(probeId, t);
                            }

                            if (key.EndsWith("[n]"))
                            {
                                t.Name = form[key];
                            }

                            if (key.EndsWith("[d]"))
                            {
                                long val;
                                long.TryParse(form[key], out val);
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
    }
}
