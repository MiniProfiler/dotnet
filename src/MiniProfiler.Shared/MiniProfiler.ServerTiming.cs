using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler
    {
        /// <summary>
        /// Gets the Server-Timing header for this profiler, summarizing where time was spent for the browser.
        /// Example output: sql=9; "sql", redis=5; "redis", aspnet=20; "ASP.NET"
        /// </summary>
        /// <returns>A string, the value to put in a Server-Timing header.</returns>
        public string GetServerTimingHeader()
        {
            var total = DurationMilliseconds;
            var summary = new Dictionary<string, decimal>();
            foreach (var t in GetTimingHierarchy())
            {
                if (t.CustomTimings == null)
                {
                    continue;
                }
                foreach (var ct in t.CustomTimings)
                {
                    if (ct.Value?.Count > 0)
                    {
                        decimal ctTotal = 0;
                        for (var i = 0; i < ct.Value.Count; i++)
                        {
                            ctTotal += ct.Value[i]?.DurationMilliseconds ?? 0;
                        }
                        summary[ct.Key] = (summary.TryGetValue(ct.Key, out decimal cur) ? cur : 0) + ctTotal;
                    }
                }
            }

            // Server-Timing: sql;desc="sql";dur=9,redis;desc="redis";dur=5,aspnet;desc="ASP.NET";dur=20
            var sb = new StringBuilder();
            foreach (var category in summary)
            {
                sb.Append(category.Key)
                  .Append(";desc=\"").Append(category.Key).Append("\"")
                  .Append(";dur=").Append(category.Value.ToString("0.##", NumberFormatInfo.InvariantInfo))
                  .Append(',');
            }
            sb.Append("total;desc=\"Total\";dur=").Append(total.ToString("0.##", NumberFormatInfo.InvariantInfo));
            return sb.ToString();
        }
    }
}
